namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class Option
    {
        [YamlMember(Alias = "isSimplifiedTextLink")]
        [JsonProperty("isSimplifiedTextLink")]
        public string IsSimplifiedTextLink { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "required")]
        [JsonProperty("required")]
        public int Required { get; set; }

        [YamlMember(Alias = "optional")]
        [JsonProperty("optional")]
        public int Optional { get; set; }

        [YamlMember(Alias = "flags")]
        [JsonProperty("flags")]
        public string Flags { get; set; }

        [YamlMember(Alias = "longFlag")]
        [JsonProperty("longFlag")]
        public string LongFlag { get; set; }

        [YamlMember(Alias = "shortFlag")]
        [JsonProperty("shortFlag")]
        public string ShortFlag { get; set; }

        [YamlMember(Alias = "boolValue")]
        [JsonProperty("boolValue")]
        public bool BoolValue { get; set; }
    }
}
