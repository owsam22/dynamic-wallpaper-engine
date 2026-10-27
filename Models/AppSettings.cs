using System;

namespace SmartWallpaperApp.Models
{
    public class AppSettings
    {
        public string StaticImagePath { get; set; } = string.Empty;
        public string StaticWallpaperStyle { get; set; } = "Fill";

        public string LiveSourceType { get; set; } = "LocalFile"; // "LocalFile", "LocalHtml", or "WebsiteUrl"
        public string LiveSourcePath { get; set; } = string.Empty;

        public bool AutoSwitchEnabled { get; set; } = true;
        public int PollingIntervalSeconds { get; set; } = 10;
        public bool PauseOnFullscreen { get; set; } = true;
        public int PauseBelowBatteryPercent { get; set; } = 0; // 0 = disabled

        public string SimulationMode { get; set; } = "Actual"; // "Actual", "SimulatedAC", "SimulatedBattery"
        public int SimulatedBatteryPercent { get; set; } = 85;

        public bool StartWithWindows { get; set; } = false;

        public bool HasStaticWallpaper => !string.IsNullOrWhiteSpace(StaticImagePath) && System.IO.File.Exists(StaticImagePath);
        public bool HasLiveWallpaper => !string.IsNullOrWhiteSpace(LiveSourcePath) &&
            (LiveSourceType == "WebsiteUrl" || System.IO.File.Exists(LiveSourcePath));
    }
}
