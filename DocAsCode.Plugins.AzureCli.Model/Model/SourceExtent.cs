namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class SourceExtent
    {
        [YamlMember(Alias = "line")]
        [JsonProperty("line")]
        public SourceRange Line { get; set; }

        [YamlMember(Alias = "column")]
        [JsonProperty("column")]
        public SourceRange Column { get; set; }

        [YamlMember(Alias = "offset")]
        [JsonProperty("offset")]
        public SourceRange OffSet { get; set; }

        [YamlMember(Alias = "originalText")]
        [JsonProperty("originalText")]
        public string OriginalText { get; set; }
    }
}
