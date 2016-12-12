namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class Link
    {
        [YamlMember(Alias = "isSimplifiedTextLink")]
        [JsonProperty("isSimplifiedTextLink")]
        public bool IsSimplifiedTextLink { get; set; }

        [YamlMember(Alias = "linkName")]
        [JsonProperty("linkName")]
        public string LinkName { get; set; }

        [YamlMember(Alias = "linkUri")]
        [JsonProperty("linkUri")]
        public string LinkUri { get; set; }
    }
}
