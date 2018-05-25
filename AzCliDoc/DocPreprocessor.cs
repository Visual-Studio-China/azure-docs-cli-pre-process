using Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.DocAsCode.Common;

namespace AzCliDocPreprocessor
{
    internal class DocPreprocessor
    {
        private const string FormalAzGroupName = "Reference";
        private const string YamlMimeProcessor = "### YamlMime:UniversalReference";
        private const string AutoGenFolderName = "docs-ref-autogen";
        private const string ReferenceIndexFileName = "reference-index";
        private const string ExtensionReferenceIndexFileName = "index";
        private const string ExtensionGlobalPrefix = "ext";

        private bool IsExtensionXml { get; set; }
        private string ExtensionXmlFolder { get; set; }
        private Options Options { get; set; }
        private CommandGroupConfiguration CommandGroupConfiguration { get; set; }
        private List<AzureCliViewModel> CommandGroups { get; set; } = new List<AzureCliViewModel>();
        private List<AzureCliUniversalParameter> GlobalParameters { get; set; } = new List<AzureCliUniversalParameter>();
        private List<string> SourceXmlPathSet = new List<string>();
        private List<string> ExtensionXmlPathSet = new List<string>();
        private Dictionary<string, CommitInfo> DocCommitIdMap { get; set; } = new Dictionary<string, CommitInfo>();
        private Dictionary<string, AzureCliViewModel> NameCommandGroupMap { get; set; } = new Dictionary<string, AzureCliViewModel>();
        private Dictionary<string, TocTitleMappings> TitleMappings { get; set; } = new Dictionary<string, TocTitleMappings>();
        private Dictionary<string, StringBuilder> TocFileContent { get; set; } = new Dictionary<string, StringBuilder>();
        private Dictionary<string, AzureCliUniversalViewModel> UniversalCommandGroups { get; set; } = new Dictionary<string, AzureCliUniversalViewModel>();

        public bool Run(Options options)
        {
            Options = options;
            Initialize();

            foreach (string sourceXmlPath in SourceXmlPathSet)
            {
                IsExtensionXml = false;
                ProccessOneXml(sourceXmlPath);
            }

            foreach (string extensionXmlPath in ExtensionXmlPathSet)
            {
                IsExtensionXml = true;

                // the extension group such as 'azure-cli-iot-ext'
                ExtensionXmlFolder = Path.GetDirectoryName(extensionXmlPath).Replace(Path.HasExtension(Options.ExtensionXmlPath) ? Path.GetDirectoryName(Options.ExtensionXmlPath) : Options.ExtensionXmlPath, "").TrimStart('\\');
                ProccessOneXml(extensionXmlPath);
            }

            JoinExtensionToc();

            return true;
        }

        private void ProccessOneXml(string oneXmlPath)
        {
            // Inits
            CommandGroups.Clear();
            NameCommandGroupMap.Clear();
            TocFileContent.Clear();
            UniversalCommandGroups.Clear();
            // Loads xml content
            var xDoc = XDocument.Load(oneXmlPath);
            var groups = xDoc.Root.XPathSelectElements("desc[@desctype='cligroup']");
            foreach (var group in groups)
            {
                var commandGroup = ExtractCommandGroup(group);
                var parentGroupName = commandGroup.Name.Substring(0, commandGroup.Name.LastIndexOf(' ') < 0 ? 0 : commandGroup.Name.LastIndexOf(' '));
                if ((TitleMappings.ContainsKey(commandGroup.Name) && !TitleMappings[commandGroup.Name].Show)
                    || (!string.IsNullOrEmpty(parentGroupName) && !NameCommandGroupMap.ContainsKey(parentGroupName)))
                    continue;
                CommandGroups.Add(commandGroup);
                NameCommandGroupMap[commandGroup.Name] = commandGroup;
            }

            var commands = xDoc.Root.XPathSelectElements("desc[@desctype='clicommand']");
            foreach (var command in commands)
            {
                string groupName = null;
                try
                {
                    var commandEntry = ExtractCommandEntry(command);
                    groupName = commandEntry.Name.Substring(0, commandEntry.Name.LastIndexOf(' '));
                    if (!NameCommandGroupMap.ContainsKey(groupName) ||
                        (TitleMappings.ContainsKey(commandEntry.Name) && !TitleMappings[commandEntry.Name].Show))
                        continue;
                    NameCommandGroupMap[groupName].Children.Add(commandEntry);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("The given key was not present in the dictionary"))
                        System.Console.WriteLine(groupName);
                    else
                        System.Console.WriteLine(ex.Message);
                }
            }

