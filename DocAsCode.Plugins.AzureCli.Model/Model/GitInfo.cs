namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class GitInfo
    {
        /// <summary>
        /// File path relative to root, e.g. src/component/class.cs
        /// </summary>
        [YamlMember(Alias = "path")]
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// Branch name, e.g. master
        /// </summary>
        [YamlMember(Alias = "branch")]
        [JsonProperty("branch")]
        public string Branch { get; set; }

        /// <summary>
        /// Repository name, e.g. https://github.com/azure/azure_cli.git
        /// </summary>
        [YamlMember(Alias = "repo")]
        [JsonProperty("repo")]
        public string Repository { get; set; }
    }

    public class RemoteGitInfo
    {
        [YamlMember(Alias = "remote")]
        [JsonProperty("remote")]
        public GitInfo Remote { get; set; }
    }
}
