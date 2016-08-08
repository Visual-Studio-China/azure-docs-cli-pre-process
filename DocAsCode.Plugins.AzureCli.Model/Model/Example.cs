namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class Example
    {
        [YamlMember(Alias = "title")]
        [JsonProperty("title")]
        public string Title { get; set; }

        [YamlMember(Alias = "code")]
        [JsonProperty("code")]
        public string Code { get; set; }

        [YamlMember(Alias = "remarks")]
        [JsonProperty("remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = "introduction")]
        [JsonProperty("introduction")]
        public string Introduction { get; set; }
    }
}
