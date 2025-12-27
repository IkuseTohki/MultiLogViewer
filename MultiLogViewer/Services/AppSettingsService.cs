using MultiLogViewer.Models;
using System;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MultiLogViewer.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly IConfigPathResolver _configPathResolver;
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;

        public AppSettingsService(IConfigPathResolver configPathResolver)
        {
            _configPathResolver = configPathResolver;

            _serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public AppSettings Load()
        {
            var path = _configPathResolver.GetAppSettingsPath();
            if (!File.Exists(path)) return new AppSettings();

            try
            {
                using (var reader = new StreamReader(path))
                {
                    return _deserializer.Deserialize<AppSettings>(reader) ?? new AppSettings();
                }
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            var path = _configPathResolver.GetAppSettingsPath();

            // ファイルが存在しない場合は、従来通りシリアライザで新規作成する
            if (!File.Exists(path))
            {
                using (var writer = new StreamWriter(path))
                {
                    _serializer.Serialize(writer, settings);
                }
                return;
            }

            // ファイルが存在する場合は、コメントを維持するために必要な箇所だけ書き換える
            string content = File.ReadAllText(path);

            content = UpdateYamlValue(content, "polling_interval_ms", settings.PollingIntervalMs);
            content = UpdateYamlValue(content, "log_retention_limit", settings.LogRetentionLimit);
            content = UpdateYamlValue(content, "skip_tail_mode_warning", settings.SkipTailModeWarning);

            File.WriteAllText(path, content);
        }

        /// <summary>
        /// YAML 文字列内の特定のキーの値を更新します。キーが存在しない場合は末尾に追記します。
        /// </summary>
        private string UpdateYamlValue(string content, string key, object? value)
        {
            // 値が null の場合は、既存の値を消さないよう何もしない（未指定状態を維持）
            if (value == null) return content;

            // YAML形式の文字列表現に変換
            string valueStr = value switch
            {
                bool b => b.ToString().ToLower(),
                string s => $"\"{s}\"",
                _ => value.ToString() ?? ""
            };

            // 行頭（または空白後）に "key:" があり、その後に値が続くパターンにマッチさせる
            // コメントアウトされている行は対象外とする（^ で行頭を指定）
            var pattern = $@"^(?<prefix>\s*{key}\s*:\s*).*$";
            var regex = new Regex(pattern, RegexOptions.Multiline);

            if (regex.IsMatch(content))
            {
                // 既存のキーがある場合は、値を置換
                return regex.Replace(content, $"${{prefix}}{valueStr}");
            }
            else
            {
                // 既存のキーがない場合は、ファイルの末尾に追加
                var separator = content.EndsWith(Environment.NewLine) ? "" : Environment.NewLine;
                return content + separator + $"{key}: {valueStr}" + Environment.NewLine;
            }
        }
    }
}