            //prepare command table to contain descendants, from bottom to top
            CommandGroups.Sort((group1, group2) => GetWordCount(group2.Name).CompareTo(GetWordCount(group1.Name)));
            foreach (var commandGroup in CommandGroups)
            {
                PrepareCommandBasicInfoList(commandGroup, commandGroup.Children, false);
                int groupWordCount = GetWordCount(commandGroup.Name);
                var subGroups = GetSubGroups(commandGroup.Name, groupWordCount);
                PrepareCommandBasicInfoList(commandGroup, subGroups, true);
                commandGroup.CommandBasicInfoList.Sort((item1, item2) => string.CompareOrdinal(item1.Name, item2.Name));
            }

            // Prepares universal yml object list
            foreach (var commandGroup in CommandGroups)
            {
                List<AzureCliUniversalItem> items = new List<AzureCliUniversalItem>();
                items.Add(new AzureCliUniversalItem()
                {
                    Uid = commandGroup.Uid,
                    Name = commandGroup.Name,
                    Langs = new List<string>() { CommandGroupConfiguration.Language },
                    Summary = commandGroup.Summary,
                    Description = commandGroup.Description,
                    Children = commandGroup.Children.Select(x => x.Uid).ToList()
                });
                foreach (AzureCliViewModel commandGroupChild in commandGroup.Children)
                {
                    AzureCliUniversalItem azureCliUniversalItem = new AzureCliUniversalItem()
                    {
                        Uid = commandGroupChild.Uid,
                        Name = commandGroupChild.Name,
                        Langs = new List<string>() { CommandGroupConfiguration.Language },
                        Summary = commandGroupChild.Summary,
                        Description = commandGroupChild.Description
                    };
                    if (null != commandGroupChild.Parameters && 0 != commandGroupChild.Parameters.Count)
                    {
                        azureCliUniversalItem.Parameters = (from parameterItem in commandGroupChild.Parameters
                                                            select new AzureCliUniversalParameter
                                                            {
                                                                Name = parameterItem.Name,
                                                                DefaultValue = parameterItem.DefaultValue,
                                                                IsRequired = bool.Parse(parameterItem.IsRequired),
                                                                Summary = parameterItem.Summary,
                                                                Description = parameterItem.Description,
                                                                ParameterValueGroup = parameterItem.ParameterValueGroup,
                                                                ValueFrom = parameterItem.ValueFrom
                                                            }).Except(GlobalParameters, new AzureCliUniversalParameterComparer()).ToList();
                    }
                    if (null != commandGroupChild.Examples && 0 != commandGroupChild.Examples.Count)
                    {
                        azureCliUniversalItem.Examples = (from exampleItem in commandGroupChild.Examples
                                                          select new AzureCliUniversalExample { Summary = exampleItem.Title, Syntax = new AzureCliUniversalSyntax() { Content = exampleItem.Code } }).ToList();
                    }
                    if (null != commandGroupChild.Source && null != commandGroupChild.Source.Remote)
                        azureCliUniversalItem.Source = new AzureCliUniversalSource()
                        {
                            Path = commandGroupChild.Source.Remote.Path,
                            Remote = new AzureCliUniversalRemote()
                            {
                                Path = commandGroupChild.Source.Remote.Path,
                                Branch = commandGroupChild.Source.Remote.Branch,
                                Repo = commandGroupChild.Source.Remote.Repository
                            }
                        };
                    items.Add(azureCliUniversalItem);
                }

                AzureCliUniversalViewModel universalViewModel = new AzureCliUniversalViewModel()
                {
                    GlobalParameters = GetGlobalParameters(),
                    Commands = (from commandGroupBasicItem in commandGroup.CommandBasicInfoList
                                select new AzureCliUniversalCommand { Uid = GetUid(commandGroupBasicItem.Name), Name = commandGroupBasicItem.Name, Summary = commandGroupBasicItem.Description })
                               .ToList(),
                    Items = items,
                    Metadata = commandGroup.Metadata
                };
                UniversalCommandGroups.Add(commandGroup.Name, universalViewModel);
            }

