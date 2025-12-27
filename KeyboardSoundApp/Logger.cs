using System;
using System.IO;

namespace KeyboardSoundApp
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyboardSoundApp",
            "debug.log"
        );

        public static void Log(string message)
        {
            try
            {
                var logDir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] {message}";
                
                File.AppendAllText(LogPath, logMessage + Environment.NewLine);
                
                // Also output to debug console (visible in DebugView or Visual Studio)
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch
            {
                // Silently fail - don't break the app if logging fails
            }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            Log($"ERROR: {message}");
            if (ex != null)
            {
                Log($"Exception: {ex.GetType().Name}: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}

