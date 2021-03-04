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
using Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model.Model.SDP;
using System.Net;
using System.Text.RegularExpressions;

namespace AzCliDocPreprocessor
{
    internal class DocPreprocessor
    {
        private const string FormalAzGroupName = "Reference";
        private const string YamlMimeProcessor = "### YamlMime:AzureCLIGroup";
        private const string AutoGenFolderName = "docs-ref-autogen";
        private const string ServicePageFolderName = "service-page";
        private const string ReferenceIndexFileName = "reference-index";
        private const string ExtensionReferenceIndexFileName = "index";
        private const string ExtensionGlobalPrefix = "ext";
        private readonly Regex TagRegex = new Regex(@"(?!<a)<[\w-]+>");

        // from config
        private AzureCLIConfig AzureCLIConfig { get; set; }
        private ExtensionsInformation ExtensionsInformation { get; set; }
        private List<AzureCliUniversalParameter> GlobalParameters { get; set; } = new List<AzureCliUniversalParameter>();
        private Dictionary<string, TocTitleMappings> TitleMappings { get; set; } = new Dictionary<string, TocTitleMappings>();

        // for each run of ind.xml
        private bool IsExtensionXml { get; set; }
        private string ExtensionXmlFolder { get; set; }
        private ExtensionInformation ExtensionInformation { get; set; }
        private string ExtensionInformationString { get; set; }
        private Options Options { get; set; }
        private CommandGroupConfiguration CommandGroupConfiguration { get; set; }
        private List<AzureCliViewModel> CommandGroups { get; set; } = new List<AzureCliViewModel>();
        private List<string> SourceXmlPathSet = new List<string>();
        private List<string> ExtensionXmlPathSet = new List<string>();
        private Dictionary<string, CommitInfo> DocCommitIdMap { get; set; } = new Dictionary<string, CommitInfo>();
        private Dictionary<string, AzureCliViewModel> NameCommandGroupMap { get; set; } = new Dictionary<string, AzureCliViewModel>();
        private Dictionary<string, StringBuilder> TocFileContent { get; set; } = new Dictionary<string, StringBuilder>();
        private Dictionary<string, AzureCliUniversalViewModel> UniversalCommandGroups { get; set; } = new Dictionary<string, AzureCliUniversalViewModel>();

        // all the data
        // key => moniker
        // Notice: if this is azsphere, the key is just the folder name, not moniker
        private Dictionary<string, SDPCLIGroup[]> CoreSDPGroups { get; set; } = new Dictionary<string, SDPCLIGroup[]>();
        // key => extension name
        private Dictionary<string, SDPCLIGroup[]> ExtensionSDPGroups { get; set; } = new Dictionary<string, SDPCLIGroup[]>();

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

                if (AzureCLIConfig?.ExtensionInformationTemplate != null && ExtensionsInformation != null)
                {
                    ExtensionInformation = ExtensionsInformation.Extensions[ExtensionXmlFolder].Last();
                    ExtensionInformationString = AzureCLIConfig.ExtensionInformationTemplate
                        .Replace("{EXTENSION_NAME}", ExtensionXmlFolder)
                        .Replace("{MIN_CORE_VERSION}", ExtensionInformation.Metadata.MinCliCoreVersion);
                }
                else ExtensionInformationString = null;

                ProccessOneXml(extensionXmlPath);
            }

            if(Options.GroupName == CommandGroupType.AZSPHERE)
            {
                OrganizeAndSaveAzSphere();
            }
            else
            {
                OrganizeAndSave();
            }

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
                bool isTopGroup = string.Equals(commandGroup.Name, CommandGroupConfiguration.CommandPrefix, StringComparison.OrdinalIgnoreCase);
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

