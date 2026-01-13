using System.Collections.Generic;

namespace MultiLogViewer.Models
{
    public enum MatchType
    {
        First,
        All
    }

    public class SubPatternConfig
    {
        public string SourceField { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public MatchType MatchType { get; set; } = MatchType.First;
        public string Separator { get; set; } = ", ";
        public List<string> Options { get; set; } = new List<string>();
        public List<FieldTransformConfig> FieldTransforms { get; set; } = new List<FieldTransformConfig>();
    }
}
