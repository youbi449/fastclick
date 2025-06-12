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
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref System.Drawing.Rectangle lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(IntPtr lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x20;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x80;

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
                // Save current cursor position
                System.Drawing.Point originalPosition;
                GetCursorPos(out originalPosition);

                // Lock cursor to a tiny area around original position to prevent interference
                var lockRect = new System.Drawing.Rectangle(
                    originalPosition.X - 1, 
                    originalPosition.Y - 1, 
                    2, 
                    2
                );
                ClipCursor(ref lockRect);

                // Move cursor to target position
                var screenPosition = GetScreenPosition(point);
                
                // Temporarily unlock to allow movement to target
                ClipCursor(IntPtr.Zero);
                SetCursorPos(screenPosition.X, screenPosition.Y);

                // Lock cursor at target position during action
                var targetLockRect = new System.Drawing.Rectangle(
                    screenPosition.X - 1, 
                    screenPosition.Y - 1, 
                    2, 
                    2
                );
                ClipCursor(ref targetLockRect);

                // Small delay to ensure cursor movement
                Thread.Sleep(10);

                // Execute the action based on repeat count
                for (int i = 0; i < point.RepeatCount; i++)
                {
                    if (i > 0 && point.DelayMs > 0)
                    {
                        Thread.Sleep(point.DelayMs);
                    }

                    ExecuteSingleAction(point.Action, screenPosition.X, screenPosition.Y);
                }

                // Unlock cursor and restore original position
                ClipCursor(IntPtr.Zero);
                SetCursorPos(originalPosition.X, originalPosition.Y);
            }
            catch (Exception ex)
            {
                // Make sure cursor is unlocked even if there's an error
                ClipCursor(IntPtr.Zero);
                System.Diagnostics.Debug.WriteLine($"Error executing action: {ex.Message}");
            }
        }

        private void ExecuteSingleAction(MouseAction action, int x, int y)
        {
            switch (action)
            {
                case MouseAction.LeftClick:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
                    break;

                case MouseAction.RightClick:
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)x, (uint)y, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_RIGHTUP, (uint)x, (uint)y, 0, 0);
                    break;

                case MouseAction.DoubleClick:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
                    Thread.Sleep(10);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
                    break;

                case MouseAction.MouseDown:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                    break;

                case MouseAction.MouseUp:
                    mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
                    break;

                case MouseAction.MiddleClick:
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, (uint)x, (uint)y, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_MIDDLEUP, (uint)x, (uint)y, 0, 0);
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