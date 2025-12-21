using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace MultiLogViewer.Models
{
    public class ColumnStyleConfig
    {
        [YamlMember(Alias = "column_header")]
        public string ColumnHeader { get; set; } = string.Empty;

        [YamlMember(Alias = "rules")]
        public List<ColorRuleConfig> Rules { get; set; } = new List<ColorRuleConfig>();

        [YamlMember(Alias = "semantic_coloring")]
        public bool SemanticColoring { get; set; }
    }

    public class ColorRuleConfig
    {
        [YamlMember(Alias = "pattern")]
        public string Pattern { get; set; } = string.Empty;

        [YamlMember(Alias = "foreground")]
        public string Foreground { get; set; } = string.Empty;

        [YamlMember(Alias = "background")]
        public string Background { get; set; } = string.Empty;

        [YamlMember(Alias = "font_weight")]
        public string FontWeight { get; set; } = string.Empty;
    }
}
