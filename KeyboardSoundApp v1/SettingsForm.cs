using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KeyboardSoundApp
{
    public partial class SettingsForm : Form
    {
        private readonly AudioFileManager _fileManager;
        private readonly AppConfig _config;
        private ListBox listBoxFiles = null!;
        private Button buttonAddFile = null!;
        private Button buttonDelete = null!;
        private Button buttonSetDefault = null!;
        private Button buttonClose = null!;
        private CheckBox checkBoxEnable = null!;
        private Label labelStatus = null!;

        public SettingsForm(AudioFileManager fileManager, AppConfig config)
        {
            _fileManager = fileManager;
            _config = AppConfig.Load(); // Reload to get latest
            InitializeComponent();
            LoadFileList();
        }

        private void LoadFileList()
        {
            listBoxFiles.Items.Clear();
            var files = _fileManager.GetAllFiles();
            foreach (var file in files)
            {
                listBoxFiles.Items.Add(file);
            }

            // Highlight default file
            if (!string.IsNullOrEmpty(_config.DefaultAudioFile))
            {
                int index = listBoxFiles.Items.IndexOf(_config.DefaultAudioFile);
                if (index >= 0)
                {
                    listBoxFiles.SelectedIndex = index;
                }
            }

            UpdateButtonStates();
        }

        private void ButtonAddFile_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Audio Files (*.mp3;*.wav;*.wma;*.m4a;*.aac;*.ogg;*.flac)|*.mp3;*.wav;*.wma;*.m4a;*.aac;*.ogg;*.flac|All Files (*.*)|*.*";
                dialog.Title = "Select Audio File";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (_fileManager.AddFile(dialog.FileName))
                    {
                        LoadFileList();
                        labelStatus.Text = "File added successfully.";
                    }
                    else
                    {
                        MessageBox.Show("Failed to add file. Please ensure it's a valid audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        labelStatus.Text = "Failed to add file.";
                    }
                }
            }
        }

        private void ButtonDelete_Click(object? sender, EventArgs e)
        {
            if (listBoxFiles.SelectedItem == null)
            {
                MessageBox.Show("Please select a file to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string? selectedItem = listBoxFiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedItem))
            {
                return;
            }
            string selectedFile = selectedItem;
            string fullPath = _fileManager.GetFullPath(selectedFile);

            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete '{selectedFile}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                if (_fileManager.DeleteFile(selectedFile))
                {
                    // If deleted file was the default, clear default
                    if (_config.DefaultAudioFile == selectedFile)
                    {
                        _config.DefaultAudioFile = string.Empty;
                        _config.Save();
                    }

                    LoadFileList();
                    labelStatus.Text = "File deleted successfully.";
                }
                else
                {
                    MessageBox.Show("Failed to delete file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    labelStatus.Text = "Failed to delete file.";
                }
            }
        }

        private void ButtonSetDefault_Click(object? sender, EventArgs e)
        {
            if (listBoxFiles.SelectedItem == null)
            {
                MessageBox.Show("Please select a file to set as default.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedFile = listBoxFiles.SelectedItem.ToString()!;
            _config.DefaultAudioFile = selectedFile;
            _config.Save();

            labelStatus.Text = $"Default file set to: {selectedFile}";
            UpdateButtonStates();
        }

        private void ButtonClose_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CheckBoxEnable_CheckedChanged(object? sender, EventArgs e)
        {
            _config.IsEnabled = checkBoxEnable.Checked;
            _config.Save();
        }

        private void ListBoxFiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = listBoxFiles.SelectedItem != null;
            buttonDelete.Enabled = hasSelection;
            buttonSetDefault.Enabled = hasSelection;

            if (hasSelection && listBoxFiles.SelectedItem != null)
            {
                string selectedFile = listBoxFiles.SelectedItem.ToString()!;
                buttonSetDefault.Text = selectedFile == _config.DefaultAudioFile ? "Default (Current)" : "Set as Default";
            }
            else
            {
                buttonSetDefault.Text = "Set as Default";
            }
        }
    }
}

