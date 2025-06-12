using System;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace FastClick.Models
{
    public class PointConfig
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string HotkeyText { get; set; } = "";
        public Keys Modifiers { get; set; }
        public Keys Key { get; set; }
        public MouseAction Action { get; set; }
        public int RepeatCount { get; set; } = 1;
        public int DelayMs { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;
        public string Name { get; set; } = "";
        public int ScreenIndex { get; set; } = 0;
        public string TargetApplicationName { get; set; } = "";

        [JsonIgnore]
        public string DisplayText => $"{Name} ({X}, {Y}) - {HotkeyText} - {Action}";

        public PointConfig()
        {
            Action = MouseAction.LeftClick;
        }

        public PointConfig(int x, int y, string name = "")
        {
            X = x;
            Y = y;
            Name = string.IsNullOrEmpty(name) ? $"Point {x},{y}" : name;
            Action = MouseAction.LeftClick;
        }
    }

    public enum MouseAction
    {
        LeftClick,
        RightClick,
        DoubleClick,
        MouseDown,
        MouseUp,
        MiddleClick
    }
}