using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;

namespace KeyboardSoundApp
{
    public class TrayApplicationContext : ApplicationContext
    {
        private static int _playbackCounter = 0;
        
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
            var keypressTime = DateTime.Now;
            Logger.Log($"KeyPressed event received at {keypressTime:HH:mm:ss.fff}");
            
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
                int playbackId = System.Threading.Interlocked.Increment(ref _playbackCounter);
                Logger.Log($">>> Starting playback #{playbackId} - file: {_currentAudioFile}");
                
                // Play sound on a separate thread - fire and forget for overlapping playback
                System.Threading.Tasks.Task.Run(() =>
                {
                    var taskStartTime = DateTime.Now;
                    var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    Logger.Log($"  Playback #{playbackId} - Task started on thread {threadId} at {taskStartTime:HH:mm:ss.fff}");
                    
                    MediaPlayer? player = null;
                    try
                    {
                        var createTime = DateTime.Now;
                        player = new MediaPlayer();
                        Logger.Log($"  Playback #{playbackId} - MediaPlayer instance created at {createTime:HH:mm:ss.fff}");
                        
                        // Add event handlers to track playback state
                        player.MediaOpened += (s, args) => 
                        {
                            Logger.Log($"  Playback #{playbackId} - MediaOpened event fired");
                        };
                        
                        player.MediaEnded += (s, args) => 
                        {
                            Logger.Log($"  Playback #{playbackId} - MediaEnded event fired");
                            try { player?.Close(); } catch { }
                        };
                        
                        player.MediaFailed += (s, args) => 
                        {
                            Logger.LogError($"  Playback #{playbackId} - MediaFailed event fired: {args.ErrorException?.Message ?? "Unknown error"}", args.ErrorException);
                        };
                        
                        // Log current state
                        Logger.Log($"  Playback #{playbackId} - Initial state: HasAudio={player.HasAudio}, CanPause={player.CanPause}");
                        
                        // Convert local path to URI (ensure it's a proper file URI)
                        var uri = new Uri(System.IO.Path.GetFullPath(_currentAudioFile));
                        Logger.Log($"  Playback #{playbackId} - Opening media URI: {uri}");
                        
                        player.Open(uri);
                        Logger.Log($"  Playback #{playbackId} - MediaPlayer.Open() called");
                        
                        player.Volume = 1.0;
                        
                        var playStartTime = DateTime.Now;
                        player.Play();
                        var playEndTime = DateTime.Now;
                        var playDuration = (playEndTime - playStartTime).TotalMilliseconds;
                        Logger.Log($"  Playback #{playbackId} - MediaPlayer.Play() called in {playDuration:F2}ms");
                        
                        // No sleep needed - MediaPlayer.Play() is non-blocking
                        // Each MediaPlayer instance will play independently, allowing overlapping sounds
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Playback #{playbackId} - Error playing sound", ex);
                        player?.Close();
                    }
                    // Note: MediaPlayer will be garbage collected when out of scope
                    // This allows multiple instances to play simultaneously
                });
                
                Logger.Log($"<<< Playback #{playbackId} - Task.Run() returned (non-blocking)");
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

