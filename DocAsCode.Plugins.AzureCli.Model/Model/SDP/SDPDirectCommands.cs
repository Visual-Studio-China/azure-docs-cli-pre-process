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

        [YamlMember(Alias = "syntax")]
        public string Syntax { get; set; }

        [YamlMember(Alias = "examples")]
        public List<SDPExample> Examples { get; set; }

        [YamlMember(Alias = "requiredParameters")]
        public List<SDPParameter> RequiredParameters { get; set; }

        [YamlMember(Alias = "optionalParameters")]
        public List<SDPParameter> OptionalParameters { get; set; }

        [YamlMember(Alias = "editLink")]
        public string EditLink { get; set; }

        public static SDPDirectCommands FromUniversalItem(AzureCliUniversalItem item)
        {
            if (item == null) return null;

            var result = new SDPDirectCommands()
            {
                Uid = item.Uid,
                Name = item.Name,
                Summary = item.Summary,
                Description = item.Description,
                Examples = item.Examples == null || !item.Examples.Any() ? null : item.Examples.Select(SDPExample.FromUniversalExample).ToList(),
                EditLink = BuildEditLink(item.Source)
            };

            // optional parameters
            if(item.Parameters != null)
            {
                var parameters = item.Parameters.Select(SDPParameter.FromUniversalParameter).OrderBy(p => p.Name.ToLowerInvariant(), StringComparer.Ordinal).ToList();
                result.RequiredParameters = parameters.Where(p => p.IsRequired).ToList();
                result.OptionalParameters = parameters.Where(p => !p.IsRequired).ToList();

                if(result.RequiredParameters.Count == 0)
                {
                    result.RequiredParameters = null;
                }
                if (result.OptionalParameters.Count == 0)
                {
                    result.OptionalParameters = null;
                }
            }

            // compose syntax
            ComposeSyntax(result);

            return result;
        }

        static private void ComposeSyntax(SDPDirectCommands command)
        {
            var indent = new string(' ', command.Name.Length);
            var syntax = command.Name;

            if(command.RequiredParameters != null)
            {
                foreach (var parameter in command.RequiredParameters)
                {
                    var paramString = ComposeParameterSyntax(parameter);
                    var paramLineString = $" {paramString}\n{indent}";
                    syntax += paramLineString;
                }
            }

            if (command.OptionalParameters != null)
            {
                foreach (var parameter in command.OptionalParameters)
                {
                    var paramString = ComposeParameterSyntax(parameter);
                    var paramLineString = $" [{paramString}]\n{indent}";
                    syntax += paramLineString;
                }
            }

            command.Syntax = syntax.Trim();
        }

        static private string ComposeParameterSyntax(SDPParameter parameter)
        {
            var paramPart = parameter.Name.Split(' ').FirstOrDefault();
            var valuePart = string.IsNullOrEmpty(parameter.ParameterValueGroup) ? "" : $" {{{parameter.ParameterValueGroup}}}";
            return paramPart + valuePart;
        }

        static private string BuildEditLink(AzureCliUniversalSource source)
        {
            if(source?.Remote != null)
            {
                var repo = source.Remote.Repo.Replace(".git", "/blob/");

                return $"{repo}{source.Remote.Branch}/{source.Remote.Path}";
            }
            return null;
        }
    }
}
