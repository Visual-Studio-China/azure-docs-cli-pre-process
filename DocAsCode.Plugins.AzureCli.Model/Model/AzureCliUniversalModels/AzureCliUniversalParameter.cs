namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AzureCliUniversalParameter
    {
        [YamlMember(Alias = "isRequired")]
        [JsonProperty("isRequired")]
        public bool IsRequired { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "defaultValue")]
        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        [YamlMember(Alias = "parameterValueGroup")]
        [JsonProperty("parameterValueGroup")]
        public string ParameterValueGroup { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "valueFrom")]
        [JsonProperty("valueFrom")]
        public string ValueFrom { get; set; }

        public bool Equals(AzureCliUniversalParameter other)
        {
            if (null == other)
                return false;
            return string.Equals(this.Name ?? "", other.Name ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class AzureCliUniversalParameterComparer : IEqualityComparer<AzureCliUniversalParameter>
    {
        public int GetHashCode(AzureCliUniversalParameter parameter)
        {
            if (null == parameter)
                return 0;

            return (parameter.Name ?? "").GetHashCode();
        }

        public bool Equals(AzureCliUniversalParameter param_1, AzureCliUniversalParameter param_2)
        {
            if (object.ReferenceEquals(param_1, param_2))
                return true;
            if (object.ReferenceEquals(param_1, null) || object.ReferenceEquals(param_2, null))
                return false;
            return (param_1.Name ?? "").ToLower().Trim() == (param_2.Name ?? "").ToLower().Trim();
        }
    }
}
