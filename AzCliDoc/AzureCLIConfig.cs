using Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzCliDocPreprocessor
{
    public class AzureCLIConfig
    {
        public string ExtensionInformationTemplate { get; set; }
        public Dictionary<string, TocTitleMappings> TitleMapping { get; set; }
        public List<AzureCliUniversalParameter> GlobalParameters { get; set; }
    }
}
