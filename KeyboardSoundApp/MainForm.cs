using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;

namespace KeyboardSoundApp
{
    public partial class MainForm : Form
    {
        private KeyboardHook? _keyboardHook;
        private AudioFileManager _fileManager;
        private AppConfig _config;
        private string _currentAudioFile = string.Empty;

        public MainForm()
        {
            Logger.Log("MainForm constructor called");
            try
            {
                Logger.Log("Initializing component...");
                InitializeComponent();
                Logger.Log("Component initialized");

                Logger.Log("Creating AudioFileManager...");
                _fileManager = new AudioFileManager();
                Logger.Log($"AudioFileManager created. Storage path: {_fileManager.GetStoragePath()}");

                Logger.Log("Loading configuration...");
                _config = AppConfig.Load();
                Logger.Log($"Configuration loaded. IsEnabled: {_config.IsEnabled}, DefaultAudioFile: {_config.DefaultAudioFile}");

                Logger.Log("Initializing application...");
                InitializeApplication();
                Logger.Log("Application initialization complete");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in MainForm constructor", ex);
                throw;
            }
        }

        private void InitializeApplication()
        {
            Logger.Log("InitializeApplication called");
            try
            {
                // Set form to be hidden
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                Logger.Log("Form set to minimized and hidden from taskbar");

                // Initialize keyboard hook
                Logger.Log("Creating KeyboardHook...");
                _keyboardHook = new KeyboardHook();
                _keyboardHook.KeyPressed += KeyboardHook_KeyPressed;
                Logger.Log("KeyboardHook created and event handler attached");

                // Load default audio file
                Logger.Log("Loading audio file...");
                LoadAudioFile();

                // Update tray icon tooltip
                notifyIcon.Text = "Keyboard Sound App";
                Logger.Log("Tray icon text set");

                // Update context menu based on enabled state
                Logger.Log("Updating context menu...");
                UpdateContextMenu();

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
                    }
                }
                else
                {
                    Logger.Log("No default audio file configured");
                }

                // Try to load first available file
                Logger.Log("Looking for available audio files...");
                var files = _fileManager.GetAllFiles();
                Logger.Log($"Found {files.Count} audio file(s)");
                if (files.Count > 0)
                {
                    _currentAudioFile = _fileManager.GetFullPath(files[0]);
                    _config.DefaultAudioFile = files[0];
                    _config.Save();
                    Logger.Log($"Set first available file as default: {_currentAudioFile}");
                }
                else
                {
                    Logger.Log("WARNING: No audio files available!");
                    _currentAudioFile = string.Empty;
                }
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
                // Play sound on a separate thread to avoid blocking the hook
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
                        
                        // Wait briefly to ensure playback starts
                        System.Threading.Thread.Sleep(200);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error playing sound", ex);
                        player?.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in keyboard hook handler", ex);
            }
        }

        private void UpdateContextMenu()
        {
            contextMenuStrip.Items.Clear();

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += SettingsItem_Click;
            contextMenuStrip.Items.Add(settingsItem);

            var enableItem = new ToolStripMenuItem(_config.IsEnabled ? "Disable" : "Enable");
            enableItem.Click += EnableItem_Click;
            contextMenuStrip.Items.Add(enableItem);

            contextMenuStrip.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitItem_Click;
            contextMenuStrip.Items.Add(exitItem);
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
            _keyboardHook?.Dispose();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                _keyboardHook?.Dispose();
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _keyboardHook?.Dispose();
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}

