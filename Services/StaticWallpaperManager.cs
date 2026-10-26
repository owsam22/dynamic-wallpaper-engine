using System;
using System.IO;
using System.Runtime.InteropServices;
using SmartWallpaperApp.Core;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.Services
{
    public class StaticWallpaperManager
    {
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
        private string? _lastAppliedPath;
        private string? _lastAppliedStyle;

        public bool ApplyWallpaper(string imagePath, string style, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                Logger.Warn("ApplyWallpaper failed: Image path is empty.");
                return false;
            }

            if (!File.Exists(imagePath))
            {
                Logger.Warn($"ApplyWallpaper failed: File does not exist at '{imagePath}'.");
                return false;
            }

            string ext = Path.GetExtension(imagePath).ToLowerInvariant();
            if (Array.IndexOf(SupportedExtensions, ext) < 0)
            {
                Logger.Warn($"ApplyWallpaper failed: Unsupported image extension '{ext}'.");
                return false;
            }

            if (!force && string.Equals(_lastAppliedPath, imagePath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_lastAppliedStyle, style, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Debug("Static wallpaper already applied with identical path and style. Skipping.");
                return true;
            }

            try
            {
                // Update registry style first
                WinApiHelpers.SetWallpaperStyle(style);

                // Call SystemParametersInfo to trigger shell update
                bool result = WinApiHelpers.SystemParametersInfo(
                    WinApiHelpers.SPI_SETDESKWALLPAPER,
                    0,
                    imagePath,
                    WinApiHelpers.SPIF_UPDATEINIFILE | WinApiHelpers.SPIF_SENDCHANGE
                );

                if (result)
                {
                    _lastAppliedPath = imagePath;
                    _lastAppliedStyle = style;
                    Logger.Info($"Static wallpaper successfully applied: '{imagePath}' (Style: {style})");
                    return true;
                }
                else
                {
                    int err = Marshal.GetLastWin32Error();
                    Logger.Error($"SystemParametersInfo failed with Win32 error code {err}.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Exception applying static wallpaper", ex);
                return false;
            }
        }

        public void InvalidateCache()
        {
            _lastAppliedPath = null;
            _lastAppliedStyle = null;
        }
    }
}
