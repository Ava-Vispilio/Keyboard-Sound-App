using System;
using System.IO;
using System.Text.Json;

namespace KeyboardSoundApp
{
    public class AppConfig
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyboardSoundApp",
            "config.json"
        );

        public string DefaultAudioFile { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;

        public void Save()
        {
            try
            {
                var configDir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    return config ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
            }

            return new AppConfig();
        }
    }
}

