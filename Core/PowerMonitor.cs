using System;
using System.Windows.Forms;
using SmartWallpaperApp.Models;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.Core
{
    public class PowerStatusInfo
    {
        public bool IsAcConnected { get; init; }
        public int? BatteryPercent { get; init; }
        public bool IsUnknown { get; init; }
        public bool IsSimulated { get; init; }
        public string StatusSummary { get; init; } = string.Empty;
    }

    public class PowerMonitor : IDisposable
    {
        public const int WM_POWERBROADCAST = 0x0218;
        public const int PBT_APMPOWERSTATUSCHANGE = 0x000A;
        public const int PBT_POWERSETTINGCHANGE = 0x8013;

        private readonly System.Windows.Forms.Timer _pollingTimer;
        private readonly System.Windows.Forms.Timer _debounceTimer;
        private AppSettings _settings;
        private PowerStatusInfo _lastReportedStatus;

        public event Action<PowerStatusInfo>? PowerStateChanged;

        public PowerStatusInfo CurrentStatus => _lastReportedStatus;

        public PowerMonitor(AppSettings settings)
        {
            _settings = settings;

            _pollingTimer = new System.Windows.Forms.Timer
            {
                Interval = Math.Max(1, _settings.PollingIntervalSeconds) * 1000
            };
            _pollingTimer.Tick += (_, _) => CheckStatus();

            _debounceTimer = new System.Windows.Forms.Timer
            {
                Interval = 500
            };
            _debounceTimer.Tick += OnDebounceTick;

            _lastReportedStatus = QueryPowerStatus();
        }

        public void Start()
        {
            _pollingTimer.Start();
            Logger.Info($"PowerMonitor started. Polling every {_pollingTimer.Interval / 1000}s.");
            // Immediately notify initial status
            _lastReportedStatus = QueryPowerStatus();
            PowerStateChanged?.Invoke(_lastReportedStatus);
        }

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings;
            _pollingTimer.Interval = Math.Max(1, _settings.PollingIntervalSeconds) * 1000;
            CheckStatus();
        }

        public void HandlePowerBroadcast(int wParam)
        {
            if (wParam == PBT_APMPOWERSTATUSCHANGE || wParam == PBT_POWERSETTINGCHANGE)
            {
                Logger.Debug($"WM_POWERBROADCAST received (wParam: 0x{wParam:X}). Triggering debounced status check.");
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }

        private void OnDebounceTick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            CheckStatus();
        }

        public void CheckStatus(bool forceNotify = false)
        {
            var newStatus = QueryPowerStatus();

            bool changed = forceNotify ||
                           newStatus.IsAcConnected != _lastReportedStatus.IsAcConnected ||
                           newStatus.IsUnknown != _lastReportedStatus.IsUnknown ||
                           newStatus.IsSimulated != _lastReportedStatus.IsSimulated ||
                           Math.Abs((newStatus.BatteryPercent ?? 0) - (_lastReportedStatus.BatteryPercent ?? 0)) >= 5;

            if (changed)
            {
                Logger.Info($"Power status updated: {newStatus.StatusSummary} (Simulated: {newStatus.IsSimulated})");
                _lastReportedStatus = newStatus;
                PowerStateChanged?.Invoke(newStatus);
            }
        }

        public PowerStatusInfo QueryPowerStatus()
        {
            // Check for simulation override first
            if (_settings.SimulationMode == "SimulatedAC")
            {
                return new PowerStatusInfo
                {
                    IsAcConnected = true,
                    BatteryPercent = _settings.SimulatedBatteryPercent,
                    IsUnknown = false,
                    IsSimulated = true,
                    StatusSummary = $"[SIMULATED] AC Connected (Battery: {_settings.SimulatedBatteryPercent}%)"
                };
            }
            else if (_settings.SimulationMode == "SimulatedBattery")
            {
                return new PowerStatusInfo
                {
                    IsAcConnected = false,
                    BatteryPercent = _settings.SimulatedBatteryPercent,
                    IsUnknown = false,
                    IsSimulated = true,
                    StatusSummary = $"[SIMULATED] On Battery ({_settings.SimulatedBatteryPercent}%)"
                };
            }

            // Real Windows power status
            var power = SystemInformation.PowerStatus;
            bool isOnline = power.PowerLineStatus == PowerLineStatus.Online;
            bool isOffline = power.PowerLineStatus == PowerLineStatus.Offline;
            bool isUnknown = power.PowerLineStatus == PowerLineStatus.Unknown;

            int? batteryPercent = null;
            if (power.BatteryLifePercent >= 0.0f && power.BatteryLifePercent <= 1.0f)
            {
                batteryPercent = (int)Math.Round(power.BatteryLifePercent * 100);
            }

            // Safe fallback: if power state is unknown, do NOT treat as AC connected!
            bool isAc = isOnline && !isUnknown;

            string summary;
            if (isUnknown)
            {
                summary = "Power: Unknown (Falling back to Battery mode)";
            }
            else if (isAc)
            {
                summary = batteryPercent.HasValue
                    ? $"Power: AC Connected ({batteryPercent.Value}%)"
                    : "Power: AC Connected";
            }
            else
            {
                summary = batteryPercent.HasValue
                    ? $"Power: On Battery ({batteryPercent.Value}%)"
                    : "Power: On Battery";
            }

            return new PowerStatusInfo
            {
                IsAcConnected = isAc,
                BatteryPercent = batteryPercent,
                IsUnknown = isUnknown,
                IsSimulated = false,
                StatusSummary = summary
            };
        }

        public void Dispose()
        {
            _pollingTimer.Stop();
            _pollingTimer.Dispose();
            _debounceTimer.Stop();
            _debounceTimer.Dispose();
        }
    }
}
