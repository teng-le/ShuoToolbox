using System.Text.Json;
using System.IO;

namespace Toolbox
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Light"; // Light / Dark
        public bool AutoStart { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
        public bool EnableVSync { get; set; } = false; // 垂直同步，默认关闭

        private static readonly string SettingsPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}