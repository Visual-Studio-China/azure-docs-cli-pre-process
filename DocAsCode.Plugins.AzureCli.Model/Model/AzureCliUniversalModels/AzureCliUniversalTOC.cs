namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AzureCliUniversalTOC
    {
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string name { get; set; }

        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string uid { get; set; }

        [YamlMember(Alias = "items")]
        [JsonProperty("items")]
        public List<AzureCliUniversalTOC> items { get; set; }
    }
}
