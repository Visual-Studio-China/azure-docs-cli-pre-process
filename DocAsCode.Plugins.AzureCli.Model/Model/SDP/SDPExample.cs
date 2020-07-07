using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model.Model.SDP
{
    public class SDPExample
    {
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "syntax")]
        public string Syntax { get; set; }

        public static SDPExample FromUniversalExample(AzureCliUniversalExample example)
        {
            if (example == null) return null;

            var result = new SDPExample()
            {
                Summary = example.Summary,
                Syntax = example.Syntax?.Content
            };

            return result;
        }
    }
}
