namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AzureCliUniversalSource
    {
        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        [YamlMember(Alias = "path")]
        [JsonProperty("path")]
        public string Path { get; set; }

        [YamlMember(Alias = "remote")]
        [JsonProperty("remote")]
        public AzureCliUniversalRemote Remote { get; set; }

        [YamlMember(Alias = "startLine")]
        [JsonProperty("startLine")]
        public int StartLine { get; set; }
    }
}
