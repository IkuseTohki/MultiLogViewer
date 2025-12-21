using MultiLogViewer.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;

namespace MultiLogViewer.Services
{
    public class LogFormatConfigLoader : ILogFormatConfigLoader
    {
        public AppConfig? Load(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return new AppConfig();
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            try
            {
                using (var reader = new StreamReader(configPath))
                {
                    return deserializer.Deserialize<AppConfig>(reader);
                }
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                throw new System.Exception($"設定ファイル(config.yaml)の解析に失敗しました。書式が正しいか見直してください。\n詳細: {ex.Message}", ex);
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"設定ファイルの読み込み中に予期せぬエラーが発生しました。\n{ex.Message}", ex);
            }
        }
    }
}
