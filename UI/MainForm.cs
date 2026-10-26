using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SmartWallpaperApp.Core;
using SmartWallpaperApp.Models;
using SmartWallpaperApp.Services;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.UI
{
    public class MainForm : Form
    {
        private readonly SettingsManager _settingsManager;
        private readonly WallpaperManager _wallpaperManager;
        private readonly PowerMonitor _powerMonitor;
        private readonly FullscreenDetector _fullscreenDetector;

        private NotifyIcon _trayIcon = null!;
        private ContextMenuStrip _trayMenu = null!;
        private ToolStripMenuItem _menuStatusHeader = null!;
        private ToolStripMenuItem _menuPauseResume = null!;
        private ToolStripMenuItem _menuSimActual = null!;
        private ToolStripMenuItem _menuSimAc = null!;
        private ToolStripMenuItem _menuSimBattery = null!;

        private SettingsForm? _openSettingsForm;
        private readonly uint _taskbarCreatedMsg;

        public MainForm()
        {
            // Configure as hidden background window
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Size = new Size(0, 0);

            _taskbarCreatedMsg = WinApiHelpers.RegisterWindowMessage("TaskbarCreated");

            _settingsManager = new SettingsManager();
            _wallpaperManager = new WallpaperManager(_settingsManager);
            _powerMonitor = new PowerMonitor(_settingsManager.CurrentSettings);
            _fullscreenDetector = new FullscreenDetector();

            InitializeTray();

            _powerMonitor.PowerStateChanged += async (status) =>
            {
                await _wallpaperManager.HandlePowerChangeAsync(status);
                UpdateTrayStatus(status, _wallpaperManager.CurrentMode);
            };

            _wallpaperManager.StateChanged += (mode, msg) =>
            {
                UpdateTrayStatus(_powerMonitor.CurrentStatus, mode);
            };

            _fullscreenDetector.FullscreenStateChanged += (isFullscreen) =>
            {
                _wallpaperManager.HandleFullscreenState(isFullscreen);
            };

            Load += OnMainFormLoaded;
        }

        private async void OnMainFormLoaded(object? sender, EventArgs e)
        {
            Hide();
            Logger.Info("MainForm loaded. Initializing services and detecting power state...");

            _fullscreenDetector.Start();
            _powerMonitor.Start();

            // Perform initial mode transition
            await _wallpaperManager.HandlePowerChangeAsync(_powerMonitor.CurrentStatus, force: true);
        }

        private void InitializeTray()
        {
            _trayMenu = new ContextMenuStrip();

            _menuStatusHeader = new ToolStripMenuItem("Smart Wallpaper - Starting...") { Enabled = false, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _trayMenu.Items.Add(_menuStatusHeader);
            _trayMenu.Items.Add(new ToolStripSeparator());

            var menuSettings = new ToolStripMenuItem("⚙️ Settings...", null, (_, _) => ShowSettings());
            _trayMenu.Items.Add(menuSettings);

            var menuApply = new ToolStripMenuItem("🔄 Reapply Current Mode", null, async (_, _) => await _wallpaperManager.ForceApplyCurrentPowerModeAsync());
            _trayMenu.Items.Add(menuApply);

            _menuPauseResume = new ToolStripMenuItem("⏸️ Pause Automation", null, OnTogglePauseResume);
            _trayMenu.Items.Add(_menuPauseResume);

            // Simulation Submenu
            var menuSimulation = new ToolStripMenuItem("🧪 Simulation Override");
            _menuSimActual = new ToolStripMenuItem("Actual (Hardware Power)", null, (_, _) => SetSimulation("Actual"));
            _menuSimAc = new ToolStripMenuItem("Simulated AC Power", null, (_, _) => SetSimulation("SimulatedAC"));
            _menuSimBattery = new ToolStripMenuItem("Simulated Battery Power", null, (_, _) => SetSimulation("SimulatedBattery"));
            menuSimulation.DropDownItems.AddRange(new ToolStripItem[] { _menuSimActual, _menuSimAc, _menuSimBattery });
            _trayMenu.Items.Add(menuSimulation);

            _trayMenu.Items.Add(new ToolStripSeparator());

            var menuLogs = new ToolStripMenuItem("📁 Open Logs Folder", null, (_, _) =>
            {
                string logDir = Path.GetDirectoryName(Logger.CurrentLogFile) ?? "";
                if (Directory.Exists(logDir))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logDir,
                        UseShellExecute = true
                    });
                }
            });
            _trayMenu.Items.Add(menuLogs);

            var menuExit = new ToolStripMenuItem("❌ Exit", null, (_, _) => ExitApplication());
            _trayMenu.Items.Add(menuExit);

            _trayIcon = new NotifyIcon
            {
                Text = "Smart Wallpaper App",
                Icon = CreateTrayIcon(),
                ContextMenuStrip = _trayMenu,
                Visible = true
            };

            _trayIcon.DoubleClick += (_, _) => ShowSettings();

            UpdateSimulationMenuChecks(_settingsManager.CurrentSettings.SimulationMode);
        }

        private void UpdateTrayStatus(PowerStatusInfo? status, WallpaperMode mode)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => UpdateTrayStatus(status, mode));
                return;
            }

            string pwr = status?.StatusSummary ?? "Detecting...";
            string header = $"Mode: {mode} | {pwr}";
            _menuStatusHeader.Text = header;

            string tooltip = $"Smart Wallpaper [{mode}]\n{pwr}";
            if (tooltip.Length > 63) tooltip = tooltip.Substring(0, 60) + "...";
            _trayIcon.Text = tooltip;
        }

        private void OnTogglePauseResume(object? sender, EventArgs e)
        {
            bool newPaused = !_wallpaperManager.IsAutomationPaused;
            _wallpaperManager.SetAutomationPaused(newPaused);
            _menuPauseResume.Text = newPaused ? "▶️ Resume Automation" : "⏸️ Pause Automation";
        }

        private void SetSimulation(string mode)
        {
            _settingsManager.CurrentSettings.SimulationMode = mode;
            _settingsManager.Save(_settingsManager.CurrentSettings);
            _powerMonitor.UpdateSettings(_settingsManager.CurrentSettings);
            UpdateSimulationMenuChecks(mode);
        }

        private void UpdateSimulationMenuChecks(string currentMode)
        {
            _menuSimActual.Checked = currentMode == "Actual";
            _menuSimAc.Checked = currentMode == "SimulatedAC";
            _menuSimBattery.Checked = currentMode == "SimulatedBattery";
        }

        public void ShowSettings()
        {
            if (_openSettingsForm == null || _openSettingsForm.IsDisposed)
            {
                _openSettingsForm = new SettingsForm(_settingsManager, _wallpaperManager, _powerMonitor);
                _openSettingsForm.Show();
            }
            else
            {
                _openSettingsForm.WindowState = FormWindowState.Normal;
                _openSettingsForm.BringToFront();
                _openSettingsForm.Activate();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == PowerMonitor.WM_POWERBROADCAST)
            {
                _powerMonitor.HandlePowerBroadcast(m.WParam.ToInt32());
            }
            else if (m.Msg == _taskbarCreatedMsg && _taskbarCreatedMsg != 0)
            {
                _wallpaperManager.HandleExplorerRestart();
            }

            base.WndProc(ref m);
        }

        private static Icon CreateTrayIcon()
        {
            // Create a clean 16x16 / 32x32 vector-style icon with glowing gradient badge
            using var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Background circle
            using var brush = new SolidBrush(Color.FromArgb(15, 23, 42));
            g.FillEllipse(brush, 1, 1, 30, 30);

            // Glowing cyan border
            using var pen = new Pen(Color.FromArgb(0, 240, 255), 2.5f);
            g.DrawEllipse(pen, 2, 2, 28, 28);

            // Lightning / spark icon
            var points = new PointF[]
            {
                new(17, 6),
                new(10, 17),
                new(16, 17),
                new(14, 26),
                new(22, 14),
                new(17, 14)
            };
            using var fill = new SolidBrush(Color.FromArgb(0, 240, 255));
            g.FillPolygon(fill, points);

            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        private void ExitApplication()
        {
            Logger.Info("Exiting SmartWallpaperApp...");
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _powerMonitor.Dispose();
            _fullscreenDetector.Dispose();
            _wallpaperManager.Dispose();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _trayMenu?.Dispose();
                _fullscreenDetector?.Dispose();
                _powerMonitor?.Dispose();
                _wallpaperManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
