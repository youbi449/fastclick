using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using FastClick.Models;
using FastClick.Services;

namespace FastClick.Windows
{
    public partial class PointCaptureWindow : Window
    {
        private bool _isCapturing = false;
        private LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static PointCaptureWindow _instance;

        public PointConfig CapturedPoint { get; private set; }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public PointCaptureWindow()
        {
            InitializeComponent();
            _instance = this;
        }

        private void StartCapture_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCapturing)
            {
                StartCapture();
            }
            else
            {
                StopCapture();
            }
        }

        private void GetCurrent_Click(object sender, RoutedEventArgs e)
        {
            var pos = ActionExecutor.GetCurrentCursorPosition();
            XTextBox.Text = pos.X.ToString();
            YTextBox.Text = pos.Y.ToString();
        }

        private void StartCapture()
        {
            _isCapturing = true;
            StartCaptureButton.Content = "Stop Capture (Click anywhere)";
            WindowState = WindowState.Minimized;
            
            _hookID = SetHook(_proc);
        }

        private void StopCapture()
        {
            _isCapturing = false;
            StartCaptureButton.Content = "Start Capture";
            WindowState = WindowState.Normal;
            
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN && _instance?._isCapturing == true)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                
                _instance.Dispatcher.Invoke(() =>
                {
                    _instance.XTextBox.Text = hookStruct.pt.x.ToString();
                    _instance.YTextBox.Text = hookStruct.pt.y.ToString();
                    _instance.StopCapture();
                });
                
                return (IntPtr)1; // Suppress the click
            }
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(XTextBox.Text, out int x) && int.TryParse(YTextBox.Text, out int y))
            {
                CapturedPoint = new PointConfig(x, y, NameTextBox.Text);
                DialogResult = true;
            }
            else
            {
                System.Windows.MessageBox.Show("Please enter valid X and Y coordinates.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopCapture();
            base.OnClosed(e);
        }
    }
}