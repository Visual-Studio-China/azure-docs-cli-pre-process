namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class SourceRange
    {
        [YamlMember(Alias = "start")]
        [JsonProperty("start")]
        public int Start { get; set; }

        [YamlMember(Alias = "end")]
        [JsonProperty("end")]
        public int End { get; set; }
    }
}
