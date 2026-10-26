using System;
using System.Text;
using System.Windows.Forms;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.Core
{
    public class FullscreenDetector : IDisposable
    {
        private readonly System.Windows.Forms.Timer _checkTimer;
        private bool _lastFullscreenState;
        public bool IsFullscreenActive => _lastFullscreenState;

        public event Action<bool>? FullscreenStateChanged;

        public FullscreenDetector()
        {
            _checkTimer = new System.Windows.Forms.Timer
            {
                Interval = 2500
            };
            _checkTimer.Tick += OnTimerTick;
        }

        public void Start() => _checkTimer.Start();
        public void Stop() => _checkTimer.Stop();

        private void OnTimerTick(object? sender, EventArgs e)
        {
            bool isFs = CheckIsForegroundFullscreen();
            if (isFs != _lastFullscreenState)
            {
                _lastFullscreenState = isFs;
                Logger.Info($"Fullscreen application state changed: IsFullscreen = {isFs}");
                FullscreenStateChanged?.Invoke(isFs);
            }
        }

        public static bool CheckIsForegroundFullscreen()
        {
            try
            {
                IntPtr fgHwnd = WinApiHelpers.GetForegroundWindow();
                if (fgHwnd == IntPtr.Zero) return false;

                IntPtr shellHwnd = WinApiHelpers.GetShellWindow();
                IntPtr desktopHwnd = WinApiHelpers.GetDesktopWindow();
                if (fgHwnd == shellHwnd || fgHwnd == desktopHwnd) return false;

                // Check class name to ignore taskbars and desktop shell
                var classNameBuilder = new StringBuilder(256);
                WinApiHelpers.GetClassName(fgHwnd, classNameBuilder, classNameBuilder.Capacity);
                string className = classNameBuilder.ToString();

                if (className == "Progman" ||
                    className == "WorkerW" ||
                    className == "Shell_TrayWnd" ||
                    className == "Shell_SecondaryTrayWnd")
                {
                    return false;
                }

                if (WinApiHelpers.GetWindowRect(fgHwnd, out var rect))
                {
                    // Check against any monitor bounds
                    foreach (var screen in Screen.AllScreens)
                    {
                        if (rect.Left <= screen.Bounds.Left &&
                            rect.Top <= screen.Bounds.Top &&
                            rect.Right >= screen.Bounds.Right &&
                            rect.Bottom >= screen.Bounds.Bottom)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Fullscreen detection check failed: {ex.Message}");
            }

            return false;
        }

        public void Dispose()
        {
            _checkTimer.Stop();
            _checkTimer.Dispose();
        }
    }
}
