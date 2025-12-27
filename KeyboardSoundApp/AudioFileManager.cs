using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KeyboardSoundApp
{
    public class AudioFileManager
    {
        private static readonly string[] AllowedExtensions = { ".mp3", ".wav", ".wma", ".m4a", ".aac", ".ogg", ".flac" };
        private readonly string _storagePath;

        public AudioFileManager()
        {
            _storagePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KeyboardSoundApp",
                "AudioFiles"
            );

            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public string GetStoragePath()
        {
            return _storagePath;
        }

        public bool AddFile(string sourcePath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    return false;
                }

                if (!ValidateAudioFile(sourcePath))
                {
                    return false;
                }

                var fileName = Path.GetFileName(sourcePath);
                var destPath = Path.Combine(_storagePath, fileName);

                // Handle duplicate names
                int counter = 1;
                string baseFileName = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                while (File.Exists(destPath))
                {
                    fileName = $"{baseFileName} ({counter}){extension}";
                    destPath = Path.Combine(_storagePath, fileName);
                    counter++;
                }

                File.Copy(sourcePath, destPath, false);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding file: {ex.Message}");
                return false;
            }
        }

        public bool DeleteFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_storagePath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        public List<string> GetAllFiles()
        {
            try
            {
                if (!Directory.Exists(_storagePath))
                {
                    return new List<string>();
                }

                return Directory.GetFiles(_storagePath)
                    .Where(file => AllowedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .Select(file => Path.GetFileName(file))
                    .OrderBy(name => name)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting files: {ex.Message}");
                return new List<string>();
            }
        }

        public bool ValidateAudioFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }

        public string GetFullPath(string fileName)
        {
            return Path.Combine(_storagePath, fileName);
        }
    }
}

