using CommandLine;

namespace AzCliDocPreprocessor
{
    internal class Options
    {
        [Option('e', "ext", Required = false, Default = ".pycliyml")]
        public string DocExtension { get; set; }

        [Option('t', "toc", Required = false, Default = "toc.md")]
        public string TocFileName { get; set; }

        [Option('s', "source", Required = true)]
        public string SourceXmlPath { get; set; }
                
        [Option('d', "dest", Required = true)]
        public string DestDirectory { get; set; }

        [Option('i', "ignore", Required = false, Default = true, HelpText = "Whether ignore unknown property in xml")]
        public bool IgnoreUnknownProperty { get; set; }
    }
}
