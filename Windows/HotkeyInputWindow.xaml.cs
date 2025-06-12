using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace FastClick.Windows
{
    public partial class HotkeyInputWindow : Window
    {
        public Keys Modifiers { get; private set; }
        public Keys Key { get; private set; }
        public string HotkeyText { get; private set; }

        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        public HotkeyInputWindow()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }

        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var key = ConvertToFormsKey(e.Key);
            if (key != Keys.None)
            {
                _pressedKeys.Add(key);
                UpdateHotkeyDisplay();
            }
        }

        private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_pressedKeys.Count > 0)
            {
                ProcessHotkey();
            }
        }

        private void ProcessHotkey()
        {
            var modifiers = Keys.None;
            var mainKey = Keys.None;

            foreach (var key in _pressedKeys)
            {
                switch (key)
                {
                    case Keys.Control:
                    case Keys.ControlKey:
                    case Keys.LControlKey:
                    case Keys.RControlKey:
                        modifiers |= Keys.Control;
                        break;
                    case Keys.Alt:
                    case Keys.Menu:
                    case Keys.LMenu:
                    case Keys.RMenu:
                        modifiers |= Keys.Alt;
                        break;
                    case Keys.Shift:
                    case Keys.ShiftKey:
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                        modifiers |= Keys.Shift;
                        break;
                    case Keys.LWin:
                    case Keys.RWin:
                        modifiers |= Keys.LWin;
                        break;
                    default:
                        if (mainKey == Keys.None)
                            mainKey = key;
                        break;
                }
            }

            if (mainKey != Keys.None)
            {
                Modifiers = modifiers;
                Key = mainKey;
                HotkeyText = BuildHotkeyString(modifiers, mainKey);
                HotkeyTextBox.Text = HotkeyText;
            }

            _pressedKeys.Clear();
        }

        private void UpdateHotkeyDisplay()
        {
            var displayKeys = _pressedKeys.Select(k => GetKeyDisplayName(k)).ToList();
            HotkeyTextBox.Text = string.Join(" + ", displayKeys);
        }

        private string BuildHotkeyString(Keys modifiers, Keys key)
        {
            var parts = new List<string>();

            if (modifiers.HasFlag(Keys.Control))
                parts.Add("Ctrl");
            if (modifiers.HasFlag(Keys.Alt))
                parts.Add("Alt");
            if (modifiers.HasFlag(Keys.Shift))
                parts.Add("Shift");
            if (modifiers.HasFlag(Keys.LWin))
                parts.Add("Win");

            parts.Add(GetKeyDisplayName(key));

            return string.Join(" + ", parts);
        }

        private string GetKeyDisplayName(Keys key)
        {
            switch (key)
            {
                case Keys.Control:
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                    return "Ctrl";
                case Keys.Alt:
                case Keys.Menu:
                case Keys.LMenu:
                case Keys.RMenu:
                    return "Alt";
                case Keys.Shift:
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                    return "Shift";
                case Keys.LWin:
                case Keys.RWin:
                    return "Win";
                case Keys.D0: return "0";
                case Keys.D1: return "1";
                case Keys.D2: return "2";
                case Keys.D3: return "3";
                case Keys.D4: return "4";
                case Keys.D5: return "5";
                case Keys.D6: return "6";
                case Keys.D7: return "7";
                case Keys.D8: return "8";
                case Keys.D9: return "9";
                default:
                    return key.ToString();
            }
        }

        private Keys ConvertToFormsKey(Key wpfKey)
        {
            return (Keys)KeyInterop.VirtualKeyFromKey(wpfKey);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (Key != Keys.None)
            {
                DialogResult = true;
            }
            else
            {
                System.Windows.MessageBox.Show("Please press a key combination first.", "No Hotkey Set", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Modifiers = Keys.None;
            Key = Keys.None;
            HotkeyText = "";
            HotkeyTextBox.Text = "Press keys...";
            _pressedKeys.Clear();
        }
    }
}