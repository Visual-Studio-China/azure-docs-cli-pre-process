using CommandLine;
using CommandLine.Text;

namespace AzCliDocPreprocessor
{
    internal class Options
    {
        [Option('e', "ext", Required = false, DefaultValue = ".pycliyml")]
        public string DocExtension { get; set; }

        [Option('t', "toc", Required = false, DefaultValue = "refTOC.md")]
        public string TocFileName { get; set; }

        [Option('s', "source", Required = true)]
        public string SourceXmlPath { get; set; }
                
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

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
