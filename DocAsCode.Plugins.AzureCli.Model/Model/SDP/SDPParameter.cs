using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model.Model.SDP
{
    public class SDPParameter
    {
        [YamlMember(Alias = "isRequired")]
        public bool IsRequired { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "defaultValue")]
        public string DefaultValue { get; set; }

        [YamlMember(Alias = "parameterValueGroup")]
        public string ParameterValueGroup { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "valueFrom")]
        public string ValueFrom { get; set; }

        public static SDPParameter FromUniversalParameter(AzureCliUniversalParameter parameter)
        {
            if (parameter == null) return null;

            var result = new SDPParameter()
            {
                IsRequired = parameter.IsRequired,
                Name = parameter.Name,
                DefaultValue = parameter.DefaultValue,
                ParameterValueGroup = parameter.ParameterValueGroup,
                Summary = parameter.Summary,
                Description = parameter.Description,
                ValueFrom = parameter.ValueFrom
            };

            return result;
        }
    }
}
