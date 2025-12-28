using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;

namespace KeyboardSoundApp
{
    public class TrayApplicationContext : ApplicationContext
    {
        private KeyboardHook? _keyboardHook;
        private AudioFileManager _fileManager;
        private AppConfig _config;
        private string _currentAudioFile = string.Empty;
        private NotifyIcon _notifyIcon = null!;
        private ContextMenuStrip _contextMenuStrip = null!;
        private System.ComponentModel.IContainer _components;

        public TrayApplicationContext()
        {
            Logger.Log("TrayApplicationContext constructor called");
            try
            {
                _components = new System.ComponentModel.Container();
                
                Logger.Log("Creating AudioFileManager...");
                _fileManager = new AudioFileManager();
                Logger.Log($"AudioFileManager created. Storage path: {_fileManager.GetStoragePath()}");

                Logger.Log("Loading configuration...");
                _config = AppConfig.Load();
                Logger.Log($"Configuration loaded. IsEnabled: {_config.IsEnabled}, DefaultAudioFile: {_config.DefaultAudioFile}");

                Logger.Log("Initializing system tray...");
                InitializeSystemTray();

                Logger.Log("Initializing application...");
                InitializeApplication();

                Logger.Log("Application initialization complete");

                // Show Settings form on first launch
                Logger.Log("Showing Settings form on first launch");
                ShowSettingsForm();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in TrayApplicationContext constructor", ex);
                throw;
            }
        }

        private void InitializeSystemTray()
        {
            Logger.Log("InitializeSystemTray called");
            try
            {
                // Create NotifyIcon
                _notifyIcon = new NotifyIcon(_components)
                {
                    Text = "Keyboard Sound App",
                    Visible = true
                };

                // Set icon
                try
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error setting tray icon", ex);
                }

                // Create context menu
                _contextMenuStrip = new ContextMenuStrip();
                _notifyIcon.ContextMenuStrip = _contextMenuStrip;
                UpdateContextMenu();

                Logger.Log("System tray initialized");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in InitializeSystemTray", ex);
                throw;
            }
        }

