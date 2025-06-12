using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FastClick.Models;

namespace FastClick.Services
{
    public class ActionExecutor
    {

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(System.Drawing.Point Point);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref System.Drawing.Point lpPoint);


        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);


        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_MBUTTONDOWN = 0x0207;
        private const uint WM_MBUTTONUP = 0x0208;
        private const uint WM_LBUTTONDBLCLK = 0x0203;


        public ActionExecutor()
        {
        }

        public void ExecuteAction(PointConfig point)
        {
            if (point == null || !point.IsEnabled) return;

            // Check if we should limit to specific application
            if (!string.IsNullOrEmpty(point.TargetApplicationName))
            {
                var activeApp = GetActiveApplicationName();
                if (!string.Equals(activeApp, point.TargetApplicationName, StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping action - Active app: {activeApp}, Target app: {point.TargetApplicationName}");
                    return;
                }
            }

            try
            {
                var screenPosition = GetScreenPosition(point);

                // Execute the action based on repeat count
                for (int i = 0; i < point.RepeatCount; i++)
                {
                    if (i > 0 && point.DelayMs > 0)
                    {
                        Thread.Sleep(point.DelayMs);
                    }

                    ExecuteSingleAction(point.Action, screenPosition.X, screenPosition.Y);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing action: {ex.Message}");
            }
        }

        private void ExecuteSingleAction(MouseAction action, int x, int y)
        {
            System.Diagnostics.Debug.WriteLine($"Executing action at screen coords: {x}, {y}");

            // Trouver la fenêtre à cette position
            var point = new System.Drawing.Point(x, y);
            IntPtr hWnd = WindowFromPoint(point);
            
            if (hWnd == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("No window found at coordinates");
                return;
            }

            // Convertir les coordonnées écran en coordonnées client de la fenêtre
            var clientPoint = new System.Drawing.Point(x, y);
            ScreenToClient(hWnd, ref clientPoint);
            
            System.Diagnostics.Debug.WriteLine($"Client coords: {clientPoint.X}, {clientPoint.Y}");
            
            // Créer lParam avec les coordonnées client
            IntPtr lParam = (IntPtr)((clientPoint.Y << 16) | (clientPoint.X & 0xFFFF));

            switch (action)
            {
                case MouseAction.LeftClick:
                    PostMessage(hWnd, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
                    Thread.Sleep(50);
                    PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                    break;

                case MouseAction.RightClick:
                    PostMessage(hWnd, WM_RBUTTONDOWN, IntPtr.Zero, lParam);
                    Thread.Sleep(50);
                    PostMessage(hWnd, WM_RBUTTONUP, IntPtr.Zero, lParam);
                    break;

                case MouseAction.DoubleClick:
                    PostMessage(hWnd, WM_LBUTTONDBLCLK, IntPtr.Zero, lParam);
                    break;

                case MouseAction.MouseDown:
                    PostMessage(hWnd, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
                    break;

                case MouseAction.MouseUp:
                    PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                    break;

                case MouseAction.MiddleClick:
                    PostMessage(hWnd, WM_MBUTTONDOWN, IntPtr.Zero, lParam);
                    Thread.Sleep(50);
                    PostMessage(hWnd, WM_MBUTTONUP, IntPtr.Zero, lParam);
                    break;
            }
        }

        private System.Drawing.Point GetScreenPosition(PointConfig point)
        {
            var screens = Screen.AllScreens;
            
            if (point.ScreenIndex >= 0 && point.ScreenIndex < screens.Length)
            {
                var screen = screens[point.ScreenIndex];
                return new System.Drawing.Point(
                    screen.Bounds.X + point.X,
                    screen.Bounds.Y + point.Y
                );
            }

            // Fallback to primary screen
            return new System.Drawing.Point(point.X, point.Y);
        }

        public static System.Drawing.Point GetCurrentCursorPosition()
        {
            return Cursor.Position;
        }

        public static int GetScreenCount()
        {
            return Screen.AllScreens.Length;
        }

        public static Screen[] GetAllScreens()
        {
            return Screen.AllScreens;
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
        
        public static string GetCurrentActiveApplicationName()
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

        public static string GetActiveWindowTitle()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                StringBuilder windowTitle = new StringBuilder(256);
                GetWindowText(hwnd, windowTitle, windowTitle.Capacity);
                return windowTitle.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}