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
    public class SettingsForm : Form
    {
        private readonly SettingsManager _settingsManager;
        private readonly WallpaperManager _wallpaperManager;
        private readonly PowerMonitor _powerMonitor;

        private Panel _pnlSetupBanner = null!;

        // UI Controls - Static Wallpaper
        private TextBox _txtStaticPath = null!;
        private Button _btnBrowseStatic = null!;
        private ComboBox _cmbStaticStyle = null!;

        // UI Controls - Live Wallpaper
        private RadioButton _rbLocalHtml = null!;
        private RadioButton _rbWebsiteUrl = null!;
        private TextBox _txtLivePath = null!;
        private Button _btnBrowseLive = null!;
        private Button _btnTestLive = null!;

        // UI Controls - Automation
        private CheckBox _chkAutoSwitch = null!;
        private CheckBox _chkStartWithWindows = null!;
        private CheckBox _chkPauseFullscreen = null!;
        private NumericUpDown _numPollingInterval = null!;
        private NumericUpDown _numLowBatteryThreshold = null!;

        // UI Controls - Simulation
        private ComboBox _cmbSimulationMode = null!;
        private NumericUpDown _numSimulatedBattery = null!;
        private Label _lblSimulationNotice = null!;

        // UI Controls - Real-time Status
        private Label _lblStatusPower = null!;
        private Label _lblStatusMode = null!;
        private Label _lblStatusSource = null!;
        private Label _lblStatusWebView2 = null!;
        private Label _lblStatusLastMessage = null!;

        // Action Buttons
        private Button _btnApplyNow = null!;
        private Button _btnForceLive = null!;
        private Button _btnForceStatic = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        private System.Windows.Forms.Timer _statusRefreshTimer = null!;

        public SettingsForm(SettingsManager settingsManager, WallpaperManager wallpaperManager, PowerMonitor powerMonitor)
        {
            _settingsManager = settingsManager;
            _wallpaperManager = wallpaperManager;
            _powerMonitor = powerMonitor;

            InitializeCustomComponents();
            LoadSettingsToUI();
            RefreshRealtimeStatus();

            _statusRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _statusRefreshTimer.Tick += (_, _) => RefreshRealtimeStatus();
            _statusRefreshTimer.Start();

            FormClosing += (_, _) => _statusRefreshTimer.Stop();
        }

        private void InitializeCustomComponents()
        {
            Text = "Smart Wallpaper - Settings & Control Panel";
            Size = new Size(760, 840);
            MinimumSize = new Size(700, 750);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.FromArgb(248, 249, 250);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(16),
                AutoScroll = true
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Setup Notice
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Live Wallpaper
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Static Wallpaper
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Automation & Simulation
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Live Diagnostic Status
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            // --- 0. Setup Banner ---
            _pnlSetupBanner = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(235, 245, 255),
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 12),
                AutoSize = true,
                Visible = false
            };
            var lblBanner = new Label
            {
                Text = "💡 First-Time Setup: There are no in-built default wallpapers. Please select your own custom Live and Static wallpaper files below to enable dynamic switching.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 90, 180),
                AutoSize = true,
                Dock = DockStyle.Fill
            };
            _pnlSetupBanner.Controls.Add(lblBanner);

            // --- 1. Live Wallpaper GroupBox ---
            var grpLive = new GroupBox
            {
                Text = "⚡ Live Wallpaper (Active on AC Power)",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            var liveLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
            liveLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            liveLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
            liveLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));

            var pnlRadio = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            _rbLocalHtml = new RadioButton { Text = "Local File (HTML or Video .mp4/.webm)", AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            _rbWebsiteUrl = new RadioButton { Text = "Remote Website URL (http/https)", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _rbLocalHtml.CheckedChanged += OnLiveSourceTypeChanged;
            _rbWebsiteUrl.CheckedChanged += OnLiveSourceTypeChanged;
            pnlRadio.Controls.AddRange(new Control[] { _rbLocalHtml, _rbWebsiteUrl });

            _txtLivePath = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            _btnBrowseLive = new Button { Text = "Browse...", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            _btnBrowseLive.Click += OnBrowseLiveClick;

            _btnTestLive = new Button { Text = "Test / Preview", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            _btnTestLive.Click += OnTestLiveClick;

            liveLayout.Controls.Add(pnlRadio, 0, 0);
            liveLayout.SetColumnSpan(pnlRadio, 3);
            liveLayout.Controls.Add(_txtLivePath, 0, 1);
            liveLayout.Controls.Add(_btnBrowseLive, 1, 1);
            liveLayout.Controls.Add(_btnTestLive, 2, 1);
            grpLive.Controls.Add(liveLayout);

            // --- 2. Static Wallpaper GroupBox ---
            var grpStatic = new GroupBox
            {
                Text = "🔋 Static Wallpaper (Active on Battery)",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            var staticLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
            staticLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            staticLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
            staticLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));

            _txtStaticPath = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            _btnBrowseStatic = new Button { Text = "Browse...", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            _btnBrowseStatic.Click += OnBrowseStaticClick;

            _cmbStaticStyle = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _cmbStaticStyle.Items.AddRange(new object[] { "Fill", "Fit", "Stretch", "Tile", "Center", "Span" });
            _cmbStaticStyle.SelectedItem = "Fill";

            staticLayout.Controls.Add(_txtStaticPath, 0, 0);
            staticLayout.Controls.Add(_btnBrowseStatic, 1, 0);
            staticLayout.Controls.Add(_cmbStaticStyle, 2, 0);
            grpStatic.Controls.Add(staticLayout);

            // --- 3. Automation & Simulation Settings ---
            var grpAuto = new GroupBox
            {
                Text = "⚙️ Automation & Power Simulation Options",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            var autoLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            autoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            autoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            _chkAutoSwitch = new CheckBox { Text = "Enable automatic power-based switching", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _chkStartWithWindows = new CheckBox { Text = "Start automatically with Windows", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _chkPauseFullscreen = new CheckBox { Text = "Pause live wallpaper during fullscreen apps", AutoSize = true, Font = new Font("Segoe UI", 9f) };

            var pnlPoll = new FlowLayoutPanel { AutoSize = true };
            pnlPoll.Controls.Add(new Label { Text = "Polling Interval (s):", AutoSize = true, Padding = new Padding(0, 4, 0, 0), Font = new Font("Segoe UI", 9f) });
            _numPollingInterval = new NumericUpDown { Minimum = 1, Maximum = 300, Value = 10, Width = 65, Font = new Font("Segoe UI", 9f) };
            pnlPoll.Controls.Add(_numPollingInterval);

            var pnlLowBatt = new FlowLayoutPanel { AutoSize = true };
            pnlLowBatt.Controls.Add(new Label { Text = "Pause if Battery < % (0=off):", AutoSize = true, Padding = new Padding(0, 4, 0, 0), Font = new Font("Segoe UI", 9f) });
            _numLowBatteryThreshold = new NumericUpDown { Minimum = 0, Maximum = 90, Value = 0, Width = 55, Font = new Font("Segoe UI", 9f) };
            pnlLowBatt.Controls.Add(_numLowBatteryThreshold);

            var pnlSim = new FlowLayoutPanel { AutoSize = true };
            pnlSim.Controls.Add(new Label { Text = "Power Simulation:", AutoSize = true, Padding = new Padding(0, 4, 0, 0), Font = new Font("Segoe UI", 9f) });
            _cmbSimulationMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Font = new Font("Segoe UI", 9f) };
            _cmbSimulationMode.Items.AddRange(new object[] { "Actual", "SimulatedAC", "SimulatedBattery" });
            _cmbSimulationMode.SelectedIndex = 0;
            _cmbSimulationMode.SelectedIndexChanged += OnSimulationModeChanged;
            pnlSim.Controls.Add(_cmbSimulationMode);

            var pnlSimBatt = new FlowLayoutPanel { AutoSize = true };
            pnlSimBatt.Controls.Add(new Label { Text = "Simulated Battery %:", AutoSize = true, Padding = new Padding(0, 4, 0, 0), Font = new Font("Segoe UI", 9f) });
            _numSimulatedBattery = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 80, Width = 55, Font = new Font("Segoe UI", 9f) };
            _numSimulatedBattery.ValueChanged += (_, _) => OnSimulationModeChanged(null, EventArgs.Empty);
            pnlSimBatt.Controls.Add(_numSimulatedBattery);

            _lblSimulationNotice = new Label
            {
                Text = "⚡ Simulation Active: Hardware power status is currently overridden for testing.",
                ForeColor = Color.DarkGoldenrod,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                AutoSize = true,
                Visible = false
            };

            autoLayout.Controls.Add(_chkAutoSwitch, 0, 0);
            autoLayout.Controls.Add(_chkStartWithWindows, 1, 0);
            autoLayout.Controls.Add(_chkPauseFullscreen, 0, 1);
            autoLayout.Controls.Add(pnlPoll, 1, 1);
            autoLayout.Controls.Add(pnlLowBatt, 0, 2);
            autoLayout.Controls.Add(pnlSim, 1, 2);
            autoLayout.Controls.Add(_lblSimulationNotice, 0, 3);
            autoLayout.SetColumnSpan(_lblSimulationNotice, 2);
            autoLayout.Controls.Add(pnlSimBatt, 1, 4);

            grpAuto.Controls.Add(autoLayout);

            // --- 4. Live Diagnostic Status Panel ---
            var grpStatus = new GroupBox
            {
                Text = "📊 Real-Time Diagnostic & Power Status",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            var statusLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _lblStatusPower = new Label { Text = "Detecting...", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _lblStatusMode = new Label { Text = "Unknown", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _lblStatusSource = new Label { Text = "None", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _lblStatusWebView2 = new Label { Text = "Checking...", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _lblStatusLastMessage = new Label { Text = "None", AutoSize = true, Font = new Font("Segoe UI", 9f) };

            AddStatusRow(statusLayout, 0, "Power Detection:", _lblStatusPower);
            AddStatusRow(statusLayout, 1, "Wallpaper Mode:", _lblStatusMode);
            AddStatusRow(statusLayout, 2, "Active Source:", _lblStatusSource);
            AddStatusRow(statusLayout, 3, "WebView2 Engine:", _lblStatusWebView2);
            AddStatusRow(statusLayout, 4, "Last Transition Result:", _lblStatusLastMessage);

            var pnlTestControls = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            _btnApplyNow = new Button { Text = "Apply Appropriate Mode", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _btnApplyNow.Click += async (_, _) => await _wallpaperManager.ForceApplyCurrentPowerModeAsync();

            _btnForceLive = new Button { Text = "Force Live Mode", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _btnForceLive.Click += async (_, _) => await _wallpaperManager.ForceLiveModeAsync();

            _btnForceStatic = new Button { Text = "Force Static Mode", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _btnForceStatic.Click += async (_, _) => await _wallpaperManager.ForceStaticModeAsync();

            pnlTestControls.Controls.AddRange(new Control[] { _btnApplyNow, _btnForceLive, _btnForceStatic });

            statusLayout.Controls.Add(pnlTestControls, 0, 5);
            statusLayout.SetColumnSpan(pnlTestControls, 2);

            grpStatus.Controls.Add(statusLayout);

            // --- 5. Bottom Save / Cancel Buttons ---
            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 12, 0, 0)
            };

            _btnCancel = new Button { Text = "Close", Width = 95, Height = 34, Font = new Font("Segoe UI", 9f) };
            _btnCancel.Click += (_, _) => Close();

            _btnSave = new Button { Text = "Save Settings", Width = 120, Height = 34, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            _btnSave.Click += OnSaveClick;

            pnlBottom.Controls.AddRange(new Control[] { _btnCancel, _btnSave });

            mainLayout.Controls.Add(_pnlSetupBanner, 0, 0);
            mainLayout.Controls.Add(grpLive, 0, 1);
            mainLayout.Controls.Add(grpStatic, 0, 2);
            mainLayout.Controls.Add(grpAuto, 0, 3);
            mainLayout.Controls.Add(grpStatus, 0, 4);
            mainLayout.Controls.Add(pnlBottom, 0, 5);

            Controls.Add(mainLayout);
        }

        private static void AddStatusRow(TableLayoutPanel layout, int row, string title, Label valueLabel)
        {
            var titleLabel = new Label
            {
                Text = title,
                AutoSize = true,
                ForeColor = Color.FromArgb(70, 70, 70),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            layout.Controls.Add(titleLabel, 0, row);
            layout.Controls.Add(valueLabel, 1, row);
        }

        private void LoadSettingsToUI()
        {
            var s = _settingsManager.CurrentSettings;

            _pnlSetupBanner.Visible = !s.HasLiveWallpaper || !s.HasStaticWallpaper;

            if (s.LiveSourceType == "WebsiteUrl")
            {
                _rbWebsiteUrl.Checked = true;
            }
            else
            {
                _rbLocalHtml.Checked = true;
            }

            _txtLivePath.Text = s.LiveSourcePath;
            _txtStaticPath.Text = s.StaticImagePath;
            _cmbStaticStyle.SelectedItem = s.StaticWallpaperStyle;

            _chkAutoSwitch.Checked = s.AutoSwitchEnabled;
            _chkStartWithWindows.Checked = s.StartWithWindows;
            _chkPauseFullscreen.Checked = s.PauseOnFullscreen;
            _numPollingInterval.Value = Math.Clamp(s.PollingIntervalSeconds, 1, 300);
            _numLowBatteryThreshold.Value = Math.Clamp(s.PauseBelowBatteryPercent, 0, 90);

            _cmbSimulationMode.SelectedItem = s.SimulationMode;
            _numSimulatedBattery.Value = Math.Clamp(s.SimulatedBatteryPercent, 1, 100);

            UpdateLiveSourceUiState();
        }

        private void OnLiveSourceTypeChanged(object? sender, EventArgs e)
        {
            UpdateLiveSourceUiState();
        }

        private void UpdateLiveSourceUiState()
        {
            _btnBrowseLive.Enabled = _rbLocalHtml.Checked;
        }

        private void OnBrowseLiveClick(object? sender, EventArgs e)
        {
            string initDir = !string.IsNullOrWhiteSpace(_txtLivePath.Text) && Directory.Exists(Path.GetDirectoryName(_txtLivePath.Text))
                ? Path.GetDirectoryName(_txtLivePath.Text)!
                : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

            if (!Directory.Exists(initDir))
            {
                initDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            using var dlg = new OpenFileDialog
            {
                Title = "Select Custom Live Wallpaper (HTML or Video)",
                Filter = "All Supported Live Wallpapers (*.html;*.htm;*.mp4;*.webm)|*.html;*.htm;*.mp4;*.webm|HTML Files (*.html;*.htm)|*.html;*.htm|Video Files (*.mp4;*.webm)|*.mp4;*.webm|All Files (*.*)|*.*",
                InitialDirectory = initDir
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _txtLivePath.Text = dlg.FileName;
            }
        }

        private void OnBrowseStaticClick(object? sender, EventArgs e)
        {
            string initDir = !string.IsNullOrWhiteSpace(_txtStaticPath.Text) && Directory.Exists(Path.GetDirectoryName(_txtStaticPath.Text))
                ? Path.GetDirectoryName(_txtStaticPath.Text)!
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            using var dlg = new OpenFileDialog
            {
                Title = "Select Custom Static Wallpaper (Image)",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All Files (*.*)|*.*",
                InitialDirectory = initDir
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _txtStaticPath.Text = dlg.FileName;
            }
        }

        private void OnTestLiveClick(object? sender, EventArgs e)
        {
            string source = _txtLivePath.Text.Trim();
            if (string.IsNullOrEmpty(source))
            {
                MessageBox.Show(this, "Please enter a valid website URL or select a local HTML / Video file.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_rbLocalHtml.Checked && !File.Exists(source))
            {
                MessageBox.Show(this, $"File not found: {source}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_rbWebsiteUrl.Checked && !Uri.TryCreate(source, UriKind.Absolute, out _))
            {
                MessageBox.Show(this, $"Invalid URL format: {source}", "URL Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Launch a test preview window using system default browser or player
            try
            {
                var uri = _rbLocalHtml.Checked ? new Uri(Path.GetFullPath(source)).AbsoluteUri : source;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to launch preview: {ex.Message}", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSimulationModeChanged(object? sender, EventArgs e)
        {
            string simMode = _cmbSimulationMode.SelectedItem?.ToString() ?? "Actual";
            bool isSimulated = simMode != "Actual";
            _lblSimulationNotice.Visible = isSimulated;

            // Apply simulation mode immediately to PowerMonitor so user can test transitions on the fly
            _settingsManager.CurrentSettings.SimulationMode = simMode;
            _settingsManager.CurrentSettings.SimulatedBatteryPercent = (int)_numSimulatedBattery.Value;
            _powerMonitor.UpdateSettings(_settingsManager.CurrentSettings);
        }

        private async void OnSaveClick(object? sender, EventArgs e)
        {
            string livePath = _txtLivePath.Text.Trim();
            string staticPath = _txtStaticPath.Text.Trim();

            // Validate live file if specified
            if (_rbLocalHtml.Checked && !string.IsNullOrWhiteSpace(livePath) && !File.Exists(livePath))
            {
                MessageBox.Show(this, $"The selected live wallpaper file does not exist:\n{livePath}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validate static file if specified
            if (!string.IsNullOrWhiteSpace(staticPath) && !File.Exists(staticPath))
            {
                MessageBox.Show(this, $"The selected static wallpaper file does not exist:\n{staticPath}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var updated = new AppSettings
            {
                LiveSourceType = _rbLocalHtml.Checked ? "LocalFile" : "WebsiteUrl",
                LiveSourcePath = livePath,
                StaticImagePath = staticPath,
                StaticWallpaperStyle = _cmbStaticStyle.SelectedItem?.ToString() ?? "Fill",
                AutoSwitchEnabled = _chkAutoSwitch.Checked,
                StartWithWindows = _chkStartWithWindows.Checked,
                PauseOnFullscreen = _chkPauseFullscreen.Checked,
                PollingIntervalSeconds = (int)_numPollingInterval.Value,
                PauseBelowBatteryPercent = (int)_numLowBatteryThreshold.Value,
                SimulationMode = _cmbSimulationMode.SelectedItem?.ToString() ?? "Actual",
                SimulatedBatteryPercent = (int)_numSimulatedBattery.Value
            };

            bool saved = _settingsManager.Save(updated);
            if (saved)
            {
                _powerMonitor.UpdateSettings(_settingsManager.CurrentSettings);
                await _wallpaperManager.ForceApplyCurrentPowerModeAsync();
                MessageBox.Show(this, "Your wallpaper settings have been saved and applied.", "Smart Wallpaper", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            else
            {
                MessageBox.Show(this, "Failed to save settings. Check logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshRealtimeStatus()
        {
            var pStatus = _powerMonitor.CurrentStatus;
            _lblStatusPower.Text = pStatus != null ? pStatus.StatusSummary : "Detecting...";
            _lblStatusPower.ForeColor = (pStatus != null && pStatus.IsAcConnected) ? Color.DarkGreen : Color.FromArgb(180, 80, 0);

            _lblStatusMode.Text = $"{_wallpaperManager.CurrentMode} {(_wallpaperManager.IsAutomationPaused ? "(Automation Paused)" : "")}";

            string src = _settingsManager.CurrentSettings.LiveSourcePath;
            _lblStatusSource.Text = string.IsNullOrWhiteSpace(src) ? "(No live wallpaper selected)" : Path.GetFileName(src);

            WebWallpaperEngine.IsWebView2RuntimeAvailable(out string wv2Status);
            _lblStatusWebView2.Text = wv2Status;

            _lblStatusLastMessage.Text = $"{_wallpaperManager.LastTransitionMessage} ({_wallpaperManager.LastTransitionTime:HH:mm:ss})";
        }
    }
}
