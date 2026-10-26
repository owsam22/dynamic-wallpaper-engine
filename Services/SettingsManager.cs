using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using SmartWallpaperApp.Models;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.Services
{
    public class SettingsManager
    {
        private static readonly string SettingsFolder;
        private static readonly string SettingsFilePath;
        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "SmartWallpaperApp";

        static SettingsManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            SettingsFolder = Path.Combine(appData, "SmartWallpaperApp");
            SettingsFilePath = Path.Combine(SettingsFolder, "settings.json");
        }

        public AppSettings CurrentSettings { get; private set; } = new();

        public event Action<AppSettings>? SettingsChanged;

        public SettingsManager()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loaded != null)
                    {
                        CurrentSettings = loaded;
                    }
                }
                else
                {
                    // Create default settings.json file on first run
                    ValidateAndApplyDefaults();
                    Save(CurrentSettings);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to read settings.json. Using defaults.", ex);
                CurrentSettings = new AppSettings();
            }

            ValidateAndApplyDefaults();
        }

        public bool Save(AppSettings settings)
        {
            try
            {
                CurrentSettings = settings;
                ValidateAndApplyDefaults();

                Directory.CreateDirectory(SettingsFolder);
                string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SettingsFilePath, json);
                ApplyStartupRegistration(CurrentSettings.StartWithWindows);
                SettingsChanged?.Invoke(CurrentSettings);
                Logger.Info("Settings saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings.", ex);
                return false;
            }
        }

        private void ValidateAndApplyDefaults()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string defaultHtml = Path.Combine(baseDir, "Assets", "example_wallpaper.html");
            string defaultImg = Path.Combine(baseDir, "Assets", "default_wallpaper.png");

            if (string.IsNullOrWhiteSpace(CurrentSettings.LiveSourcePath) ||
                (CurrentSettings.LiveSourceType == "LocalHtml" && !File.Exists(CurrentSettings.LiveSourcePath)))
            {
                CurrentSettings.LiveSourceType = "LocalHtml";
                CurrentSettings.LiveSourcePath = File.Exists(defaultHtml) ? defaultHtml : "";
            }

            if (string.IsNullOrWhiteSpace(CurrentSettings.StaticImagePath) || !File.Exists(CurrentSettings.StaticImagePath))
            {
                CurrentSettings.StaticImagePath = File.Exists(defaultImg) ? defaultImg : "";
            }

            if (CurrentSettings.PollingIntervalSeconds < 1)
            {
                CurrentSettings.PollingIntervalSeconds = 10;
            }

            if (CurrentSettings.PauseBelowBatteryPercent < 0 || CurrentSettings.PauseBelowBatteryPercent > 100)
            {
                CurrentSettings.PauseBelowBatteryPercent = 0;
            }
        }

        public void ApplyStartupRegistration(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string exePath = Environment.ProcessPath ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppRegistryName, $"\"{exePath}\"");
                        Logger.Info($"Registered startup entry: {exePath}");
                    }
                }
                else
                {
                    key.DeleteValue(AppRegistryName, false);
                    Logger.Info("Removed startup entry.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to configure Windows startup registry entry.", ex);
            }
        }
    }
}
