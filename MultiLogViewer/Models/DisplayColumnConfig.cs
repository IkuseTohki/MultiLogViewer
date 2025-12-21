using YamlDotNet.Serialization;

namespace MultiLogViewer.Models
{
    public class DisplayColumnConfig
    {
        public string Header { get; set; } = string.Empty;
        public string BindingPath { get; set; } = string.Empty; // 例: Timestamp, Level, Message, AdditionalData[Key]
        public int Width { get; set; } = 100;
        public string? StringFormat { get; set; }

        [YamlIgnore]
        public ColumnStyleConfig? StyleConfig { get; set; }
    }
}
