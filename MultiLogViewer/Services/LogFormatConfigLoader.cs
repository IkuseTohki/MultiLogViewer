using MultiLogViewer.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;

namespace MultiLogViewer.Services
{
    public class LogFormatConfigLoader : ILogFormatConfigLoader
    {
        public AppConfig Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                // ファイルが存在しない場合は、空のAppConfigを返すか、例外をスローするか、要検討。
                // 現時点では空のAppConfigを返す。
                return new AppConfig();
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            using (var reader = new StreamReader(filePath))
            {
                return deserializer.Deserialize<AppConfig>(reader);
            }
        }
    }
}