            Save(GetXmlOutputFolder(oneXmlPath));
        }

        private void JoinExtensionToc()
        {
            string extFolder = Path.Combine(Options.DestDirectory, ExtensionGlobalPrefix);
            if (!Directory.Exists(extFolder)) return;

            foreach (string sourceXmlPath in SourceXmlPathSet)
            {
                IsExtensionXml = false;
                string targetYmlPath = Path.Combine(GetXmlOutputFolder(sourceXmlPath), "TOC.yml");
                var combinedYml = YamlUtility.Deserialize<List<AzureCliUniversalTOC>>(targetYmlPath);
                var extensionsReference = new AzureCliUniversalTOC
                {
                    name = "Extensions Reference",
                    items = new List<AzureCliUniversalTOC>()
                };

                foreach (var extensionYmlPath in Directory.GetFiles(extFolder, "TOC.yml", SearchOption.AllDirectories))
                {
                    var extensionYml = YamlUtility.Deserialize<List<AzureCliUniversalTOC>>(extensionYmlPath);
                    var group = extensionYmlPath.Replace(extFolder, "").TrimStart('\\').Split('\\').First();
                    var extensionRoot = extensionYml.First();
                    extensionRoot.name = group;
                    extensionsReference.items.Add(extensionRoot);
                }

                combinedYml.Add(extensionsReference);

                // Saves TOC
                using (var writer = new StreamWriter(targetYmlPath, false))
                {
                    YamlUtility.Serialize(writer, combinedYml);
                }

                // copy extension yml
                foreach (var file in Directory.GetFiles(extFolder, "*.*", SearchOption.AllDirectories))
                {
                    if(!Path.GetFileName(file).Equals("toc.yml", StringComparison.OrdinalIgnoreCase))
                    {
                        string targetPath = file.Replace(Options.DestDirectory, GetXmlOutputFolder(sourceXmlPath));
                        if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        }
                        File.Copy(file, targetPath, true);
                    }
                }
            }

