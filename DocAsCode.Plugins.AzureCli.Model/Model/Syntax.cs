namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class Syntax
    {
        [YamlMember(Alias = "parameterSetName")]
        [JsonProperty("parameterSetName")]
        public string ParameterSetName { get; set; }

        [YamlMember(Alias = "isDefault")]
        [JsonProperty("isDefault")]
        public bool IsDefault { get; set; }
        
        [YamlMember(Alias = "code")]
        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
