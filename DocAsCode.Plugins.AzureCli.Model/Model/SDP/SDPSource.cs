using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model.Model.SDP
{
    public class SDPSource
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; }

        [YamlMember(Alias = "path")]
        public string Path { get; set; }

        [YamlMember(Alias = "remote")]
        public SDPRemote Remote { get; set; }

        [YamlMember(Alias = "startLine")]
        public int StartLine { get; set; }

        public static SDPSource FromUniversalSource(AzureCliUniversalSource source)
        {
            if (source == null) return null;

            var result = new SDPSource()
            {
                Id = source.Id,
                Path = source.Path,
                Remote = SDPRemote.FromUniversalRemote(source.Remote),
                StartLine = source.StartLine
            };

            return result;
        }
    }

    public class SDPRemote
    {
        [YamlMember(Alias = "branch")]
        public string Branch { get; set; }

        [YamlMember(Alias = "path")]
        public string Path { get; set; }

        [YamlMember(Alias = "repo")]
        public string Repo { get; set; }

        public static SDPRemote FromUniversalRemote(AzureCliUniversalRemote remote)
        {
            if (remote == null) return null;

            var result = new SDPRemote()
            {
                Branch = remote.Branch,
                Path = remote.Path,
                Repo = remote.Repo
            };

            return result;
        }
    }
}
