#nullable enable
namespace KeyboardSoundApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null!;
        private System.Windows.Forms.NotifyIcon notifyIcon = null!;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip = null!;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();

            // NotifyIcon
            this.notifyIcon.Text = "Keyboard Sound App";
            this.notifyIcon.Visible = true;
            this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
            // Note: Icon will need to be set separately - using a default system icon for now
            try
            {
                this.notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            catch
            {
                // If icon fails to load, continue without icon
            }

            // Form properties - Hide MainForm, it's just a system tray manager
            this.Text = "Keyboard Sound App";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Size = new System.Drawing.Size(0, 0);
            this.Opacity = 0;
        }
    }
}

