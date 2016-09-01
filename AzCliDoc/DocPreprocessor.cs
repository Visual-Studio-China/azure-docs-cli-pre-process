using Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model;
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
        private const string FormalAzGroupName = "Azure";
        private List<AzureCliViewModel> CommandGroups { get; set; }
        private Dictionary<string, AzureCliViewModel> NameCommandGroupMap { get; set; }
        private Options Options { get; set; }

        public bool Run(Options options)
        {
            Options = options;
            Initialize();

            var xDoc = XDocument.Load(Options.SourceXmlPath);
            var groups = xDoc.Root.XPathSelectElements("desc[@desctype='cligroup']");
            foreach(var group in groups)
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

            Save(Options.DestDirectory);
            return true;
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

            CommandGroups = new List<AzureCliViewModel>();
            NameCommandGroupMap = new Dictionary<string, AzureCliViewModel>();
        }

        private void Save(string destDirectory)
        {
            if (!Directory.Exists(Options.DestDirectory))
            {
                Directory.CreateDirectory(Options.DestDirectory);
            }

            CommandGroups.Sort((group1, group2) => string.CompareOrdinal(group1.Name, group2.Name));

            using (var tocWriter = new StreamWriter(Path.Combine(destDirectory, Options.TocFileName), false))
            {
                var serializer = new Serializer();
                foreach (var group in CommandGroups)
                {
                    var relativeDocPath = PrepareDocFilePath(destDirectory, group.Name);
                    using (var writer = new StreamWriter(Path.Combine(destDirectory, relativeDocPath), false))
                    {
                        serializer.Serialize(writer, group);
                    }

                    var builder = new StringBuilder();
                    builder.Append('#', group.Name.Count(c => c == ' ') + 1);
                    string tocName = string.Equals(group.Name, AzGroupName, StringComparison.OrdinalIgnoreCase) ? FormalAzGroupName : group.Name;
                    builder.AppendFormat(" [{0}]({1})", tocName, relativeDocPath);
                    tocWriter.WriteLine(builder.ToString());
                }
            }
        }

        private string PrepareDocFilePath(string destDirectory, string name)
        {
            var pathSegments = name.Split(new[] { ' ' });
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
                            cliArg.IsRequired = Boolean.Parse(fieldValue);
                            break;
                        case "default":
                            cliArg.DefaultValue = fieldValue;
                            break;
                        case "allowed values":
                            cliArg.ParameterValueGroup = new List<string>(new string[]{ fieldValue });
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
                var newRef = XElement.Parse("<a href=''></a>");
                newRef.Attribute("href").Value = reference.Attribute("refuri")?.Value;
                newRef.Value = reference.Value;
                reference.ReplaceWith(newRef);
            }
            return String.Concat(element.Nodes());
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
            command.Uid = name.Replace(" ", "_");
            command.Summary = summary;
            command.Description = description;
            if(!string.IsNullOrEmpty(docSource))
            {
                command.Metadata["doc_source_url_repo"] = Options.RepoOfSource;
                command.Metadata["doc_source_url_path"] = docSource;
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
