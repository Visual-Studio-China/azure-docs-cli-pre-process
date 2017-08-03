namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AzureCliUniversalExample
    {
        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "syntax")]
        [JsonProperty("syntax")]
        public AzureCliUniversalSyntax Syntax { get; set; }
    }
}
