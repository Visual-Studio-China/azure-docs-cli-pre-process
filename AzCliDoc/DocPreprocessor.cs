using Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
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

            var azGroup = new AzureCliViewModel()
            {
                Children = new List<AzureCliViewModel>()
            };
            azGroup.Name = "az";
            azGroup.Uid = "az";
            CommandGroups.Add(azGroup);
            NameCommandGroupMap["az"] = azGroup;
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
                    var docFileName = group.Uid + Options.DocExtension;
                    using (var writer = new StreamWriter(Path.Combine(destDirectory, docFileName), false))
                    {
                        serializer.Serialize(writer, group);
                    }

                    var builder = new StringBuilder();
                    builder.Append('#', group.Name.Count(c => c == ' ') + 1);
                    builder.AppendFormat(" [{0}]({1})", group.Name, docFileName);
                    tocWriter.WriteLine(builder.ToString());
                }
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
                    var fieldValue = field.XPathSelectElement("field_body/paragraph").Value;
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

        private AzureCliViewModel ExtractCommand(XElement xElement, bool isGroup)
        {
            var name = "az " + xElement.XPathSelectElement("desc_signature/desc_addname").Value;
            var id = name.Replace(' ', '_');
            var fields = xElement.XPathSelectElements("desc_content/field_list/field");
            var summary = from field in fields
                          where string.Equals(field.Element("field_name").Value, "Summary", StringComparison.OrdinalIgnoreCase)
                          select field.XPathSelectElement("field_body/paragraph").Value;
            var description = from field in fields
                              where string.Equals(field.Element("field_name").Value, "Description", StringComparison.OrdinalIgnoreCase)
                              select field.XPathSelectElement("field_body/paragraph").Value;
            AzureCliViewModel command = new AzureCliViewModel()
            {
                Examples = new List<Example>()
            };

            command.Name = name;
            command.Uid = id;
            command.Summary = summary.FirstOrDefault();
            command.Description = description.FirstOrDefault();

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
    }
}
