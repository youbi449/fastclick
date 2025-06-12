using System;
using System.Collections.ObjectModel;
using System.IO;
using FastClick.Models;
using Newtonsoft.Json;

namespace FastClick.Services
{
    public class ConfigManager
    {
        private const string ConfigFileName = "fastclick_config.json";
        private readonly string _configPath;

        public ConfigManager()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "FastClick");
            
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
                
            _configPath = Path.Combine(appFolder, ConfigFileName);
        }

        public ObservableCollection<PointConfig> LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return new ObservableCollection<PointConfig>();

                var json = File.ReadAllText(_configPath);
                var points = JsonConvert.DeserializeObject<ObservableCollection<PointConfig>>(json);
                return points ?? new ObservableCollection<PointConfig>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
                return new ObservableCollection<PointConfig>();
            }
        }

        public void SaveConfig(ObservableCollection<PointConfig> points)
        {
            try
            {
                var json = JsonConvert.SerializeObject(points, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
                throw;
            }
        }

        public string GetConfigPath() => _configPath;

        public void ExportConfig(string filePath, ObservableCollection<PointConfig> points)
        {
            var json = JsonConvert.SerializeObject(points, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public ObservableCollection<PointConfig> ImportConfig(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<ObservableCollection<PointConfig>>(json) 
                   ?? new ObservableCollection<PointConfig>();
        }
    }
}