        private void InitializeApplication()
        {
            Logger.Log("InitializeApplication called");
            try
            {
                // Initialize keyboard hook
                Logger.Log("Creating KeyboardHook...");
                _keyboardHook = new KeyboardHook();
                _keyboardHook.KeyPressed += KeyboardHook_KeyPressed;
                Logger.Log("KeyboardHook created and event handler attached");

                // Load default audio file
                Logger.Log("Loading audio file...");
                LoadAudioFile();

                // Install hook if enabled
                if (_config.IsEnabled)
                {
                    Logger.Log("App is enabled, installing keyboard hook...");
                    _keyboardHook.InstallHook();
                    Logger.Log("Keyboard hook installed");
                }
                else
                {
                    Logger.Log("App is disabled, skipping hook installation");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in InitializeApplication", ex);
                throw;
            }
        }

        private void LoadAudioFile()
        {
            Logger.Log("LoadAudioFile called");
            try
            {
                if (!string.IsNullOrEmpty(_config.DefaultAudioFile))
                {
                    Logger.Log($"Attempting to load default file: {_config.DefaultAudioFile}");
                    var fullPath = _fileManager.GetFullPath(_config.DefaultAudioFile);
                    Logger.Log($"Full path: {fullPath}");
                    if (File.Exists(fullPath))
                    {
                        _currentAudioFile = fullPath;
                        Logger.Log($"Audio file loaded successfully: {_currentAudioFile}");
                        return;
                    }
                    else
                    {
                        Logger.Log($"Default audio file not found: {fullPath}");
                        _config.DefaultAudioFile = string.Empty;
                        _config.Save();
                    }
                }
                else
                {
                    Logger.Log("No default audio file configured");
                }

                // DON'T auto-set first available file - user must explicitly set it
                Logger.Log("No default audio file set - user must select one in settings");
                _currentAudioFile = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in LoadAudioFile", ex);
            }
        }

        private void KeyboardHook_KeyPressed(object? sender, EventArgs e)
        {
            Logger.Log("KeyPressed event received");
            
            if (!_config.IsEnabled)
            {
                Logger.Log("App is disabled, ignoring keypress");
                return;
            }
            
            if (string.IsNullOrEmpty(_currentAudioFile))
            {
                Logger.Log("No audio file loaded, ignoring keypress");
                return;
            }
            
            if (!File.Exists(_currentAudioFile))
            {
                Logger.Log($"Audio file does not exist: {_currentAudioFile}");
                return;
            }

            try
            {
                Logger.Log($"Playing sound: {_currentAudioFile}");
                // Play sound on a separate thread - fire and forget for overlapping playback
                System.Threading.Tasks.Task.Run(() =>
                {
                    MediaPlayer? player = null;
                    try
                    {
                        player = new MediaPlayer();
                        // Convert local path to URI (ensure it's a proper file URI)
                        var uri = new Uri(System.IO.Path.GetFullPath(_currentAudioFile));
                        Logger.Log($"Opening media URI: {uri}");
                        player.Open(uri);
                        player.Volume = 1.0;
                        player.Play();
                        Logger.Log("Media playback started");
                        
                        // No sleep needed - MediaPlayer.Play() is non-blocking
                        // Each MediaPlayer instance will play independently, allowing overlapping sounds
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error playing sound", ex);
                        player?.Close();
                    }
                    // Note: MediaPlayer will be garbage collected when out of scope
                    // This allows multiple instances to play simultaneously
                });
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in keyboard hook handler", ex);
            }
        }

        private void UpdateContextMenu()
        {
            _contextMenuStrip.Items.Clear();

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += SettingsItem_Click;
            _contextMenuStrip.Items.Add(settingsItem);

            var enableItem = new ToolStripMenuItem(_config.IsEnabled ? "Disable" : "Enable");
            enableItem.Click += EnableItem_Click;
            _contextMenuStrip.Items.Add(enableItem);

            _contextMenuStrip.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitItem_Click;
            _contextMenuStrip.Items.Add(exitItem);
        }

        private void ShowSettingsForm()
        {
            Logger.Log("ShowSettingsForm called - displaying Settings form");
            try
            {
                var settingsForm = new SettingsForm(_fileManager, _config);
                settingsForm.FormClosed += (s, args) =>
                {
                    // When Settings form closes, reload config and update
                    Logger.Log("Settings form closed, reloading configuration");
                    _config = AppConfig.Load();
                    LoadAudioFile();
                    UpdateContextMenu();

                    // Restart hook if needed
                    _keyboardHook?.UninstallHook();
                    if (_config.IsEnabled)
                    {
                        _keyboardHook?.InstallHook();
                    }
                };
                settingsForm.Show();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in ShowSettingsForm", ex);
            }
        }

        private void SettingsItem_Click(object? sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_fileManager, _config))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Reload config and audio file
                    _config = AppConfig.Load();
                    LoadAudioFile();
                    UpdateContextMenu();

                    // Restart hook if needed
                    _keyboardHook?.UninstallHook();
                    if (_config.IsEnabled)
                    {
                        _keyboardHook?.InstallHook();
                    }
                }
            }
        }

        private void EnableItem_Click(object? sender, EventArgs e)
        {
            _config.IsEnabled = !_config.IsEnabled;
            _config.Save();

            if (_config.IsEnabled)
            {
                _keyboardHook?.InstallHook();
            }
            else
            {
                _keyboardHook?.UninstallHook();
            }

            UpdateContextMenu();
        }

        private void ExitItem_Click(object? sender, EventArgs e)
        {
            Logger.Log("Exit requested from tray menu");
            _keyboardHook?.Dispose();
            _notifyIcon.Visible = false;
            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            Logger.Log("ExitThreadCore called - cleaning up");
            _keyboardHook?.Dispose();
            _notifyIcon?.Dispose();
            _components?.Dispose();
            base.ExitThreadCore();
        }
    }
}

