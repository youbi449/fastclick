using System;
using System.Windows;
using System.Windows.Forms;
using FastClick.Models;
using FastClick.Services;

namespace FastClick.Windows
{
    public partial class PointEditWindow : Window
    {
        private readonly PointConfig _point;

        public PointEditWindow(PointConfig point)
        {
            InitializeComponent();
            _point = point;
            
            InitializeControls();
            LoadPointData();
        }

        private void InitializeControls()
        {
            ActionComboBox.ItemsSource = Enum.GetValues(typeof(MouseAction));
        }

        private void LoadPointData()
        {
            NameTextBox.Text = _point.Name;
            XTextBox.Text = _point.X.ToString();
            YTextBox.Text = _point.Y.ToString();
            ActionComboBox.SelectedItem = _point.Action;
            HotkeyTextBox.Text = _point.HotkeyText;
            RepeatTextBox.Text = _point.RepeatCount.ToString();
            DelayTextBox.Text = _point.DelayMs.ToString();
            TargetAppTextBox.Text = _point.TargetApplicationName;
            EnabledCheckBox.IsChecked = _point.IsEnabled;
        }

        private void SetHotkey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HotkeyInputWindow();
            if (dialog.ShowDialog() == true)
            {
                _point.Modifiers = dialog.Modifiers;
                _point.Key = dialog.Key;
                _point.HotkeyText = dialog.HotkeyText;
                HotkeyTextBox.Text = dialog.HotkeyText;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                SavePointData();
                DialogResult = true;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please enter a name for the point.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(XTextBox.Text, out _) || !int.TryParse(YTextBox.Text, out _))
            {
                System.Windows.MessageBox.Show("Please enter valid X and Y coordinates.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(RepeatTextBox.Text, out int repeat) || repeat < 1)
            {
                System.Windows.MessageBox.Show("Repeat count must be a positive integer.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(DelayTextBox.Text, out int delay) || delay < 0)
            {
                System.Windows.MessageBox.Show("Delay must be a non-negative integer.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void SavePointData()
        {
            _point.Name = NameTextBox.Text;
            _point.X = int.Parse(XTextBox.Text);
            _point.Y = int.Parse(YTextBox.Text);
            _point.Action = (MouseAction)ActionComboBox.SelectedItem;
            _point.RepeatCount = int.Parse(RepeatTextBox.Text);
            _point.DelayMs = int.Parse(DelayTextBox.Text);
            _point.TargetApplicationName = TargetAppTextBox.Text?.Trim() ?? "";
            _point.IsEnabled = EnabledCheckBox.IsChecked ?? true;
        }

        private void GetCurrentApp_Click(object sender, RoutedEventArgs e)
        {
            var currentApp = ActionExecutor.GetCurrentActiveApplicationName();
            var windowTitle = ActionExecutor.GetActiveWindowTitle();
            
            if (!string.IsNullOrEmpty(currentApp))
            {
                TargetAppTextBox.Text = currentApp;
                System.Windows.MessageBox.Show($"Target app set to: {currentApp}\nWindow: {windowTitle}", 
                    "App Detected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Could not detect current application", 
                    "Detection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}