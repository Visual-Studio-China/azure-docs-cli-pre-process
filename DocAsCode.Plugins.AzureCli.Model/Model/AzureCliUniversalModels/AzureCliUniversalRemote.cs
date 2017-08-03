namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AzureCliUniversalRemote
    {
        [YamlMember(Alias = "branch")]
        [JsonProperty("branch")]
        public string Branch { get; set; }

        [YamlMember(Alias = "path")]
        [JsonProperty("path")]
        public string Path { get; set; }

        [YamlMember(Alias = "repo")]
        [JsonProperty("repo")]
        public string Repo { get; set; }
    }
}
