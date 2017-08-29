namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.YamlSerialization;
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AzureCliUniversalViewModel
    {
        [YamlMember(Alias = "items")]
        [JsonProperty("items")]
        public List<AzureCliUniversalItem> Items { get; set; } = new List<AzureCliUniversalItem>();

        [YamlMember(Alias = "commands")]
        [JsonProperty("commands")]
        public List<AzureCliUniversalCommand> Commands { get; set; } = new List<AzureCliUniversalCommand>();

        [YamlMember(Alias = "globalParameters")]
        [JsonProperty("globalParameters")]
        public List<AzureCliUniversalParameter> GlobalParameters { get; set; } = new List<AzureCliUniversalParameter>();

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}