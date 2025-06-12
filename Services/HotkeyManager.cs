using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;

namespace FastClick.Services
{
    public class HotkeyManager : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        
        private Dictionary<int, object> _registeredHotkeys = new Dictionary<int, object>();
        private Dictionary<Keys, Models.PointConfig> _hookHotkeys = new Dictionary<Keys, Models.PointConfig>();
        private int _currentId = 1;
        private readonly HotkeyWindow _window;
        private LowLevelKeyboardProc _proc = HookCallback;
        private IntPtr _hookID = IntPtr.Zero;
        private static HotkeyManager _instance;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<HotkeyEventArgs> HotkeyPressed;

        public HotkeyManager()
        {
            _instance = this;
            _window = new HotkeyWindow();
            _window.HotkeyPressed += (sender, args) =>
            {
                if (_registeredHotkeys.ContainsKey(args.Id))
                {
                    var tag = _registeredHotkeys[args.Id];
                    HotkeyPressed?.Invoke(this, new HotkeyEventArgs(args.Id, tag));
                }
            };
            
            _hookID = SetHook(_proc);
        }

        public bool RegisterHotkey(Keys modifiers, Keys key, object tag = null)
        {
            if (key == Keys.None) return false;

            if (tag is Models.PointConfig point && !string.IsNullOrEmpty(point.TargetApplicationName) && modifiers == Keys.None)
            {
                _hookHotkeys[key] = point;
                return true;
            }
            else
            {
                var id = _currentId++;
                var success = RegisterHotKey(_window.Handle, id, (uint)modifiers, (uint)key);
                
                if (success)
                {
                    _registeredHotkeys[id] = tag;
                }
                
                return success;
            }
        }

        public void UnregisterAll()
        {
            foreach (var id in _registeredHotkeys.Keys)
            {
                UnregisterHotKey(_window.Handle, id);
            }
            _registeredHotkeys.Clear();
            _hookHotkeys.Clear();
        }

        public void Dispose()
        {
            UnregisterAll();
            if (_hookID != IntPtr.Zero)
                UnhookWindowsHookEx(_hookID);
            _window?.Dispose();
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                var key = (Keys)vkCode;

                if (_instance != null && _instance._hookHotkeys.ContainsKey(key))
                {
                    var point = _instance._hookHotkeys[key];
                    var activeApp = _instance.GetActiveApplicationName();
                    
                    if (string.Equals(activeApp, point.TargetApplicationName, StringComparison.OrdinalIgnoreCase))
                    {
                        _instance.HotkeyPressed?.Invoke(_instance, new HotkeyEventArgs(-1, point));
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(_instance?._hookID ?? IntPtr.Zero, nCode, wParam, lParam);
        }

        private class HotkeyWindow : NativeWindow, IDisposable
        {
            public event EventHandler<HotkeyEventArgs> HotkeyPressed;

            public HotkeyWindow()
            {
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    var id = m.WParam.ToInt32();
                    HotkeyPressed?.Invoke(this, new HotkeyEventArgs(id, null));
                }
                base.WndProc(ref m);
            }

            public void Dispose()
            {
                DestroyHandle();
            }
        }

        private string GetActiveApplicationName()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                uint processId;
                GetWindowThreadProcessId(hwnd, out processId);
                
                Process process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class HotkeyEventArgs : EventArgs
    {
        public int Id { get; }
        public object Tag { get; }

        public HotkeyEventArgs(int id, object tag)
        {
            Id = id;
            Tag = tag;
        }
    }

    [Flags]
    public enum KeyModifiers : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }
}