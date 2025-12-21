using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace MultiLogViewer.Models
{
    public class AppConfig
    {
        [YamlMember(Alias = "polling_interval_ms")]
        public int PollingIntervalMs { get; set; } = 1000;

        [YamlMember(Alias = "display_columns")]
        public List<DisplayColumnConfig> DisplayColumns { get; set; } = new List<DisplayColumnConfig>();

        [YamlMember(Alias = "column_styles")]
        public List<ColumnStyleConfig> ColumnStyles { get; set; } = new List<ColumnStyleConfig>();

        [YamlMember(Alias = "log_formats")]
        public List<LogFormatConfig> LogFormats { get; set; } = new List<LogFormatConfig>();
    }
}
