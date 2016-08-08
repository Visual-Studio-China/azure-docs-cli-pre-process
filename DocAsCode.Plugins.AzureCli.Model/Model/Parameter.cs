namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class Parameter
    {
        [YamlMember(Alias = "extent")]
        [JsonProperty("extent")]
        public SourceExtent Extent { get; set; }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public string Type { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "isRequired")]
        [JsonProperty("isRequired")]
        public bool IsRequired { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "defaultValue")]
        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        [YamlMember(Alias = "variableLength")]
        [JsonProperty("variableLength")]
        public bool VariableLength { get; set; }

        [YamlMember(Alias = "acceptWildcardCharacters")]
        [JsonProperty("acceptWildcardCharacters")]
        public bool AcceptWildcardCharacters { get; set; }

        [YamlMember(Alias = "pipelineInput ")]
        [JsonProperty("pipelineInput")]
        public string PipelineInput { get; set; }

        [YamlMember(Alias = "position")]
        [JsonProperty("position")]
        public string Position { get; set; }

        [YamlMember(Alias = "aliases")]
        [JsonProperty("aliases")]
        public string[] Aliases { get; set; }

        [YamlMember(Alias = "isValueRequired")]
        [JsonProperty("isValueRequired")]
        public bool IsValueRequired { get; set; }

        [YamlMember(Alias = "valueVariableLength")]
        [JsonProperty("valueVariableLength")]
        public bool ValueVariableLength { get; set; }

        [YamlMember(Alias = "parameterValueGroup")]
        [JsonProperty("parameterValueGroup")]
        public List<string> ParameterValueGroup { get; set; }

        [YamlMember(Alias = "valueFrom")]
        [JsonProperty("valueFrom")]
        public string ValueFrom { get; set; }
    }
}