            SaveToSDPData(oneXmlPath);
        }

        private void OrganizeAndSave()
        {
            foreach (var kvp in CoreSDPGroups)
            {
                // Merge core and extension groups
                Dictionary<string, SDPCLIGroup> AllGroups = kvp.Value.ToDictionary(group => group.Name);
                MergeIntoAllGroups(AllGroups, ExtensionSDPGroups);

                // Generate service pages
                Dictionary<string, SDPCLIGroup> AllServicePages = new Dictionary<string, SDPCLIGroup>();
                AllServicePages = AzureCLIConfig.ServicePages.ToDictionary(
                    sp => sp.Name,
                    sp => new SDPCLIGroup()
                    {
                        Uid = GetUid(sp.Name, true),
                        Name = sp.Title ?? sp.Name,
                        Summary = sp.Summary,
                        Commands = sp.IsFullListPage ?
                            GetChildGroups(CommandGroupConfiguration.CommandPrefix, AllGroups).Select(g => g.Uid).ToList() :
                            sp.CommandGroups.Select(g => GetUid(g)).ToList()
                    });

                // Prepare toc
                var toc = new List<AzureCliUniversalTOC>();
                var root = new AzureCliUniversalTOC()
                {
                    name = "Reference",
                    items = new List<AzureCliUniversalTOC>()
                };
                toc.Add(root);

                // Create Service Pages TOC
                var serviceToc = AzureCLIConfig.ServicePages.Select(sp => new AzureCliUniversalTOC()
                {
                    name = sp.Name,
                    uid = GetUid(sp.Name, true),
                    items = sp.IsFullListPage ?
                        null :
                        sp.CommandGroups.Select(groupName => GroupToToc(groupName, AllGroups)).OfType<AzureCliUniversalTOC>().ToList()
                });
                root.items.AddRange(serviceToc);

                HandleAllDualPuposeTocNode(toc);

                // Write to yamls
                var destDirectory = Path.Combine(Options.DestDirectory, kvp.Key, AutoGenFolderName);
                foreach (var commandGroup in AllGroups)
                {
                    var relativeDocPath = PrepareDocFilePath(destDirectory, commandGroup.Key);
                    using (var writer = new StreamWriter(Path.Combine(destDirectory, relativeDocPath), false))
                    {
                        writer.WriteLine(YamlMimeProcessor);
                        if (TitleMappings.TryGetValue(commandGroup.Key, out var mapping))
                        {
                            // Note: MemberwiseClone SDPCLIGroup here in case the update affects next iteration/moniker
                            YamlUtility.Serialize(writer, commandGroup.Value.ShallowCopyWithName(mapping.PageTitle ?? mapping.TocTitle ?? commandGroup.Key));
                        }
                        else
                        {
                            YamlUtility.Serialize(writer, commandGroup.Value);
                        }
                    }
                }

                var servicePageDestDirectory = Path.Combine(Options.DestDirectory, kvp.Key, AutoGenFolderName, ServicePageFolderName);
                CreateDirectoryIfNotExist(servicePageDestDirectory);
                foreach (var servicePage in AllServicePages)
                {
                    using (var writer = new StreamWriter(Path.Combine(servicePageDestDirectory, servicePage.Key + ".yml"), false))
                    {
                        writer.WriteLine(YamlMimeProcessor);
                        YamlUtility.Serialize(writer, servicePage.Value);
                    }
                }

                using (var writer = new StreamWriter(Path.Combine(destDirectory, "TOC.yml"), false))
                {
                    YamlUtility.Serialize(writer, toc);
                }
            }
        }

        private void OrganizeAndSaveAzSphere()
        {
            // For azsphere, key is not moniker, just folder
            // Merge groups (should bonly happen for the root group: azsphere)
            var AllGroups = new Dictionary<string, SDPCLIGroup>();
            MergeIntoAllGroups(AllGroups, CoreSDPGroups);

            foreach (var group in AllGroups)
            {
                // azsphere feature 1: sort commands
                group.Value.Commands = group.Value.Commands.OrderBy(command => command).ToList();
                group.Value.DirectCommands = group.Value.DirectCommands.OrderBy(command => command.Name).ToList();
                // azsphere feature 2: escape some tag
                EscapeTagInGroup(group.Value);
            }

            // Prepare toc, no service pages, just azsphere as root
            var toc = new List<AzureCliUniversalTOC>();
            var root = new AzureCliUniversalTOC()
            {
                name = "Reference",
                uid = GetUid(CommandGroupConfiguration.CommandPrefix),
                items = GetChildGroups(CommandGroupConfiguration.CommandPrefix, AllGroups)
                    .Select(group => GroupToToc(group.Name, AllGroups))
                    .OrderBy(t => t.name)
                    .OfType<AzureCliUniversalTOC>().ToList()
            };
            toc.Add(root);
            HandleAllDualPuposeTocNode(toc);

            // Write command group page
            var destDirectory = Path.Combine(Options.DestDirectory, AutoGenFolderName);
            foreach (var commandGroup in AllGroups)
            {
                var relativeDocPath = PrepareDocFilePath(destDirectory, commandGroup.Key);
                using (var writer = new StreamWriter(Path.Combine(destDirectory, relativeDocPath), false))
                {
                    writer.WriteLine(YamlMimeProcessor);
                    YamlUtility.Serialize(writer, commandGroup.Value);
                }
            }

            // Write toc
            using (var writer = new StreamWriter(Path.Combine(destDirectory, "TOC.yml"), false))
            {
                YamlUtility.Serialize(writer, toc);
            }
        }

        private void EscapeTagInGroup(SDPCLIGroup group)
        {
            group.Summary = EscapeTagInString(group.Summary);
            group.Description = EscapeTagInString(group.Description);
            foreach (var directCommand in group.DirectCommands)
            {
                directCommand.Summary = EscapeTagInString(directCommand.Summary);
                directCommand.Description = EscapeTagInString(directCommand.Description);

                if (directCommand.Examples != null)
                {
                    foreach (var example in directCommand.Examples)
                    {
                        example.Summary = EscapeTagInString(example.Summary);
                    }
                }

                if (directCommand.RequiredParameters != null)
                {
                    foreach (var requiredParameter in directCommand.RequiredParameters)
                    {
                        requiredParameter.Summary = EscapeTagInString(requiredParameter.Summary);
                        requiredParameter.Description = EscapeTagInString(requiredParameter.Description);
                    }
                }

                if (directCommand.OptionalParameters != null)
                {
                    foreach (var optionalParameter in directCommand.OptionalParameters)
                    {
                        optionalParameter.Summary = EscapeTagInString(optionalParameter.Summary);
                        optionalParameter.Description = EscapeTagInString(optionalParameter.Description);
                    }
                }
            }
        }

        private string EscapeTagInString(string content)
        {
            return TagRegex.Replace(content, @"\$&");
        }

        private void MergeIntoAllGroups(Dictionary<string, SDPCLIGroup> allGroups, Dictionary<string, SDPCLIGroup[]> groups)
        {
            // Merge groups, no need to merge commands
            foreach (var kvp in groups)
            {
                foreach (var newGroup in kvp.Value)
                {
                    if (allGroups.ContainsKey(newGroup.Name))
                    {
                        var group = allGroups[newGroup.Name];

                        // keep group.Uid (should be the same)
                        // keep group.Name (should be the same)
                        // ignore extGroup.ExtensionInformation (group is introduced by cli core, just some sub groups/commands from extension)
                        // keep group.Summary (TBD)
                        // keep group.Description (TBD)
                        // merge DirectCommands
                        if (newGroup.DirectCommands?.Count > 0)
                        {
                            if (group.DirectCommands?.Count > 0)
                            {
                                group.DirectCommands = group.DirectCommands.Union(newGroup.DirectCommands, DirectCommandsComparer._default).ToList();
                            }
                            else
                            {
                                group.DirectCommands = newGroup.DirectCommands;
                            }
                        }
                        // merge Commands
                        if (newGroup.Commands?.Count > 0)
                        {
                            if (group.Commands?.Count > 0)
                            {
                                group.Commands = group.Commands.Union(newGroup.Commands).ToList();
                            }
                            else
                            {
                                group.Commands = newGroup.Commands;
                            }
                        }
                        // keep group.GlobalParameters (should be the same)
                        // keep group.Metadata (TBD)
                    }
                    else
                    {
                        allGroups.Add(newGroup.Name, newGroup);
                    }
                }
            }
        }

        private AzureCliUniversalTOC GroupToToc(string name, Dictionary<string, SDPCLIGroup> AllGroups)
        {
            if (!AllGroups.ContainsKey(name))
            {
                return null;
            }

            var group = AllGroups[name];
            var result = new AzureCliUniversalTOC()
            {
                name = TitleMappings.ContainsKey(name) ? TitleMappings[name].TocTitle : GetLastGroupName(group.Name),
                uid = group.Uid,
                items = new List<AzureCliUniversalTOC>()
            };

            // group, sort from A - Z
            var tocChildGroups = GetChildGroups(group.Name, AllGroups);

            result.items.AddRange(tocChildGroups.OrderBy(g => GetLastGroupName(g.Name))
                .Select(g => GroupToToc(g.Name, AllGroups))
                .OfType<AzureCliUniversalTOC>());

            return result;
        }

        private List<SDPCLIGroup> GetChildGroups(string parentGroupName, Dictionary<string, SDPCLIGroup> AllGroups)
        {
            var groupWordCount = GetWordCount(parentGroupName);
            return AllGroups.Values.Where(c => GetWordCount(c.Name) == groupWordCount + 1 && c.Name.StartsWith(parentGroupName + " ")).ToList();
        }

        /// <summary>
        /// https://ceapex.visualstudio.com/Engineering/_workitems/edit/225978
        /// </summary>
        /// <param name="nodes"></param>
        private void HandleAllDualPuposeTocNode(List<AzureCliUniversalTOC> nodes)
        {
            if (nodes == null) return;

            foreach (var node in nodes)
            {
                HandleOneDualPuposeTocNode(node);
                HandleAllDualPuposeTocNode(node.items);
            }
        }

        private void HandleOneDualPuposeTocNode(AzureCliUniversalTOC node)
        {
            if (!string.IsNullOrEmpty(node.uid) && node.items != null && node.items.Count() > 0)
            {
                node.items.Insert(0, new AzureCliUniversalTOC()
                {
                    name = "Summary",
                    uid = node.uid
                });
                node.uid = null;
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

            if (string.IsNullOrEmpty(Options.SourceXmlPath) && Options.GroupName != CommandGroupType.AZSPHERE)
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

            AzureCLIConfig = GetAzureCLIConfig();
            ExtensionsInformation = GetExtensionsInformation();
            GlobalParameters = AzureCLIConfig?.GlobalParameters ?? GetGlobalParameters();
            TitleMappings = AzureCLIConfig?.TitleMapping ?? GetTitleMappings();
            CommandGroupConfiguration = ParserCommandGroupConfiguration(Options.GroupName);
        }

        private void SaveToSDPData(string xmlPath)
        {
            if (IsExtensionXml)
            {
                var extensionName = Path.GetFileName(Path.GetDirectoryName(xmlPath));
                var groups = UniversalCommandGroups.Values.Select(udp => {
                    PrepareMetaData(udp);
                    var group = SDPCLIGroup.FromUniversalModel(udp);
                    group.ExtensionInformation = ExtensionInformationString?.Replace("{COMMAND_GROUP}", group.Name);
                    return group;
                }).ToArray();
                ExtensionSDPGroups.Add(extensionName, groups);
            }
            else
            {
                var moniker = Path.GetFileName(Path.GetDirectoryName(xmlPath));
                var groups = UniversalCommandGroups.Values.Select(udp => {
                    PrepareMetaData(udp);
                    var group = SDPCLIGroup.FromUniversalModel(udp);
                    return group;
                }).ToArray();
                CoreSDPGroups.Add(moniker, groups);
            }
        }

        private void PrepareMetaData(AzureCliUniversalViewModel commandGroup)
        {
            var metadata = commandGroup.Metadata;
            var root = commandGroup.Items.First();
            if (!metadata.ContainsKey("description") || string.IsNullOrEmpty(metadata["description"] as string))
            {
                var description = string.IsNullOrEmpty(root.Description) ? root.Summary : root.Description;
                if (!string.IsNullOrEmpty(description))
                {
                    metadata["description"] = description;
                }
            }
        }

        private List<AzureCliViewModel> GetSubGroups(string groupName, int groupWordCount)
        {
            return CommandGroups.FindAll(c => GetWordCount(c.Name) == groupWordCount + 1 && c.Name.StartsWith(groupName + " "));
        }

        private static string GetLastGroupName(string name)
        {
            return name.Split(' ').Last();
        }

        private static int GetWordCount(string name)
        {
            return name.Count(c => c == ' ') + 1;
        }

        private string GetUid(string commandName, bool isServicePage = false)
        {
            if (isServicePage) commandName = "sp-" + commandName;

            string raw = commandName.Replace(' ', '_');
            return raw;
        }

        private string PrepareDocFilePath(string destDirectory, string name)
        {
            var pathSegments = name.Split(' ');
            if (pathSegments.Length == 1)
            {
                return ReferenceIndexFileName + Options.DocExtension;
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
                    var fieldValue = WebUtility.HtmlDecode(ExtractFieldValue(field));
                    var fieldName = field.Element("field_name").Value.ToLower();
                    switch (fieldName)
                    {
                        case "summary":
                            cliArg.Summary = EscapeAsterisk(fieldValue);
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
                return ResolveHyperLink(DedentInnerXML(paragraph));

            var listItems = field.XPathSelectElements("field_body/bullet_list/list_item");
            if (listItems.Count() > 0)
                return string.Join("|", listItems.Select(element => ResolveHyperLink(element)).ToArray());

            throw new ApplicationException(string.Format("UNKNOW field_body:{0}", field.Value));
        }

        /// <summary>
        /// for issue: https://github.com/Azure/azure-cli/issues/4869
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string EscapeAsterisk(string content)
        {
            return content.Replace("*", "\\*");
        }

        /// <summary>
        /// for the issue: https://github.com/MicrosoftDocs/azure-docs-cli/issues/1569
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private XElement DedentInnerXML(XElement element)
        {
            var content = element.Nodes().Aggregate("", (b, node) => b += node.ToString());
            var depth = element.AncestorsAndSelf().Count();
            var lines = content.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if(line.Length - line.TrimStart(' ').Length >= depth * 4)
                {
                    lines[i] = line.Substring(depth * 4);
                }
                else
                {
                    // could this happen?
                    return element;
                }
            }

            var replaceContent = string.Join("\n", lines);
            element.ReplaceNodes(XElement.Parse($"<temproot>{replaceContent}</temproot>").Nodes());

            return element;
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

            // node.ToString() use XmlWriterSettings.NewLineHandling, so it is "\r\n".
            // But yamlserilizer will make it to 2 newlines, eg. "a\r\n b" to "a\n\n b"
            // Just replace "\r\n" with "\n" for work around
            var value = String.Concat(element.Nodes().Select(node => node.ToString().Replace("\r\n", "\n")));
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
            var summary = WebUtility.HtmlDecode(ExtractFieldValueByName(xElement, "Summary"));
            var description = WebUtility.HtmlDecode(ExtractFieldValueByName(xElement, "Description"));
            var docSource = WebUtility.HtmlDecode((ExtractFieldValueByName(xElement, "Doc Source", false)));

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
                cliExample.Code = DedentInnerXML(example.XPathSelectElement("desc_content/paragraph")).Value;
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

            DedentInnerXML(element);
            return resolveHyperLink ? ResolveHyperLink(element) : element.Value;
        }

        private AzureCLIConfig GetAzureCLIConfig()
        {
            if (Options.AzureCLIConfigFile != null)
            {
                return new JavaScriptSerializer().Deserialize<AzureCLIConfig>(new StreamReader(Options.AzureCLIConfigFile).ReadToEnd());
            }
            return null;
        }

        private ExtensionsInformation GetExtensionsInformation()
        {
            if(Options.ExtensionInformationFile != null)
            {
                return JsonConvert.DeserializeObject<ExtensionsInformation>(new StreamReader(Options.ExtensionInformationFile).ReadToEnd());
            }
            return null;
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
                case CommandGroupType.AZSPHERE:
                    return new JavaScriptSerializer().Deserialize<CommandGroupConfiguration>(new StreamReader("./data/AzSphereCliConfiguration.json").ReadToEnd());
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

    class DirectCommandsComparer : IEqualityComparer<SDPDirectCommands>
    {
        public static DirectCommandsComparer _default = new DirectCommandsComparer();

        public bool Equals(SDPDirectCommands x, SDPDirectCommands y) => x.Name == y.Name;

        public int GetHashCode(SDPDirectCommands obj) => obj.Name.GetHashCode();
    }
}