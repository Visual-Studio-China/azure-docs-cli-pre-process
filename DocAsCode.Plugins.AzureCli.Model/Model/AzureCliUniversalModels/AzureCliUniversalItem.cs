namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AzureCliUniversalItem
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "langs")]
        [JsonProperty("langs")]
        public List<string> Langs { get; set; } = new List<string>();

        [YamlMember(Alias = "children")]
        [JsonProperty("children")]
        public List<string> Children { get; set; }

        [YamlMember(Alias = "examples")]
        [JsonProperty("examples")]
        public List<AzureCliUniversalExample> Examples { get; set; }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<AzureCliUniversalParameter> Parameters { get; set; }

        [YamlMember(Alias = "source")]
        [JsonProperty("source")]
        public AzureCliUniversalSource Source { get; set; }
    }
}
