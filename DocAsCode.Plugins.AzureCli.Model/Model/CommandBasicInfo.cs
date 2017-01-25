namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class CommandBasicInfo
    {
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string HyperLink { get; set; }

        [YamlMember(Alias = "isGroup")]
        [JsonProperty("isGroup")]
        public bool IsGroup { get; set; }
    }
}
