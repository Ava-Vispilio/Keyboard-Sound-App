using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardSoundApp
{
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event EventHandler? KeyPressed;

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public void InstallHook()
        {
            Logger.Log("Installing keyboard hook...");
            _hookID = SetHook(_proc);
            if (_hookID != IntPtr.Zero)
            {
                Logger.Log($"Keyboard hook installed successfully. Hook ID: {_hookID}");
            }
            else
            {
                Logger.Log("ERROR: Failed to install keyboard hook (hook ID is zero)");
            }
        }

        public void UninstallHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                Logger.Log($"Uninstalling keyboard hook. Hook ID: {_hookID}");
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                Logger.Log("Keyboard hook uninstalled");
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            return IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                KeyPressed?.Invoke(this, EventArgs.Empty);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public void Dispose()
        {
            UninstallHook();
        }
    }
}

