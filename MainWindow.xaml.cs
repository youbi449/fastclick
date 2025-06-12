using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using FastClick.Models;
using FastClick.Services;
using FastClick.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace FastClick
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ConfigManager _configManager;
        private readonly HotkeyManager _hotkeyManager;
        private readonly ActionExecutor _actionExecutor;
        private TaskbarIcon _taskbarIcon;
        private bool _isClosingToTray = false;

        public ObservableCollection<PointConfig> Points { get; set; }
        
        private PointConfig _selectedPoint;
        public PointConfig SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                _selectedPoint = value;
                OnPropertyChanged(nameof(SelectedPoint));
            }
        }

        public List<MouseAction> AvailableActions { get; set; }
        public List<int> AvailableScreens { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _configManager = new ConfigManager();
            _hotkeyManager = new HotkeyManager();
            _actionExecutor = new ActionExecutor();

            // Initialize dropdown data
            AvailableActions = new List<MouseAction>
            {
                MouseAction.LeftClick,
                MouseAction.RightClick,
                MouseAction.DoubleClick,
                MouseAction.MiddleClick,
                MouseAction.MouseDown,
                MouseAction.MouseUp
            };

            // Initialize available screens (0-based indexing)
            AvailableScreens = new List<int>();
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                AvailableScreens.Add(i);
            }

            Points = _configManager.LoadConfig();
            
            InitializeSystemTray();
            RegisterHotkeys();

            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            if (EnabledCheckBox != null)
            {
                EnabledCheckBox.Checked += (s, e) => RegisterHotkeys();
                EnabledCheckBox.Unchecked += (s, e) => UnregisterHotkeys();
            }
        }

        private void InitializeSystemTray()
        {
            _taskbarIcon = new TaskbarIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                ToolTipText = "FastClick - Click automation tool"
            };

            var contextMenu = new System.Windows.Controls.ContextMenu();
            
            var showItem = new System.Windows.Controls.MenuItem { Header = "Show" };
            showItem.Click += (s, e) => ShowWindow();
            
            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => Close();
            
            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            contextMenu.Items.Add(exitItem);
            
            _taskbarIcon.ContextMenu = contextMenu;
            _taskbarIcon.TrayLeftMouseDown += (s, e) => ShowWindow();
        }

        private void RegisterHotkeys()
        {
            _hotkeyManager.UnregisterAll();
            
            if (EnabledCheckBox.IsChecked == true)
            {
                foreach (var point in Points.Where(p => p.IsEnabled && p.Key != Keys.None))
                {
                    _hotkeyManager.RegisterHotkey(point.Modifiers, point.Key, point);
                }
            }
        }

        private void UnregisterHotkeys()
        {
            _hotkeyManager.UnregisterAll();
        }

        private void OnHotkeyPressed(object sender, HotkeyEventArgs e)
        {
            var point = e.Tag as PointConfig;
            if (point != null && point.IsEnabled)
            {
                _actionExecutor.ExecuteAction(point);
                UpdateStatus($"Executed {point.Action} at ({point.X}, {point.Y})");
            }
        }

        private void AddPoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new PointCaptureWindow();
                if (dialog.ShowDialog() == true)
                {
                    var newPoint = dialog.CapturedPoint;
                    Points.Add(newPoint);
                    SaveConfig();
                    SelectedPoint = newPoint;
                    UpdateStatus("Point added successfully");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error adding point: {ex.Message}");
            }
        }

        private void EditPoint_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPoint == null) return;

            var dialog = new PointEditWindow(SelectedPoint);
            if (dialog.ShowDialog() == true)
            {
                SaveConfig();
                RegisterHotkeys();
                UpdateStatus("Point updated successfully");
            }
        }

        private void DeletePoint_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPoint == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete '{SelectedPoint.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Points.Remove(SelectedPoint);
                SaveConfig();
                RegisterHotkeys();
                UpdateStatus("Point deleted");
            }
        }

        private void TestPoint_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPoint == null) return;

            _actionExecutor.ExecuteAction(SelectedPoint);
            UpdateStatus($"Test executed: {SelectedPoint.Action} at ({SelectedPoint.X}, {SelectedPoint.Y})");
        }

        private void SetHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPoint == null) return;

            var dialog = new HotkeyInputWindow();
            if (dialog.ShowDialog() == true)
            {
                SelectedPoint.Modifiers = dialog.Modifiers;
                SelectedPoint.Key = dialog.Key;
                SelectedPoint.HotkeyText = dialog.HotkeyText;
                SaveConfig();
                RegisterHotkeys();
                OnPropertyChanged(nameof(SelectedPoint));
                UpdateStatus("Hotkey updated");
            }
        }

        private void GetCurrentApp_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPoint == null) return;

            var currentApp = ActionExecutor.GetCurrentActiveApplicationName();
            var windowTitle = ActionExecutor.GetActiveWindowTitle();
            
            if (!string.IsNullOrEmpty(currentApp))
            {
                SelectedPoint.TargetApplicationName = currentApp;
                OnPropertyChanged(nameof(SelectedPoint));
                SaveConfig();
                UpdateStatus($"Target app set to: {currentApp} ({windowTitle})");
            }
            else
            {
                UpdateStatus("Could not detect current application");
            }
        }

        private void ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Import FastClick Configuration"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var importedPoints = _configManager.ImportConfig(dialog.FileName);
                    Points.Clear();
                    foreach (var point in importedPoints)
                        Points.Add(point);
                    
                    RegisterHotkeys();
                    UpdateStatus($"Imported {importedPoints.Count} points");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error importing config: {ex.Message}", 
                        "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Export FastClick Configuration",
                FileName = "fastclick_config.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _configManager.ExportConfig(dialog.FileName, Points);
                    UpdateStatus($"Configuration exported to {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error exporting config: {ex.Message}", 
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "FastClick v1.0\n\nAutomated mouse clicking with global hotkeys.\n\nPress hotkeys to trigger mouse actions at predefined screen positions.",
                "About FastClick",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            HideWindow();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _taskbarIcon.Visibility = Visibility.Hidden;
        }

        private void HideWindow()
        {
            _isClosingToTray = true;
            Hide();
            _taskbarIcon.Visibility = Visibility.Visible;
            _taskbarIcon.ShowBalloonTip("FastClick", "Application minimized to tray", BalloonIcon.Info);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isClosingToTray)
            {
                var result = System.Windows.MessageBox.Show(
                    "Do you want to minimize to system tray instead of closing?",
                    "FastClick",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    HideWindow();
                    return;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            SaveConfig();
            UnregisterHotkeys();
            _taskbarIcon?.Dispose();
            base.OnClosing(e);
        }

        private void SaveConfig()
        {
            _configManager.SaveConfig(Points);
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}