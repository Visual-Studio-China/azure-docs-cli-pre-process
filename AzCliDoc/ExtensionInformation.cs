using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzCliDocPreprocessor
{
    public class ExtensionsInformation
    {
        [JsonProperty(PropertyName = "extensions")]
        public Dictionary<string, ExtensionInformation[]> Extensions { get; set; }
    }

    public class ExtensionInformation
    {
        [JsonProperty(PropertyName = "metadata")]
        public ExtensionMetadata Metadata;
    }

    public class ExtensionMetadata
    {
        [JsonProperty(PropertyName = "azext.isExperimental")]
        public bool IsExperimental { get; set; }

        [JsonProperty(PropertyName = "azext.minCliCoreVersion")]
        public string MinCliCoreVersion { get; set; }
    }
}
