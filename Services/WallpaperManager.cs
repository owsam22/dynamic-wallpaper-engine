using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using SmartWallpaperApp.Core;
using SmartWallpaperApp.Models;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.Services
{
    public enum WallpaperMode
    {
        Unknown,
        Static,
        Live,
        TransitioningToStatic,
        TransitioningToLive,
        Paused
    }

    public class WallpaperManager : IDisposable
    {
        private readonly SettingsManager _settingsManager;
        private readonly StaticWallpaperManager _staticManager;
        private readonly WebWallpaperEngine _liveEngine;
        private readonly SemaphoreSlim _transitionLock = new(1, 1);

        private WallpaperMode _currentMode = WallpaperMode.Unknown;
        private bool _isAutomationPaused;
        private PowerStatusInfo? _lastPowerStatus;

        public WallpaperMode CurrentMode => _currentMode;
        public bool IsAutomationPaused => _isAutomationPaused;
        public string LastTransitionMessage { get; private set; } = "Initialized";
        public DateTime LastTransitionTime { get; private set; } = DateTime.Now;

        public event Action<WallpaperMode, string>? StateChanged;

        public WallpaperManager(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            _staticManager = new StaticWallpaperManager();
            _liveEngine = new WebWallpaperEngine();

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        public void SetAutomationPaused(bool paused)
        {
            _isAutomationPaused = paused;
            LastTransitionMessage = paused ? "Automation paused by user" : "Automation resumed";
            Logger.Info(LastTransitionMessage);
            StateChanged?.Invoke(_currentMode, LastTransitionMessage);
        }

        public async Task HandlePowerChangeAsync(PowerStatusInfo status, bool force = false)
        {
            _lastPowerStatus = status;

            if (_isAutomationPaused && !force)
            {
                Logger.Debug("HandlePowerChangeAsync ignored: Automation is paused.");
                return;
            }

            if (!_settingsManager.CurrentSettings.AutoSwitchEnabled && !force)
            {
                Logger.Debug("HandlePowerChangeAsync ignored: AutoSwitch is disabled.");
                return;
            }

            // Decide desired mode
            bool shouldBeLive = status.IsAcConnected;

            // Check low battery rule
            int threshold = _settingsManager.CurrentSettings.PauseBelowBatteryPercent;
            if (shouldBeLive && threshold > 0 && status.BatteryPercent.HasValue && status.BatteryPercent.Value < threshold)
            {
                Logger.Info($"Battery is below configured threshold ({status.BatteryPercent.Value}% < {threshold}%). Preferring static wallpaper.");
                shouldBeLive = false;
            }

            if (shouldBeLive)
            {
                await TransitionToLiveAsync(force);
            }
            else
            {
                await TransitionToStaticAsync(force);
            }
        }

        public async Task ForceApplyCurrentPowerModeAsync()
        {
            if (_lastPowerStatus != null)
            {
                await HandlePowerChangeAsync(_lastPowerStatus, force: true);
            }
            else
            {
                await TransitionToStaticAsync(force: true);
            }
        }

        public async Task ForceLiveModeAsync()
        {
            Logger.Info("Force Live mode requested.");
            await TransitionToLiveAsync(force: true);
        }

        public async Task ForceStaticModeAsync()
        {
            Logger.Info("Force Static mode requested.");
            await TransitionToStaticAsync(force: true);
        }

        public async Task TransitionToLiveAsync(bool force = false)
        {
            var settings = _settingsManager.CurrentSettings;

            // Wait for any previous transition to cleanly finish without dropping events
            await _transitionLock.WaitAsync();

            try
            {
                if (!settings.HasLiveWallpaper)
                {
                    _currentMode = WallpaperMode.Unknown;
                    LastTransitionMessage = "No live wallpaper selected. Open Settings to select one.";
                    LastTransitionTime = DateTime.Now;
                    Logger.Info(LastTransitionMessage);
                    StateChanged?.Invoke(_currentMode, LastTransitionMessage);
                    return;
                }

                if (!force && _currentMode == WallpaperMode.Live && _liveEngine.IsRunning &&
                    string.Equals(_liveEngine.CurrentSource, settings.LiveSourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Debug("Already in Live mode with current source. No transition needed.");
                    return;
                }

                _currentMode = WallpaperMode.TransitioningToLive;
                StateChanged?.Invoke(_currentMode, "Starting Live Wallpaper...");

                Logger.Info($"Beginning transition to Live Wallpaper ({settings.LiveSourceType}: {settings.LiveSourcePath})");

                bool success = await _liveEngine.StartAsync(settings.LiveSourceType, settings.LiveSourcePath, forceReload: force);

                if (success)
                {
                    _currentMode = WallpaperMode.Live;
                    LastTransitionMessage = $"Live mode active ({Path.GetFileName(settings.LiveSourcePath)})";
                    LastTransitionTime = DateTime.Now;
                    Logger.Info(LastTransitionMessage);
                }
                else
                {
                    Logger.Warn("Failed to initialize Live Wallpaper. Falling back to configured static wallpaper if available.");
                    _liveEngine.Stop();

                    if (settings.HasStaticWallpaper)
                    {
                        bool fallbackSuccess = _staticManager.ApplyWallpaper(settings.StaticImagePath, settings.StaticWallpaperStyle, force: true);
                        _currentMode = WallpaperMode.Static;
                        LastTransitionMessage = fallbackSuccess
                            ? "Live wallpaper failed; fallback static wallpaper active"
                            : "Live wallpaper failed; static fallback also failed";
                    }
                    else
                    {
                        _currentMode = WallpaperMode.Unknown;
                        LastTransitionMessage = "Live wallpaper failed to start and no static wallpaper is configured.";
                    }
                    LastTransitionTime = DateTime.Now;
                }

                StateChanged?.Invoke(_currentMode, LastTransitionMessage);
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during TransitionToLiveAsync", ex);
                _currentMode = WallpaperMode.Unknown;
                LastTransitionMessage = $"Error: {ex.Message}";
                StateChanged?.Invoke(_currentMode, LastTransitionMessage);
            }
            finally
            {
                _transitionLock.Release();
            }
        }

        public async Task TransitionToStaticAsync(bool force = false)
        {
            var settings = _settingsManager.CurrentSettings;

            // Wait for any previous transition to cleanly finish without dropping events
            await _transitionLock.WaitAsync();

            try
            {
                _currentMode = WallpaperMode.TransitioningToStatic;
                StateChanged?.Invoke(_currentMode, "Switching to Static Wallpaper...");

                Logger.Info("Stopping Live Wallpaper engine and releasing resources...");
                _liveEngine.Stop();

                if (!settings.HasStaticWallpaper)
                {
                    _currentMode = WallpaperMode.Static;
                    LastTransitionMessage = "No static wallpaper selected. Open Settings to select one.";
                    LastTransitionTime = DateTime.Now;
                    Logger.Info(LastTransitionMessage);
                    StateChanged?.Invoke(_currentMode, LastTransitionMessage);
                    return;
                }

                bool success = _staticManager.ApplyWallpaper(settings.StaticImagePath, settings.StaticWallpaperStyle, force);

                _currentMode = WallpaperMode.Static;
                LastTransitionMessage = success ? $"Static mode active ({Path.GetFileName(settings.StaticImagePath)})" : "Static wallpaper application failed";
                LastTransitionTime = DateTime.Now;
                Logger.Info(LastTransitionMessage);

                StateChanged?.Invoke(_currentMode, LastTransitionMessage);
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during TransitionToStaticAsync", ex);
                _currentMode = WallpaperMode.Static;
                LastTransitionMessage = $"Error: {ex.Message}";
                StateChanged?.Invoke(_currentMode, LastTransitionMessage);
            }
            finally
            {
                _transitionLock.Release();
            }
        }

        public void HandleFullscreenState(bool isFullscreen)
        {
            if (!_settingsManager.CurrentSettings.PauseOnFullscreen) return;

            if (_currentMode == WallpaperMode.Live && _liveEngine.IsRunning)
            {
                if (isFullscreen)
                {
                    Logger.Info("Fullscreen application detected. Suspending live wallpaper engine.");
                    _liveEngine.Suspend();
                }
                else
                {
                    Logger.Info("Fullscreen application exited. Resuming live wallpaper engine.");
                    _liveEngine.Resume();
                }
            }
        }

        public void HandleExplorerRestart()
        {
            Logger.Warn("Windows Explorer restart detected. Re-embedding or restoring wallpaper...");
            if (_currentMode == WallpaperMode.Live && _liveEngine.IsRunning)
            {
                _liveEngine.Reattach();
            }
            else
            {
                _staticManager.InvalidateCache();
                _staticManager.ApplyWallpaper(
                    _settingsManager.CurrentSettings.StaticImagePath,
                    _settingsManager.CurrentSettings.StaticWallpaperStyle,
                    force: true
                );
            }
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            Logger.Info("Display settings changed (resolution or monitor layout). Updating wallpaper...");
            if (_currentMode == WallpaperMode.Live && _liveEngine.IsRunning)
            {
                _liveEngine.Reattach();
            }
        }

        public void Dispose()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _liveEngine.Dispose();
            _transitionLock.Dispose();
        }
    }
}
