using CommandLine;
using CommandLine.Text;
using Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli;

namespace AzCliDocPreprocessor
{
    internal class Options
    {
        [Option('e', "ext", Required = false, DefaultValue = ".yml")]
        public string DocExtension { get; set; }

        [Option('t', "toc", Required = false, DefaultValue = "TOC.md")]
        public string TocFileName { get; set; }

        [Option('s', "source", Required = true)]
        public string SourceXmlPath { get; set; }

        [Option('x', "extension", Required = false)]
        public string ExtensionXmlPath { get; set; }

        [Option('d', "dest", Required = true)]
        public string DestDirectory { get; set; }

        [Option('r', "repo", Required = false, DefaultValue = "https://github.com/Azure/azure-cli")]
        public string RepoOfSource{ get; set; }

        [Option('b', "branch", Required = false, DefaultValue = "master")]
        public string Branch { get; set; }

        [Option('c', "commitFile", Required =false)]
        public string DocCommitMapFile { get; set; }

        [Option('i', "ignore", Required = false, DefaultValue = true, HelpText = "Whether ignore unknown property in xml")]
        public bool IgnoreUnknownProperty { get; set; }

        [Option('v', "version", Required = false, DefaultValue = 0)]
        public int Version { get; set; }

        [Option('f', "filter", Required = false)]
        public string CommandFilter { get; set; }

        [Option("extInfo", Required = false)]
        public string ExtensionInformationFile { get; set; }

        [Option("config", Required = false)]
        public string AzureCLIConfigFile { get; set; }

        [Option('g', "group", Required = false, DefaultValue = CommandGroupType.AZURE)]
        public CommandGroupType GroupName { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
