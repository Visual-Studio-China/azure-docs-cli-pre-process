using Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using YamlDotNet.Serialization;

namespace AzCliDocPreprocessor
{
    internal class DocPreprocessor
    {
        private const string AzGroupName = "az";
        private const string FormalAzGroupName = "Reference";
        private List<AzureCliViewModel> CommandGroups { get; set; }
        private List<string> TocLines { get; set; }
        private Dictionary<string, AzureCliViewModel> NameCommandGroupMap { get; set; }
        private Dictionary<string, CommitInfo> DocCommitIdMap { get; set; }
        private Options Options { get; set; }

        public bool Run(Options options)
        {
            Options = options;
            Initialize();

            var xDoc = XDocument.Load(Options.SourceXmlPath);
            var groups = xDoc.Root.XPathSelectElements("desc[@desctype='cligroup']");
            foreach (var group in groups)
            {
                var commandGroup = ExtractCommandGroup(group);
                CommandGroups.Add(commandGroup);
                NameCommandGroupMap[commandGroup.Name] = commandGroup;
            }

            var commands = xDoc.Root.XPathSelectElements("desc[@desctype='clicommand']");
            foreach (var command in commands)
            {
                var commandEntry = ExtractCommandEntry(command);

                var groupName = commandEntry.Name.Substring(0, commandEntry.Name.LastIndexOf(' '));
                NameCommandGroupMap[groupName].Children.Add(commandEntry);
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

            Save(Options.DestDirectory);
            return true;
        }

        private static void PrepareCommandBasicInfoList(AzureCliViewModel group, IList<AzureCliViewModel> subItems, bool isGroup)
        {
            foreach (var subItem in subItems)
            {
                group.CommandBasicInfoList.Add(new CommandBasicInfo
                {
                    Name = subItem.Name,
                    Description = !string.IsNullOrEmpty(subItem.Summary) ? subItem.Summary : subItem.Description,
                    HyperLink = isGroup ? $"{group.HtmlId}/{subItem.HtmlId}" : $"{group.HtmlId}#{subItem.HtmlId}",
                    IsGroup = isGroup
                });

                if(isGroup && !string.Equals(group.Name, AzGroupName, StringComparison.OrdinalIgnoreCase))
                {
                    foreach(var basicInfo in subItem.CommandBasicInfoList)
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

            if (string.IsNullOrEmpty(Options.SourceXmlPath) || !File.Exists(Options.SourceXmlPath))
                throw new ArgumentException("Invalid SourceXmlPath");

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

            CommandGroups = new List<AzureCliViewModel>();
            NameCommandGroupMap = new Dictionary<string, AzureCliViewModel>();
            TocLines = new List<string>();
            if (!Directory.Exists(Options.DestDirectory))
            {
                Directory.CreateDirectory(Options.DestDirectory);
            }
        }

        private void Save(string destDirectory)
        {
            //save group
            CommandGroups.Sort((group1, group2) => string.CompareOrdinal(group1.Name, group2.Name));
            Dictionary<string, string> groupToFilePathMap = new Dictionary<string, string>();
            var serializer = new Serializer();
            foreach (var commandGroup in CommandGroups)
            {
                var relativeDocPath = PrepareDocFilePath(Options.DestDirectory, commandGroup.Name);
                groupToFilePathMap.Add(commandGroup.Name, relativeDocPath);
                using (var writer = new StreamWriter(Path.Combine(destDirectory, relativeDocPath), false))
                {
                    serializer.Serialize(writer, commandGroup);
                }
            }

            PrepareToc(NameCommandGroupMap[AzGroupName], groupToFilePathMap);
            File.WriteAllLines(Path.Combine(destDirectory, Options.TocFileName), TocLines);
        }

        private void PrepareToc(AzureCliViewModel group, IDictionary<string, string> groupToFilePathMap)
        {
            var builder = new StringBuilder();
            int groupWordCount = GetWordCount(group.Name);
            builder.Append('#', groupWordCount);
            string tocName = string.Equals(group.Name, AzGroupName, StringComparison.OrdinalIgnoreCase) ? FormalAzGroupName : group.Name;
            builder.AppendFormat(" [{0}]({1})", tocName, groupToFilePathMap[group.Name]);
            TocLines.Add(builder.ToString());

            //get all immediate children
            List<AzureCliViewModel> subItems = new List<AzureCliViewModel>();
            subItems.AddRange(group.Children);
            var subGroups = GetSubGroups(group.Name, groupWordCount);
            subItems.AddRange(subGroups);
            subItems.Sort((item1, item2) => string.CompareOrdinal(item1.Name, item2.Name));

            foreach (var subItem in subItems)
            {
                if(subItem.CommandBasicInfoList.Count > 0)
                {
                    PrepareToc(NameCommandGroupMap[subItem.Name], groupToFilePathMap);
                }
                else
                {
                    var subBuilder = new StringBuilder();
                    subBuilder.Append('#', GetWordCount(subItem.Name));
                    subBuilder.AppendFormat(" [{0}]({1}#{2})", subItem.Name, groupToFilePathMap[group.Name], subItem.HtmlId);
                    TocLines.Add(subBuilder.ToString());
                }
            }
        }

        private List<AzureCliViewModel> GetSubGroups(string groupName, int groupWordCount)
        {
            return CommandGroups.FindAll(c => GetWordCount(c.Name) == groupWordCount + 1 && c.Name.StartsWith(groupName + " "));
        }

        private static int GetWordCount(string name)
        {
            return name.Count(c => c == ' ') + 1;
        }

        private static string GetUid(string commandName)
        {
            return commandName.Replace(' ', '_');
        }

        private string PrepareDocFilePath(string destDirectory, string name)
        {
            var pathSegments = name.Split(' ');
            if (pathSegments.Length == 1)
                return "index" + Options.DocExtension;

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
            foreach(var arg in args)
            {
                var cliArg = new Parameter();
                cliArg.Name = arg.XPathSelectElement("desc_signature/desc_addname").Value;
                var fields = arg.XPathSelectElements("desc_content/field_list/field");
                foreach(var field in fields)
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
                            cliArg.IsRequired = fieldValue;
                            break;
                        case "default":
                            cliArg.DefaultValue = fieldValue;
                            break;
                        case "allowed values":
                            cliArg.ParameterValueGroup = fieldValue;
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

        private string ExtractFieldValue(XElement field)
        {
            var paragraph = field.XPathSelectElement("field_body/paragraph");
            if (paragraph != null)
                return ResolveHyperLink(paragraph);

            var listItems = field.XPathSelectElements("field_body/bullet_list/list_item");
            if(listItems.Count() > 0)
                return string.Join("|", listItems.Select(element => ResolveHyperLink(element)).ToArray());

            throw new ApplicationException(string.Format("UNKNOW field_body:{0}", field.Value));
        }

        private string ResolveHyperLink(XElement element)
        {
            var refs = element.Descendants("reference");
            if (!refs.Any())
                return element.Value;

            foreach(var reference in refs.ToArray())
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
            if(!string.Equals(name, AzGroupName, StringComparison.OrdinalIgnoreCase))
            {
                name = string.Format("{0} {1}", AzGroupName, name);
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
            if (!string.IsNullOrEmpty(docSource))
            {
                if (isGroup)
                {
                    command.Metadata["doc_source_url_repo"] = string.Format("{0}/blob/{1}/", Options.RepoOfSource, Options.Branch);
                    command.Metadata["doc_source_url_path"] = docSource;
                    command.Metadata["original_content_git_url"] = string.Format("{0}/blob/{1}/{2}", Options.RepoOfSource, Options.Branch, docSource);
                    command.Metadata["gitcommit"] = string.Format("{0}/blob/{1}/{2}", Options.RepoOfSource, DocCommitIdMap[docSource].Commit, docSource);

                    var date = DocCommitIdMap[docSource].Date;
                    command.Metadata["updated_at"] = date.ToString();
                    command.Metadata["ms.date"] = date.ToShortDateString();
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
            foreach(var example in examples)
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
    }
}
