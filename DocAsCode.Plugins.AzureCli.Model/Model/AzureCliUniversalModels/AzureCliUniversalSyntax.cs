namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AzureCliUniversalSyntax
    {
        [YamlMember(Alias = "content")]
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
