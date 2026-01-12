using YamlDotNet.Serialization;

namespace MultiLogViewer.Models
{
    public class AppSettings
    {
        [YamlMember(Alias = "polling_interval_ms")]
        public int PollingIntervalMs { get; set; } = 1000;

        [YamlMember(Alias = "log_retention_limit")]
        public string? LogRetentionLimit { get; set; }

        [YamlMember(Alias = "skip_tail_mode_warning")]
        public bool SkipTailModeWarning { get; set; } = false;

        [YamlMember(Alias = "enable_trace_log")]
        public bool EnableTraceLog { get; set; } = false;
    }
}
