using System;
using System.IO;
using MultiLogViewer.Utils;

namespace MultiLogViewer.Services
{
    public class ConfigPathResolver : IConfigPathResolver
    {
        private const string DefaultLogProfileName = "LogProfile.yaml";
        private const string AppSettingsFileName = "AppSettings.yaml";

        public string ResolveLogProfilePath(string[] args)
        {
            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                return args[0];
            }

            return Path.Combine(PathHelper.GetBaseDirectory(), DefaultLogProfileName);
        }

        public string GetAppSettingsPath()
        {
            return Path.Combine(PathHelper.GetBaseDirectory(), AppSettingsFileName);
        }
    }
}