            Directory.Delete(extFolder, true);
        }

        private string GetXmlOutputFolder(string xmlPath)
        {
            if (IsExtensionXml == false)
            {
                return Path.Combine(Options.DestDirectory, Path.GetDirectoryName(xmlPath).Replace(Path.HasExtension(Options.SourceXmlPath) ? Path.GetDirectoryName(Options.SourceXmlPath) : Options.SourceXmlPath, "").Replace("\\", ""), AutoGenFolderName);
            }
            else
            {
                return Path.Combine(Options.DestDirectory, ExtensionGlobalPrefix, ExtensionXmlFolder);
            }
        }

        private void PrepareCommandBasicInfoList(AzureCliViewModel group, IList<AzureCliViewModel> subItems, bool isGroup)
        {
            bool isTopGroup = string.Equals(group.Name, CommandGroupConfiguration.CommandPrefix, StringComparison.OrdinalIgnoreCase);
            foreach (var subItem in subItems)
            {
                group.CommandBasicInfoList.Add(new CommandBasicInfo
                {
                    Name = subItem.Name,
                    Description = !string.IsNullOrEmpty(subItem.Summary) ? subItem.Summary : subItem.Description,
                    HyperLink = isTopGroup ? (isGroup ? $"{subItem.HtmlId}" : $"#{subItem.HtmlId}") : (isGroup ? $"{group.HtmlId}/{subItem.HtmlId}" : $"{group.HtmlId}#{subItem.HtmlId}"),
                    IsGroup = isGroup
                });

                if (isGroup && !string.Equals(group.Name, CommandGroupConfiguration.CommandPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var basicInfo in subItem.CommandBasicInfoList)
                    {
                        group.CommandBasicInfoList.Add(new CommandBasicInfo
                        {
                            Name = basicInfo.Name,
                            Description = basicInfo.Description,
                            HyperLink = $"{group.HtmlId}/{basicInfo.HyperLink}",
                            IsGroup = basicInfo.IsGroup
                        });
                    }
                }
            }
        }

        private void Initialize()
        {
            if (string.IsNullOrEmpty(Options.DocExtension) || !Options.DocExtension.StartsWith("."))
                throw new ArgumentException("Invalid DocExtension");

            if (string.IsNullOrEmpty(Options.TocFileName))
                throw new ArgumentException("Invalid TocFileName");

            if (string.IsNullOrEmpty(Options.SourceXmlPath))
                throw new ArgumentException("Invalid Empty SourceXmlPath");
            else
            {
                FileAttributes attr = File.GetAttributes(Options.SourceXmlPath);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    SourceXmlPathSet.AddRange(GetSourceXmlPathSet(Options.SourceXmlPath));
                }
                else
                {
                    if (!File.Exists(Options.SourceXmlPath))
                        throw new ArgumentException("Invalid SourceXmlPath");
                    SourceXmlPathSet.Add(Options.SourceXmlPath);
                }
            }

            if (!string.IsNullOrEmpty(Options.ExtensionXmlPath))
            {
                FileAttributes attr = File.GetAttributes(Options.ExtensionXmlPath);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    ExtensionXmlPathSet.AddRange(GetSourceXmlPathSet(Options.ExtensionXmlPath));
                }
                else
                {
                    if (!File.Exists(Options.ExtensionXmlPath))
                        throw new ArgumentException("Invalid ExtensionXmlPathSet");
                    ExtensionXmlPathSet.Add(Options.ExtensionXmlPath);
                }
            }

            if (string.IsNullOrEmpty(Options.DestDirectory))
                throw new ArgumentException("Invalid DestDirectory");

            if (!string.IsNullOrEmpty(Options.DocCommitMapFile))
            {
                using (StreamReader reader = new StreamReader(Options.DocCommitMapFile))
                {
                    var docCommitFileContent = reader.ReadToEnd();
                    DocCommitIdMap = JsonConvert.DeserializeObject<Dictionary<string, CommitInfo>>(docCommitFileContent);
                }
            }

            GlobalParameters = GetGlobalParameters();
            TitleMappings = GetTitleMappings();
            if (Enum.GetNames(typeof(CommandGroupType)).Contains(Options.GroupName.ToUpper()))
                CommandGroupConfiguration = ParserCommandGroupConfiguration((CommandGroupType)Enum.Parse(typeof(CommandGroupType), Options.GroupName.ToUpper(), true));
            else
                CommandGroupConfiguration = ParserCommandGroupConfiguration(CommandGroupType.AZURE);
        }

        private void Save(string destDirectory)
        {
            if (!Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }
            // Saves groups
            CommandGroups.Sort((group1, group2) => string.CompareOrdinal(group1.Name, group2.Name));
            Dictionary<string, string> groupToFilePathMap = new Dictionary<string, string>();
            foreach (var commandGroup in UniversalCommandGroups)
            {
                var relativeDocPath = PrepareDocFilePath(destDirectory, commandGroup.Key);
                groupToFilePathMap.Add(commandGroup.Key, relativeDocPath.Replace('\\', '/'));
                using (var writer = new StreamWriter(Path.Combine(destDirectory, relativeDocPath), false))
                {
                    writer.WriteLine(YamlMimeProcessor);
                    YamlUtility.Serialize(writer, commandGroup.Value);
                }
            }

            // Saves TOC
            using (var writer = new StreamWriter(Path.Combine(destDirectory, "TOC.yml"), false))
            {
                if (NameCommandGroupMap.ContainsKey(CommandGroupConfiguration.CommandPrefix))
                    YamlUtility.Serialize(writer, new List<AzureCliUniversalTOC>() { PrepareFusionToc(NameCommandGroupMap[CommandGroupConfiguration.CommandPrefix], groupToFilePathMap) });
            }
        }

        private AzureCliUniversalTOC PrepareFusionToc(AzureCliViewModel group, IDictionary<string, string> groupToFilePathMap, string parentName = "")
        {
            AzureCliUniversalTOC azureCliUniversalTOC = null;
            string tocName = null;

            if (string.Equals(group.Name, CommandGroupConfiguration.CommandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                tocName = FormalAzGroupName;
                azureCliUniversalTOC = new AzureCliUniversalTOC()
                {
                    name = tocName,
                    uid = GetUid(group.Name)
                };
            }
            else
            {
                tocName = TitleMappings.ContainsKey(group.Name) ? TitleMappings[group.Name].TocTitle : group.Name.Replace(parentName, "").Trim();
                azureCliUniversalTOC = new AzureCliUniversalTOC()
                {
                    name = new CultureInfo("en-US", false).TextInfo.ToTitleCase(tocName),
                    uid = GetUid(group.Name),
                    displayName = group.Name
                };
            }
            List<AzureCliViewModel> children = group.Children;
            List<AzureCliViewModel> subGroups = GetSubGroups(group.Name, GetWordCount(group.Name));
            if (children.Count > 0 || subGroups.Count > 0)
                azureCliUniversalTOC.items = new List<AzureCliUniversalTOC>();
            List<AzureCliUniversalTOC> childrenTOC = new List<AzureCliUniversalTOC>();
            foreach (AzureCliViewModel child in children)
            {
                childrenTOC.Add(new AzureCliUniversalTOC()
                {
                    name = new CultureInfo("en-US", false).TextInfo.ToTitleCase(TitleMappings.ContainsKey(child.Name) ? TitleMappings[child.Name].TocTitle : child.Name.Replace(group.Name, "").Trim()),
                    uid = GetUid(child.Name),
                    displayName = child.Name
                });
            }
            List<AzureCliUniversalTOC> subGroupsToc = new List<AzureCliUniversalTOC>();
            foreach (AzureCliViewModel subGroup in subGroups)
            {
                subGroupsToc.Add(PrepareFusionToc(subGroup, groupToFilePathMap, group.Name));
            }
            if (childrenTOC.Count > 0 || subGroupsToc.Count > 0)
            {
                azureCliUniversalTOC.items = new List<AzureCliUniversalTOC>();
                childrenTOC.Sort((item1, item2) => string.CompareOrdinal(item1.name, item2.name));
                subGroupsToc.Sort((item1, item2) => string.CompareOrdinal(item1.name, item2.name));
                azureCliUniversalTOC.items.AddRange(childrenTOC);
                azureCliUniversalTOC.items.AddRange(subGroupsToc);
            }

            return azureCliUniversalTOC;
        }

        private List<AzureCliViewModel> GetSubGroups(string groupName, int groupWordCount)
        {
            return CommandGroups.FindAll(c => GetWordCount(c.Name) == groupWordCount + 1 && c.Name.StartsWith(groupName + " "));
        }

        private static int GetWordCount(string name)
        {
            return name.Count(c => c == ' ') + 1;
        }

        private string GetUid(string commandName)
        {
            string raw = commandName.Replace(' ', '_').Replace('-', '_');
            return IsExtensionXml ? $"{ExtensionGlobalPrefix}_{ExtensionXmlFolder.Replace("\\","_")}_{raw}": raw;
        }

        private string PrepareDocFilePath(string destDirectory, string name)
        {
            var pathSegments = name.Split(' ');
            if (pathSegments.Length == 1)
            {
                if (IsExtensionXml)
                {
                    return ExtensionReferenceIndexFileName + Options.DocExtension;
                }
                else
                {
                    return ReferenceIndexFileName + Options.DocExtension;
                }
            }

            string relativePath = string.Empty;

            for (int i = 1; i < pathSegments.Length - 1; ++i)
            {
                relativePath = Path.Combine(relativePath, pathSegments[i]);
                CreateDirectoryIfNotExist(Path.Combine(destDirectory, relativePath));
            }
            return Path.Combine(relativePath, pathSegments[pathSegments.Length - 1] + Options.DocExtension);
        }

        private void CreateDirectoryIfNotExist(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private AzureCliViewModel ExtractCommandGroup(XElement group)
        {
            var groupModel = ExtractCommand(group, true) as AzureCliViewModel;
            groupModel.Children = new List<AzureCliViewModel>();
            return groupModel;
        }

        private AzureCliViewModel ExtractCommandEntry(XElement commandElement)
        {
            var command = ExtractCommand(commandElement, false) as AzureCliViewModel;
            command.Parameters = new List<Parameter>();

            var args = commandElement.XPathSelectElements("desc_content/desc[@desctype='cliarg']");
            foreach (var arg in args)
            {
                var cliArg = new Parameter();
                cliArg.Name = arg.XPathSelectElement("desc_signature/desc_addname").Value;
                var fields = arg.XPathSelectElements("desc_content/field_list/field");
                foreach (var field in fields)
                {
                    var fieldValue = ExtractFieldValue(field);
                    var fieldName = field.Element("field_name").Value.ToLower();
                    switch (fieldName)
                    {
                        case "summary":
                            cliArg.Summary = fieldValue;
                            break;
                        case "description":
                            cliArg.Description = fieldValue;
                            break;
                        case "required":
                            cliArg.IsRequired = string.IsNullOrEmpty(fieldValue) ? fieldValue : fieldValue.ToLower();
                            break;
                        case "default":
                            cliArg.DefaultValue = fieldValue;
                            break;
                        case "allowed values":
                            cliArg.ParameterValueGroup = QuoteValueIfNecessary(fieldValue);
                            break;
                        case "values from":
                            cliArg.ValueFrom = fieldValue;
                            break;
                        default:
                            if (Options.IgnoreUnknownProperty)
                            {
                                var color = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(string.Format("UNKNOWN argument field:{0}", fieldName));
                                Console.ForegroundColor = color;
                                break;
                            }
                            else
                            {
                                throw new ApplicationException(string.Format("UNKNOWN argument field:{0}", fieldName));
                            }
                    }
                }
                command.Parameters.Add(cliArg);
            }
            return command;
        }

        /// <summary>
        /// Need to convert value from 'item1', 'item2' to '''item1'', ''item2'''
        /// </summary>
        /// <param name="fieldValue"></param>
        /// <returns></returns>
        private string QuoteValueIfNecessary(string fieldValue)
        {
            if (string.IsNullOrEmpty(fieldValue))
                return fieldValue;

            var subFields = fieldValue.Split(',');
            if (subFields.Length > 1)
            {
                char first = subFields[0][0];
                if (first == '\'')
                {
                    fieldValue = fieldValue.Replace("'", "''");
                    fieldValue = $"'{fieldValue}'";
                }
                else if (first == '"')
                {
                    fieldValue = $"'{fieldValue}'";
                }
            }
            return fieldValue;
        }

        private string ExtractFieldValue(XElement field)
        {
            var paragraph = field.XPathSelectElement("field_body/paragraph");
            if (paragraph != null)
                return ResolveHyperLink(paragraph);

            var listItems = field.XPathSelectElements("field_body/bullet_list/list_item");
            if (listItems.Count() > 0)
                return string.Join("|", listItems.Select(element => ResolveHyperLink(element)).ToArray());

            throw new ApplicationException(string.Format("UNKNOW field_body:{0}", field.Value));
        }

        private string ResolveHyperLink(XElement element)
        {
            var refs = element.Descendants("reference");
            var titleRefs = element.Descendants("title_reference");
            if (!refs.Any() && !titleRefs.Any())
                return element.Value;

            foreach (var titleReference in titleRefs.ToArray())
            {
                titleReference.ReplaceWith(new XText(string.Format("`{0}`", titleReference.Value)));
            }

            foreach (var reference in refs.ToArray())
            {
                if (IsValidUrl(reference.Value))
                {
                    var newRef = XElement.Parse("<a href=''></a>");
                    newRef.Attribute("href").Value = reference.Attribute("refuri")?.Value;
                    newRef.Value = reference.Value;
                    reference.ReplaceWith(newRef);
                }
                else
                {
                    var textNode = new XText(reference.Value);
                    reference.ReplaceWith(textNode);
                }
            }

            var value = String.Concat(element.Nodes());
            return value.Replace("</a><a", "</a> <a");
        }

        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return !url.EndsWith("://") && Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        private AzureCliViewModel ExtractCommand(XElement xElement, bool isGroup)
        {
            var name = xElement.XPathSelectElement("desc_signature/desc_addname").Value;
            if (!string.Equals(name, CommandGroupConfiguration.CommandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                name = string.Format("{0} {1}", CommandGroupConfiguration.CommandPrefix, name);
            }
            var ids = name.Split();
            var summary = ExtractFieldValueByName(xElement, "Summary");
            var description = ExtractFieldValueByName(xElement, "Description");
            var docSource = ExtractFieldValueByName(xElement, "Doc Source", false);

            AzureCliViewModel command = new AzureCliViewModel()
            {
                Examples = new List<Example>()
            };

            command.Name = name;
            command.HtmlId = ids[ids.Length - 1];
            command.Uid = GetUid(name);
            command.Summary = summary;
            command.Description = description;
            if (!string.IsNullOrEmpty(docSource) && !string.IsNullOrEmpty(Options.RepoOfSource))
            {
                if (isGroup)
                {
                    command.Metadata["doc_source_url_repo"] = string.Format("{0}/blob/{1}/", Options.RepoOfSource, Options.Branch);
                    command.Metadata["doc_source_url_path"] = docSource;
                    command.Metadata["original_content_git_url"] = string.Format("{0}/blob/{1}/{2}", Options.RepoOfSource, Options.Branch, docSource);
                    if (DocCommitIdMap.ContainsKey(docSource))
                    {
                        command.Metadata["gitcommit"] = string.Format("{0}/blob/{1}/{2}", Options.RepoOfSource, DocCommitIdMap[docSource].Commit, docSource);
                        var date = DocCommitIdMap[docSource].Date;
                        command.Metadata["updated_at"] = date.ToString();
                        command.Metadata["ms.date"] = date.ToString("MM/dd/yyyy");
                    }
                }

                command.Source = new RemoteGitInfo()
                {
                    Remote = new GitInfo()
                    {
                        Repository = Options.RepoOfSource + ".git",
                        Branch = Options.Branch,
                        Path = docSource
                    }
                };
            }

            var examples = xElement.XPathSelectElements("desc_content/desc[@desctype='cliexample']");
            foreach (var example in examples)
            {
                var cliExample = new Example();
                cliExample.Title = example.XPathSelectElement("desc_signature/desc_addname").Value;
                cliExample.Code = example.XPathSelectElement("desc_content/paragraph").Value;
                command.Examples.Add(cliExample);
            }

            return command;
        }

        private string ExtractFieldValueByName(XElement parent, string fieldName, bool resolveHyperLink = true)
        {
            var xPath = string.Format("desc_content/field_list/field[field_name = '{0}']/field_body/paragraph", fieldName);
            var element = parent.XPathSelectElement(xPath);
            if (element == null)
                return string.Empty;

            return resolveHyperLink ? ResolveHyperLink(element) : element.Value;
        }

        private List<AzureCliUniversalParameter> GetGlobalParameters()
        {
            return new JavaScriptSerializer().Deserialize<List<AzureCliUniversalParameter>>(new StreamReader("./data/GlobalParameters.json").ReadToEnd());
        }

        private Dictionary<string, TocTitleMappings> GetTitleMappings()
        {
            return new JavaScriptSerializer().Deserialize<Dictionary<string, TocTitleMappings>>(new StreamReader(Options.CommandFilter ?? "./data/TitleMapping.json").ReadToEnd());
        }

        private CommandGroupConfiguration ParserCommandGroupConfiguration(CommandGroupType commandGroupType)
        {
            switch (commandGroupType)
            {
                case CommandGroupType.AZURE:
                    return new JavaScriptSerializer().Deserialize<CommandGroupConfiguration>(new StreamReader("./data/AzureCliConfiguration.json").ReadToEnd());
                case CommandGroupType.VSTS:
                    return new JavaScriptSerializer().Deserialize<CommandGroupConfiguration>(new StreamReader("./data/VSTSCliConfiguration.json").ReadToEnd());
                default:
                    return new JavaScriptSerializer().Deserialize<CommandGroupConfiguration>(new StreamReader("./data/AzureCliConfiguration.json").ReadToEnd());
            }
        }

        private List<string> GetSourceXmlPathSet(string path)
        {
            List<string> filePaths = new List<string>();
            filePaths.AddRange(Directory.GetFiles(path, "*.xml"));
            foreach (string subPath in Directory.GetDirectories(path))
            {
                filePaths.AddRange(GetSourceXmlPathSet(subPath));
            }
            return filePaths;
        }
    }
}