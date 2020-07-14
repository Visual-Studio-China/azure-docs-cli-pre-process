using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model.Model.SDP
{
    public class SDPCLIGroup
    {
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "directCommands")]
        public List<SDPDirectCommands> DirectCommands { get; set; }

        [YamlMember(Alias = "commands")]
        public List<string> Commands { get; set; }

        [YamlMember(Alias = "globalParameters")]
        public List<SDPParameter> GlobalParameters { get; set; }

        [YamlMember(Alias = "metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public static SDPCLIGroup FromUniversalModel(AzureCliUniversalViewModel model)
        {
            if (model == null) return null;

            var result = new SDPCLIGroup();

            if(model.Items?.Count() > 0)
            {
                var root = model.Items[0];

                result.Uid = root.Uid;
                result.Name = root.Name;
                result.Summary = root.Summary;
                result.Description = root.Description;

                result.DirectCommands = model.Items.Count() == 1 ? null : model.Items.Skip(1).Select(SDPDirectCommands.FromUniversalItem).ToList();
                result.Commands = model.Commands == null || !model.Commands.Any() ? null : model.Commands.Select(command => command.Uid).ToList();
                result.GlobalParameters = model.GlobalParameters == null ? null : model.GlobalParameters.Select(SDPParameter.FromUniversalParameter).ToList();

                CopyKeyValue(model.Metadata, result.Metadata, "original_content_git_url");
                CopyKeyValue(model.Metadata, result.Metadata, "gitcommit");
                CopyKeyValue(model.Metadata, result.Metadata, "updated_at");
                CopyKeyValue(model.Metadata, result.Metadata, "ms.date");
                CopyKeyValue(model.Metadata, result.Metadata, "description");
            }

            return result;
        }

        private static void CopyKeyValue(Dictionary<string, object> from, Dictionary<string, object> to, string key)
        {
            if (from != null && to != null && from.ContainsKey(key))
            {
                to[key] = from[key];
            }
        }
    }
}
