using MultiLogViewer.Models;

namespace MultiLogViewer.Services
{
    public interface ILogFormatConfigLoader
    {
        AppConfig Load(string filePath);
    }
}
