using System.Drawing;

#nullable enable
namespace KeyboardSoundApp
{
    partial class SettingsForm
    {
        // Components are managed by the base Form class

        private void InitializeComponent()
        {
            this.listBoxFiles = new System.Windows.Forms.ListBox();
            this.buttonAddFile = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.buttonSetDefault = new System.Windows.Forms.Button();
            this.buttonClose = new System.Windows.Forms.Button();
            this.checkBoxEnable = new System.Windows.Forms.CheckBox();
            this.labelStatus = new System.Windows.Forms.Label();

            this.SuspendLayout();

            // listBoxFiles
            this.listBoxFiles.FormattingEnabled = true;
            this.listBoxFiles.ItemHeight = 15;
            this.listBoxFiles.Location = new Point(12, 40);
            this.listBoxFiles.Size = new Size(360, 214);
            this.listBoxFiles.TabIndex = 0;
            this.listBoxFiles.SelectedIndexChanged += ListBoxFiles_SelectedIndexChanged;

            // buttonAddFile
            this.buttonAddFile.Location = new Point(12, 260);
            this.buttonAddFile.Size = new Size(85, 30);
            this.buttonAddFile.TabIndex = 1;
            this.buttonAddFile.Text = "Add File";
            this.buttonAddFile.UseVisualStyleBackColor = true;
            this.buttonAddFile.Click += ButtonAddFile_Click;

            // buttonDelete
            this.buttonDelete.Location = new Point(103, 260);
            this.buttonDelete.Size = new Size(85, 30);
            this.buttonDelete.TabIndex = 2;
            this.buttonDelete.Text = "Delete";
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Enabled = false;
            this.buttonDelete.Click += ButtonDelete_Click;

            // buttonSetDefault
            this.buttonSetDefault.Location = new Point(194, 260);
            this.buttonSetDefault.Size = new Size(120, 30);
            this.buttonSetDefault.TabIndex = 3;
            this.buttonSetDefault.Text = "Set as Default";
            this.buttonSetDefault.UseVisualStyleBackColor = true;
            this.buttonSetDefault.Enabled = false;
            this.buttonSetDefault.Click += ButtonSetDefault_Click;

            // buttonClose
            this.buttonClose.Location = new Point(287, 300);
            this.buttonClose.Size = new Size(85, 30);
            this.buttonClose.TabIndex = 5;
            this.buttonClose.Text = "Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += ButtonClose_Click;

            // checkBoxEnable
            this.checkBoxEnable.AutoSize = true;
            this.checkBoxEnable.Location = new Point(12, 12);
            this.checkBoxEnable.Size = new Size(63, 19);
            this.checkBoxEnable.TabIndex = 4;
            this.checkBoxEnable.Text = "Enable";
            this.checkBoxEnable.UseVisualStyleBackColor = true;
            this.checkBoxEnable.Checked = _config.IsEnabled;
            this.checkBoxEnable.CheckedChanged += CheckBoxEnable_CheckedChanged;

            // labelStatus
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new Point(12, 306);
            this.labelStatus.Size = new Size(39, 15);
            this.labelStatus.TabIndex = 6;
            this.labelStatus.Text = "Ready";

            // SettingsForm
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(384, 342);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.checkBoxEnable);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.buttonSetDefault);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.buttonAddFile);
            this.Controls.Add(this.listBoxFiles);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Keyboard Sound Settings";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}

