namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing.AzureCli.Model
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AzureCliViewModel : AzureCliViewModelBase
    {
        [YamlMember(Alias = "htmlId")]
        [JsonProperty("htmlId")]
        public string HtmlId { get; set; }

        [YamlMember(Alias = "usage")]
        [JsonProperty("usage")]
        public string Usage { get; set; }

        [YamlMember(Alias = "filePath")]
        [JsonProperty("filePath")]
        public string FilePath { get; set; }

        [YamlMember(Alias = "synopsis")]
        [JsonProperty("synopsis")]
        public string Synopsis { get; set; }

        [YamlMember(Alias = "isWorkflow")]
        [JsonProperty("isWorkflow")]
        public bool Isworkflow { get; set; }

        [YamlMember(Alias = "extent")]
        [JsonProperty("extent")]
        public SourceExtent Extent { get; set; }

        [YamlMember(Alias = "isSupportCommonParameters")]
        [JsonProperty("isSupportCommonParameters")]
        public bool IsSupportCommonParameters { get; set; }

        [YamlMember(Alias = "notes")]
        [JsonProperty("notes")]
        public string Notes { get; set; }

        [YamlMember(Alias = "options")]
        [JsonProperty("options")]
        public List<Option> Options { get; set; }

        [YamlMember(Alias = "inputs")]
        [JsonProperty("inputs")]
        public List<InputOutput> Inputs { get; set; }

        [YamlMember(Alias = "outputs")]
        [JsonProperty("outputs")]
        public List<InputOutput> Outputs { get; set; }

        [YamlMember(Alias = "links")]
        [JsonProperty("links")]
        public List<Link> Links { get; set; }

        [YamlMember(Alias = "syntax")]
        [JsonProperty("syntax")]
        public List<Syntax> Syntax { get; set; }

        [YamlMember(Alias = "examples")]
        [JsonProperty("examples")]
        public List<Example> Examples { get; set; }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<Parameter> Parameters { get; set; }

        [YamlMember(Alias = "children")]
        [JsonProperty("children")]
        public List<AzureCliViewModel> Children { get; set; }
    }
}
