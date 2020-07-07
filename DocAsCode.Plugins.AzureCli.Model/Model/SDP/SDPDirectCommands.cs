using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model.Model.SDP
{
    public class SDPDirectCommands
    {
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "examples")]
        public List<SDPExample> Examples { get; set; }

        [YamlMember(Alias = "parameters")]
        public List<SDPParameter> Parameters { get; set; }

        [YamlMember(Alias = "source")]
        public SDPSource Source { get; set; }

        public static SDPDirectCommands FromUniversalItem(AzureCliUniversalItem item)
        {
            if (item == null) return null;

            var result = new SDPDirectCommands()
            {
                Name = item.Name,
                Summary = item.Summary,
                Description = item.Description,
                Examples = item.Examples == null ? null : item.Examples.Select(SDPExample.FromUniversalExample).ToList(),
                Parameters = item.Parameters == null ? null : item.Parameters.Select(SDPParameter.FromUniversalParameter).ToList(),
                Source = SDPSource.FromUniversalSource(item.Source)
            };

            return result;
        }
    }
}
