namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class InputOutput
    {
        [YamlMember(Alias = "typeName")]
        [JsonProperty("typeName")]
        public string TypeName { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
