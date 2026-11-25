using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using DynamicDesk.Core;
using DynamicDesk.Models;
using DynamicDesk.Services;
using DynamicDesk.Utils;

namespace DynamicDesk.UI
{
    public class SettingsForm : Form
    {
        private readonly SettingsManager _settingsManager;
        private readonly WallpaperManager _wallpaperManager;
        private readonly PowerMonitor _powerMonitor;

        // --- UI Elements: Header ---
        private Label _lblPowerBadge = null!;
        private Label _lblActiveModeSub = null!;
        private Button _btnQuickApply = null!;
        private Button _btnQuickPause = null!;
        private Panel _pnlSetupBanner = null!;

        // --- UI Elements: Live Wallpaper Card ---
        private Panel _pnlLiveMonitorContainer = null!;
        private Panel _pnlLiveScreen = null!;
        private Panel _pnlLivePlaceholder = null!;
        private Label _lblLivePlaceholderTitle = null!;
        private Label _lblLivePlaceholderSub = null!;
        private WebView2? _previewWebView;
        private CoreWebView2Environment? _previewEnvironment;
        private bool _isPreviewWebViewReady;
        private Panel _pnlLiveLed = null!;

        private RadioButton _rbLocalFile = null!;
        private RadioButton _rbWebUrl = null!;
        private TextBox _txtLivePath = null!;
        private Label _lblLiveFileBadge = null!;
        private Button _btnBrowseLive = null!;
        private Button _btnTestLive = null!;
        private TableLayoutPanel _pnlLiveUrlInput = null!;
        private TableLayoutPanel _pnlLiveFileInput = null!;

        // --- UI Elements: Static Wallpaper Card ---
        private Panel _pnlStaticMonitorContainer = null!;
        private Panel _pnlStaticScreen = null!;
        private Image? _staticImage;
        private Panel _pnlStaticLed = null!;
        private string? _loadedStaticPath;

        private TextBox _txtStaticPath = null!;
        private Label _lblStaticFileBadge = null!;
        private Button _btnBrowseStatic = null!;
        private ComboBox _cmbStaticStyle = null!;
        private Button _btnTestStatic = null!;

        // --- UI Elements: Preferences ---
        private CheckBox _chkAutoSwitch = null!;
        private CheckBox _chkPauseFullscreen = null!;
        private CheckBox _chkStartWithWindows = null!;
        private ComboBox _cmbLowBatteryThreshold = null!;

        // --- UI Elements: Advanced & Simulation ---
        private Panel _pnlAdvancedBody = null!;
        private Button _btnToggleAdvanced = null!;
        private bool _isAdvancedExpanded = false;
        private ComboBox _cmbSimulationMode = null!;
        private NumericUpDown _numSimulatedBattery = null!;
        private Label _lblSimulationBanner = null!;
        private NumericUpDown _numPollingInterval = null!;
        private Label _lblStatusPower = null!;
        private Label _lblStatusMode = null!;
        private Label _lblStatusWebView2 = null!;
        private Label _lblStatusLastMessage = null!;
        private Button _btnForceLive = null!;
        private Button _btnForceStatic = null!;

        // --- UI Elements: Bottom Action Bar ---
        private Label _lblBottomStatus = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        private readonly ToolTip _toolTip = new();
        private readonly System.Windows.Forms.Timer _statusRefreshTimer;

        public SettingsForm(SettingsManager settingsManager, WallpaperManager wallpaperManager, PowerMonitor powerMonitor)
        {
            _settingsManager = settingsManager;
            _wallpaperManager = wallpaperManager;
            _powerMonitor = powerMonitor;

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            InitializeForm();

            LoadSettingsToUI();
            RefreshRealtimeStatus();

            _statusRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _statusRefreshTimer.Tick += (_, _) => RefreshRealtimeStatus();
            _statusRefreshTimer.Start();

            FormClosing += OnFormClosingHandler;
            Shown += async (_, _) => await InitializePreviewWebViewAsync();
        }

        private void InitializeForm()
        {
            Text = "DynamicDesk - Wallpaper Hub";
            Size = new Size(960, 780);
            MinimumSize = new Size(880, 660);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.FromArgb(248, 250, 252);
            Icon = Icon;

            // Main Root Container
            var pnlRoot = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 250, 252) };

            // 1. Top Header Bar
            var pnlHeader = CreateHeaderPanel();
            pnlHeader.Dock = DockStyle.Top;

            // 2. Bottom Sticky Action Bar
            var pnlBottom = CreateBottomActionBar();
            pnlBottom.Dock = DockStyle.Bottom;

            // 3. Scrollable Content Area
            var pnlScrollContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 14, 24, 20),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var mainStack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Width = 890
            };
            mainStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Section: Setup Alert Banner
            _pnlSetupBanner = CreateSetupBanner();
            mainStack.Controls.Add(_pnlSetupBanner, 0, 0);

            // Section: Dual Wallpaper Showcase Cards (Live & Static)
            var pnlShowcase = CreateWallpaperShowcase();
            mainStack.Controls.Add(pnlShowcase, 0, 1);

            // Section: Preferences / Automation
            var pnlPreferences = CreatePreferencesCard();
            mainStack.Controls.Add(pnlPreferences, 0, 2);

            // Section: Advanced & Testing (Collapsible)
            var pnlAdvanced = CreateAdvancedSection();
            mainStack.Controls.Add(pnlAdvanced, 0, 3);

            pnlScrollContent.Controls.Add(mainStack);

            // Add all to root
            pnlRoot.Controls.Add(pnlScrollContent);
            pnlRoot.Controls.Add(pnlHeader);
            pnlRoot.Controls.Add(pnlBottom);

            Controls.Add(pnlRoot);
        }

        #region Header Panel
        private Panel CreateHeaderPanel()
        {
            var pnl = new Panel
            {
                Height = 74,
                BackColor = Color.White,
                Padding = new Padding(24, 12, 24, 12)
            };
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawLine(pen, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
            };

            // Left Brand Section
            var pnlBrand = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            var pnlLogoBadge = new Panel { Size = new Size(38, 38), Margin = new Padding(0, 2, 12, 0) };
            pnlLogoBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new LinearGradientBrush(new Rectangle(0, 0, 38, 38), Color.FromArgb(14, 165, 233), Color.FromArgb(99, 102, 241), 45f);
                e.Graphics.FillEllipse(brush, 1, 1, 36, 36);

                // Lightning spark in white
                var points = new PointF[]
                {
                    new(20, 8),
                    new(13, 20),
                    new(19, 20),
                    new(17, 30),
                    new(26, 17),
                    new(20, 17)
                };
                using var fill = new SolidBrush(Color.White);
                e.Graphics.FillPolygon(fill, points);
            };

            var pnlBrandTitles = new Panel { AutoSize = true, Height = 44 };
            var lblBrandName = new Label
            {
                Text = "DynamicDesk",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var lblBrandTagline = new Label
            {
                Text = "Smart power-aware wallpaper manager",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(1, 23)
            };
            pnlBrandTitles.Controls.AddRange(new Control[] { lblBrandName, lblBrandTagline });
            pnlBrand.Controls.AddRange(new Control[] { pnlLogoBadge, pnlBrandTitles });

            // Right Power Status Pill Section
            var pnlRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            var pnlStatusPill = new Panel
            {
                Height = 44,
                Width = 240,
                Margin = new Padding(0, 0, 10, 0),
                BackColor = Color.FromArgb(241, 245, 249)
            };
            pnlStatusPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pnlStatusPill.Width - 1, pnlStatusPill.Height - 1);
                using var pen = new Pen(Color.FromArgb(203, 213, 225), 1);
                e.Graphics.DrawRoundedRectangle(pen, rect, 8);
            };

            _lblPowerBadge = new Label
            {
                Text = "⚡ Detecting Power...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Location = new Point(10, 4)
            };
            _lblActiveModeSub = new Label
            {
                Text = "Checking mode...",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(11, 23)
            };
            pnlStatusPill.Controls.AddRange(new Control[] { _lblPowerBadge, _lblActiveModeSub });

            _btnQuickApply = new Button
            {
                Text = "🔄 Apply",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height = 42,
                Width = 84,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(15, 23, 42),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 6, 0)
            };
            _btnQuickApply.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnQuickApply.Click += async (_, _) =>
            {
                _lblBottomStatus.Text = "Applying current mode...";
                await _wallpaperManager.ForceApplyCurrentPowerModeAsync();
                _lblBottomStatus.Text = "Current wallpaper applied!";
            };

            _btnQuickPause = new Button
            {
                Text = "⏸️ Pause",
                Font = new Font("Segoe UI", 9f),
                Height = 42,
                Width = 84,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 0)
            };
            _btnQuickPause.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnQuickPause.Click += (_, _) =>
            {
                bool newPaused = !_wallpaperManager.IsAutomationPaused;
                _wallpaperManager.SetAutomationPaused(newPaused);
                _btnQuickPause.Text = newPaused ? "▶️ Resume" : "⏸️ Pause";
                RefreshRealtimeStatus();
            };

            pnlRight.Controls.AddRange(new Control[] { pnlStatusPill, _btnQuickApply, _btnQuickPause });

            pnl.Controls.Add(pnlBrand);
            pnl.Controls.Add(pnlRight);
            return pnl;
        }
        #endregion

        #region Setup Alert Banner
        private Panel CreateSetupBanner()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(238, 246, 255),
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0, 0, 0, 16),
                AutoSize = true,
                Visible = false
            };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(186, 215, 255), 1.5f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 8);
            };

            var lbl = new Label
            {
                Text = "👋 Welcome to DynamicDesk!\nChoose your Live Wallpaper (for AC power) and Static Wallpaper (for Battery) below to get started.\n📌 DynamicDesk runs in the background in your system tray near the clock.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(12, 74, 162),
                AutoSize = true,
                Dock = DockStyle.Fill
            };
            pnl.Controls.Add(lbl);
            return pnl;
        }
        #endregion

        #region Dual Wallpaper Showcase Cards
        private Control CreateWallpaperShowcase()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var cardLive = CreateLiveWallpaperCard();
            var cardStatic = CreateStaticWallpaperCard();

            layout.Controls.Add(cardLive, 0, 0);
            layout.Controls.Add(cardStatic, 1, 0);

            return layout;
        }

        private Panel CreateLiveWallpaperCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 8, 0),
                AutoSize = true,
                AllowDrop = true
            };
            card.Paint += (s, e) => DrawCardBorder(e.Graphics, card.ClientRectangle);
            card.DragEnter += (s, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                    e.Effect = DragDropEffects.Copy;
            };
            card.DragDrop += (s, e) =>
            {
                if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    string file = files[0];
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext is ".html" or ".htm" or ".mp4" or ".webm" or ".mov" or ".mkv")
                    {
                        _rbLocalFile.Checked = true;
                        _txtLivePath.Text = file;
                        _lblLiveFileBadge.Text = Path.GetFileName(file);
                        _toolTip.SetToolTip(_lblLiveFileBadge, file);
                        UpdateLivePreview();
                    }
                }
            };

            var stack = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Card Header
            var pnlHead = new Panel { Dock = DockStyle.Top, Height = 42 };
            var lblTitle = new Label
            {
                Text = "⚡ Live Wallpaper",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var lblSub = new Label
            {
                Text = "Active when plugged into AC power",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(2, 22)
            };
            var lblBadge = new Label
            {
                Text = "AC Mode",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                BackColor = Color.FromArgb(224, 242, 254),
                ForeColor = Color.FromArgb(3, 105, 161),
                Padding = new Padding(6, 3, 6, 3),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(310, 2)
            };
            pnlHead.Controls.AddRange(new Control[] { lblTitle, lblSub, lblBadge });
            stack.Controls.Add(pnlHead);

            // Monitor Preview Container
            _pnlLiveMonitorContainer = CreateMonitorFrame(out _pnlLiveScreen, out _pnlLiveLed, isLive: true);
            stack.Controls.Add(_pnlLiveMonitorContainer);

            // Inside the live screen: WebView2 and Placeholder
            _previewWebView = new WebView2
            {
                Dock = DockStyle.Fill,
                Visible = false,
                DefaultBackgroundColor = Color.FromArgb(15, 23, 42)
            };

            _pnlLivePlaceholder = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(12)
            };
            _lblLivePlaceholderTitle = new Label
            {
                Text = "⚡ No Live Wallpaper Selected",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(224, 231, 255),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _lblLivePlaceholderSub = new Label
            {
                Text = "Select an HTML file, video (.mp4/.webm), or web URL below.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlLivePlaceholder.Resize += (s, e) =>
            {
                _lblLivePlaceholderTitle.Location = new Point((_pnlLivePlaceholder.Width - _lblLivePlaceholderTitle.Width) / 2, _pnlLivePlaceholder.Height / 2 - 20);
                _lblLivePlaceholderSub.Location = new Point((_pnlLivePlaceholder.Width - _lblLivePlaceholderSub.Width) / 2, _pnlLivePlaceholder.Height / 2 + 6);
            };
            _pnlLivePlaceholder.Controls.AddRange(new Control[] { _lblLivePlaceholderTitle, _lblLivePlaceholderSub });

            _pnlLiveScreen.Controls.Add(_previewWebView);
            _pnlLiveScreen.Controls.Add(_pnlLivePlaceholder);

            // Source Selector (Local File vs Web Link)
            var pnlType = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 10, 0, 6) };
            _rbLocalFile = new RadioButton
            {
                Text = "Local File (HTML or Video)",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand
            };
            _rbWebUrl = new RadioButton
            {
                Text = "Remote Web Link (URL)",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize = true,
                Margin = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            _rbLocalFile.CheckedChanged += OnLiveSourceModeChanged;
            _rbWebUrl.CheckedChanged += OnLiveSourceModeChanged;
            pnlType.Controls.AddRange(new Control[] { _rbLocalFile, _rbWebUrl });
            stack.Controls.Add(pnlType);

            // Inputs: Local File View
            _pnlLiveFileInput = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            _pnlLiveFileInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _pnlLiveFileInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));

            _lblLiveFileBadge = new Label
            {
                Dock = DockStyle.Fill,
                Height = 34,
                Text = "No file chosen",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Padding = new Padding(8, 0, 0, 0),
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            _lblLiveFileBadge.Click += OnBrowseLiveClick;
            _lblLiveFileBadge.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(203, 213, 225), 1);
                e.Graphics.DrawRoundedRectangle(p, new Rectangle(0, 0, _lblLiveFileBadge.Width - 1, _lblLiveFileBadge.Height - 1), 6);
            };

            _btnBrowseLive = new Button
            {
                Text = "📁 Choose File...",
                Dock = DockStyle.Fill,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            _btnBrowseLive.FlatAppearance.BorderSize = 0;
            _btnBrowseLive.Click += OnBrowseLiveClick;

            _pnlLiveFileInput.Controls.Add(_lblLiveFileBadge, 0, 0);
            _pnlLiveFileInput.Controls.Add(_btnBrowseLive, 1, 0);
            stack.Controls.Add(_pnlLiveFileInput);

            // Inputs: Web URL View
            _pnlLiveUrlInput = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Visible = false,
                Margin = new Padding(0, 0, 0, 6)
            };
            _pnlLiveUrlInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _pnlLiveUrlInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));

            _txtLivePath = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                Height = 34,
                PlaceholderText = "https://example.com/interactive-wallpaper"
            };
            _txtLivePath.TextChanged += (_, _) =>
            {
                _lblLiveFileBadge.Text = string.IsNullOrWhiteSpace(_txtLivePath.Text) ? "No file chosen" : Path.GetFileName(_txtLivePath.Text);
                UpdateLivePreview();
            };

            var btnLoadUrl = new Button
            {
                Text = "Load Link",
                Dock = DockStyle.Fill,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnLoadUrl.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnLoadUrl.Click += (_, _) => UpdateLivePreview();

            _pnlLiveUrlInput.Controls.Add(_txtLivePath, 0, 0);
            _pnlLiveUrlInput.Controls.Add(btnLoadUrl, 1, 0);
            stack.Controls.Add(_pnlLiveUrlInput);

            // Bottom Buttons & Tip
            var pnlLiveActions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            _btnTestLive = new Button
            {
                Text = "👁️ Test in Browser / Fullscreen",
                AutoSize = true,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            _btnTestLive.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnTestLive.Click += OnTestLiveClick;
            pnlLiveActions.Controls.Add(_btnTestLive);
            stack.Controls.Add(pnlLiveActions);

            var lblLiveTip = new Label
            {
                Dock = DockStyle.Top,
                Text = "💡 Supports .html web files and .mp4 / .webm video loops.",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Padding = new Padding(0, 6, 0, 0),
                AutoSize = true
            };
            stack.Controls.Add(lblLiveTip);

            card.Controls.Add(stack);
            return card;
        }

        private Panel CreateStaticWallpaperCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(8, 0, 0, 0),
                AutoSize = true,
                AllowDrop = true
            };
            card.Paint += (s, e) => DrawCardBorder(e.Graphics, card.ClientRectangle);
            card.DragEnter += (s, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                    e.Effect = DragDropEffects.Copy;
            };
            card.DragDrop += (s, e) =>
            {
                if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    string file = files[0];
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext is ".jpg" or ".jpeg" or ".png" or ".bmp")
                    {
                        _txtStaticPath.Text = file;
                        _lblStaticFileBadge.Text = Path.GetFileName(file);
                        _toolTip.SetToolTip(_lblStaticFileBadge, file);
                        LoadStaticPreviewImage(file);
                    }
                }
            };

            var stack = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Card Header
            var pnlHead = new Panel { Dock = DockStyle.Top, Height = 42 };
            var lblTitle = new Label
            {
                Text = "🔋 Static Wallpaper",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var lblSub = new Label
            {
                Text = "Active when running on battery to save power",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(2, 22)
            };
            var lblBadge = new Label
            {
                Text = "Battery Mode",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                BackColor = Color.FromArgb(236, 253, 245),
                ForeColor = Color.FromArgb(4, 120, 87),
                Padding = new Padding(6, 3, 6, 3),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(290, 2)
            };
            pnlHead.Controls.AddRange(new Control[] { lblTitle, lblSub, lblBadge });
            stack.Controls.Add(pnlHead);

            // Monitor Preview Container
            _pnlStaticMonitorContainer = CreateMonitorFrame(out _pnlStaticScreen, out _pnlStaticLed, isLive: false);
            stack.Controls.Add(_pnlStaticMonitorContainer);

            // Inside static screen: Custom Paint Canvas
            _pnlStaticScreen.Paint += OnPaintStaticScreen;

            // Hidden TextBox to store exact path
            _txtStaticPath = new TextBox { Visible = false };
            stack.Controls.Add(_txtStaticPath);

            // File Row
            var pnlFileRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 6)
            };
            pnlFileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlFileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));

            _lblStaticFileBadge = new Label
            {
                Dock = DockStyle.Fill,
                Height = 34,
                Text = "No image chosen",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Padding = new Padding(8, 0, 0, 0),
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            _lblStaticFileBadge.Click += OnBrowseStaticClick;
            _lblStaticFileBadge.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(203, 213, 225), 1);
                e.Graphics.DrawRoundedRectangle(p, new Rectangle(0, 0, _lblStaticFileBadge.Width - 1, _lblStaticFileBadge.Height - 1), 6);
            };

            _btnBrowseStatic = new Button
            {
                Text = "🖼️ Choose Image...",
                Dock = DockStyle.Fill,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            _btnBrowseStatic.FlatAppearance.BorderSize = 0;
            _btnBrowseStatic.Click += OnBrowseStaticClick;

            pnlFileRow.Controls.Add(_lblStaticFileBadge, 0, 0);
            pnlFileRow.Controls.Add(_btnBrowseStatic, 1, 0);
            stack.Controls.Add(pnlFileRow);

            // Style Selector Row
            var pnlStyleRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            pnlStyleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95f));
            pnlStyleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlStyleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85f));

            var lblStyleTag = new Label
            {
                Text = "Display Style:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _cmbStaticStyle = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Height = 32
            };
            _cmbStaticStyle.Items.AddRange(new object[] { "Fill", "Fit", "Stretch", "Tile", "Center", "Span" });
            _cmbStaticStyle.SelectedItem = "Fill";
            _cmbStaticStyle.SelectedIndexChanged += (_, _) => _pnlStaticScreen.Invalidate();

            _btnTestStatic = new Button
            {
                Text = "👁️ View",
                Dock = DockStyle.Fill,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            _btnTestStatic.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnTestStatic.Click += OnTestStaticClick;

            pnlStyleRow.Controls.Add(lblStyleTag, 0, 0);
            pnlStyleRow.Controls.Add(_cmbStaticStyle, 1, 0);
            pnlStyleRow.Controls.Add(_btnTestStatic, 2, 0);
            stack.Controls.Add(pnlStyleRow);

            var lblStaticTip = new Label
            {
                Dock = DockStyle.Top,
                Text = "💡 Supports JPG, PNG, and BMP image files. 'Fill' fits best on standard monitors.",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Padding = new Padding(0, 6, 0, 0),
                AutoSize = true
            };
            stack.Controls.Add(lblStaticTip);

            card.Controls.Add(stack);
            return card;
        }

        private static Panel CreateMonitorFrame(out Panel screenPanel, out Panel ledIndicator, bool isLive)
        {
            var frame = new Panel
            {
                Dock = DockStyle.Top,
                Height = 224,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(8, 8, 8, 20),
                Margin = new Padding(0, 4, 0, 4)
            };
            frame.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, frame.Width - 1, frame.Height - 1);
                using var pen = new Pen(Color.FromArgb(15, 23, 42), 2f);
                e.Graphics.DrawRoundedRectangle(pen, rect, 10);
            };

            screenPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            // LED indicator in monitor bezel
            var led = new Panel
            {
                Size = new Size(8, 8),
                BackColor = Color.FromArgb(71, 85, 105),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(frame.Width - 24, frame.Height - 14)
            };
            led.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var b = new SolidBrush(led.BackColor);
                e.Graphics.FillEllipse(b, 0, 0, 7, 7);
            };

            frame.Resize += (s, e) =>
            {
                led.Location = new Point(frame.Width - 24, frame.Height - 14);
            };

            ledIndicator = led;
            frame.Controls.Add(screenPanel);
            frame.Controls.Add(led);
            return frame;
        }
        #endregion

        #region Preferences Card
        private Panel CreatePreferencesCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.White,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 0, 16),
                AutoSize = true
            };
            card.Paint += (s, e) => DrawCardBorder(e.Graphics, card.ClientRectangle);

            var stack = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var lblHead = new Label
            {
                Text = "⚙️ Smart Behavior & Preferences",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            stack.Controls.Add(lblHead);

            // 1. Auto Switch
            _chkAutoSwitch = new CheckBox
            {
                Text = "Automatically switch wallpapers based on AC / Battery power",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 4, 0, 2)
            };
            var lblAutoSub = new Label
            {
                Text = "When plugged in, switches to live wallpaper. When on battery, switches to static wallpaper.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(26, 0, 0, 8)
            };
            stack.Controls.Add(_chkAutoSwitch);
            stack.Controls.Add(lblAutoSub);

            // 2. Pause in Fullscreen
            _chkPauseFullscreen = new CheckBox
            {
                Text = "Pause live wallpaper during games and fullscreen applications",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 4, 0, 2)
            };
            var lblPauseSub = new Label
            {
                Text = "Frees GPU and CPU resources automatically whenever you play a game or watch a fullscreen video.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(26, 0, 0, 8)
            };
            stack.Controls.Add(_chkPauseFullscreen);
            stack.Controls.Add(lblPauseSub);

            // 3. Start With Windows
            _chkStartWithWindows = new CheckBox
            {
                Text = "Start DynamicDesk automatically with Windows",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 4, 0, 2)
            };
            var lblStartSub = new Label
            {
                Text = "Runs silently in the system tray when you log in, keeping your desktop wallpaper power-aware.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(26, 0, 0, 8)
            };
            stack.Controls.Add(_chkStartWithWindows);
            stack.Controls.Add(lblStartSub);

            // 4. Low Battery Threshold
            var pnlLowBatt = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(4, 4, 0, 4) };
            var lblLowBatt = new Label
            {
                Text = "Switch to static wallpaper early if battery drops below:",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Padding = new Padding(0, 4, 8, 0)
            };
            _cmbLowBatteryThreshold = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220,
                Font = new Font("Segoe UI", 9f)
            };
            _cmbLowBatteryThreshold.Items.AddRange(new object[]
            {
                "Disabled (Switch only on unplug)",
                "10% Battery",
                "15% Battery",
                "20% Battery (Recommended)",
                "25% Battery",
                "30% Battery",
                "50% Battery"
            });
            _cmbLowBatteryThreshold.SelectedIndex = 0;
            pnlLowBatt.Controls.AddRange(new Control[] { lblLowBatt, _cmbLowBatteryThreshold });
            stack.Controls.Add(pnlLowBatt);

            card.Controls.Add(stack);
            return card;
        }
        #endregion

        #region Advanced & Power Simulation (Collapsible)
        private Panel CreateAdvancedSection()
        {
            var pnlContainer = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.White,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 0, 16),
                AutoSize = true
            };
            pnlContainer.Paint += (s, e) => DrawCardBorder(e.Graphics, pnlContainer.ClientRectangle);

            var stack = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Header Toggle Button
            _btnToggleAdvanced = new Button
            {
                Text = "🛠️ Advanced Settings & Power Simulation  ▾",
                Dock = DockStyle.Top,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = Color.FromArgb(248, 250, 252),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Padding = new Padding(8, 0, 0, 0)
            };
            _btnToggleAdvanced.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            _btnToggleAdvanced.Click += (_, _) =>
            {
                _isAdvancedExpanded = !_isAdvancedExpanded;
                _pnlAdvancedBody.Visible = _isAdvancedExpanded;
                _btnToggleAdvanced.Text = _isAdvancedExpanded
                    ? "🛠️ Advanced Settings & Power Simulation  ▴"
                    : "🛠️ Advanced Settings & Power Simulation  ▾";
            };
            stack.Controls.Add(_btnToggleAdvanced);

            // Collapsible Body
            _pnlAdvancedBody = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Visible = false,
                Padding = new Padding(4, 12, 4, 4)
            };

            var bodyStack = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true };
            bodyStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Notice Banner
            _lblSimulationBanner = new Label
            {
                Text = "⚡ Simulation Active: Hardware power detection is currently overridden for testing.",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(161, 98, 7),
                BackColor = Color.FromArgb(254, 249, 195),
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10),
                Visible = false,
                AutoSize = true
            };
            bodyStack.Controls.Add(_lblSimulationBanner);

            // Simulation Controls
            var grpSim = new GroupBox
            {
                Text = "🧪 Power State Simulation (Test without unplugging your PC)",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 10)
            };
            var simFlow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            simFlow.Controls.Add(new Label { Text = "Mode:", AutoSize = true, Padding = new Padding(0, 4, 4, 0), Font = new Font("Segoe UI", 9f) });

            _cmbSimulationMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Font = new Font("Segoe UI", 9f) };
            _cmbSimulationMode.Items.AddRange(new object[] { "Actual (Hardware)", "Simulated AC Power", "Simulated Battery Power" });
            _cmbSimulationMode.SelectedIndex = 0;
            _cmbSimulationMode.SelectedIndexChanged += OnSimulationModeChanged;
            simFlow.Controls.Add(_cmbSimulationMode);

            simFlow.Controls.Add(new Label { Text = "   Battery Level %:", AutoSize = true, Padding = new Padding(0, 4, 4, 0), Font = new Font("Segoe UI", 9f) });
            _numSimulatedBattery = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 85, Width = 60, Font = new Font("Segoe UI", 9f) };
            _numSimulatedBattery.ValueChanged += (_, _) => OnSimulationModeChanged(null, EventArgs.Empty);
            simFlow.Controls.Add(_numSimulatedBattery);

            simFlow.Controls.Add(new Label { Text = "   Check Interval (s):", AutoSize = true, Padding = new Padding(0, 4, 4, 0), Font = new Font("Segoe UI", 9f) });
            _numPollingInterval = new NumericUpDown { Minimum = 1, Maximum = 300, Value = 10, Width = 60, Font = new Font("Segoe UI", 9f) };
            simFlow.Controls.Add(_numPollingInterval);

            grpSim.Controls.Add(simFlow);
            bodyStack.Controls.Add(grpSim);

            // Diagnostics Grid
            var grpDiag = new GroupBox
            {
                Text = "📊 Real-Time Engine & Power Diagnostics",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 10)
            };
            var diagTable = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            diagTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            diagTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _lblStatusPower = new Label { Text = "Detecting...", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _lblStatusMode = new Label { Text = "Unknown", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _lblStatusWebView2 = new Label { Text = "Checking...", AutoSize = true, Font = new Font("Segoe UI", 9f) };
            _lblStatusLastMessage = new Label { Text = "None", AutoSize = true, Font = new Font("Segoe UI", 9f) };

            AddStatusRow(diagTable, 0, "Hardware Power:", _lblStatusPower);
            AddStatusRow(diagTable, 1, "Active Wallpaper Mode:", _lblStatusMode);
            AddStatusRow(diagTable, 2, "WebView2 Engine:", _lblStatusWebView2);
            AddStatusRow(diagTable, 3, "Last Mode Transition:", _lblStatusLastMessage);

            // Quick Manual Override Buttons
            var pnlOverrideButtons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            _btnForceLive = new Button { Text = "Force Live Mode", AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            _btnForceLive.Click += async (_, _) => await _wallpaperManager.ForceLiveModeAsync();

            _btnForceStatic = new Button { Text = "Force Static Mode", AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            _btnForceStatic.Click += async (_, _) => await _wallpaperManager.ForceStaticModeAsync();

            var btnOpenLogs = new Button { Text = "📁 Open Logs Folder", AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            btnOpenLogs.Click += (_, _) =>
            {
                string logDir = Path.GetDirectoryName(Logger.CurrentLogFile) ?? "";
                if (Directory.Exists(logDir))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = logDir, UseShellExecute = true });
                }
            };

            pnlOverrideButtons.Controls.AddRange(new Control[] { _btnForceLive, _btnForceStatic, btnOpenLogs });
            diagTable.Controls.Add(pnlOverrideButtons, 0, 4);
            diagTable.SetColumnSpan(pnlOverrideButtons, 2);

            grpDiag.Controls.Add(diagTable);
            bodyStack.Controls.Add(grpDiag);

            _pnlAdvancedBody.Controls.Add(bodyStack);
            stack.Controls.Add(_pnlAdvancedBody);

            pnlContainer.Controls.Add(stack);
            return pnlContainer;
        }

        private static void AddStatusRow(TableLayoutPanel table, int row, string labelText, Label valueLabel)
        {
            var lblTitle = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize = true
            };
            table.Controls.Add(lblTitle, 0, row);
            table.Controls.Add(valueLabel, 1, row);
        }
        #endregion

        #region Bottom Action Bar
        private Panel CreateBottomActionBar()
        {
            var pnl = new Panel
            {
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(24, 12, 24, 12)
            };
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawLine(pen, 0, 0, pnl.Width, 0);
            };

            _lblBottomStatus = new Label
            {
                Dock = DockStyle.Left,
                Text = "🟢 Running in background • Accessible anytime from your system tray ^ ",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(51, 65, 85),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft
            };

            _btnSave = new Button
            {
                Text = "💾 Save & Apply Settings",
                Height = 36,
                Width = 180,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += OnSaveClick;

            _btnCancel = new Button
            {
                Text = "Close to Tray",
                Height = 36,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (_, _) => Close();

            pnlButtons.Controls.AddRange(new Control[] { _btnSave, _btnCancel });

            pnl.Controls.Add(_lblBottomStatus);
            pnl.Controls.Add(pnlButtons);
            return pnl;
        }
        #endregion

        #region Preview Loading & Rendering
        private async Task InitializePreviewWebViewAsync()
        {
            try
            {
                if (!WebWallpaperEngine.IsWebView2RuntimeAvailable(out _))
                {
                    _lblLivePlaceholderSub.Text = "WebView2 runtime not detected on system.";
                    return;
                }

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string previewUserData = Path.Combine(localAppData, "DynamicDesk", "PreviewUserData");
                Directory.CreateDirectory(previewUserData);

                _previewEnvironment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: previewUserData,
                    options: new CoreWebView2EnvironmentOptions()
                );

                if (_previewWebView != null && !_previewWebView.IsDisposed)
                {
                    await _previewWebView.EnsureCoreWebView2Async(_previewEnvironment);

                    var s = _previewWebView.CoreWebView2.Settings;
                    s.AreDefaultContextMenusEnabled = false;
                    s.AreDevToolsEnabled = false;
                    s.IsStatusBarEnabled = false;
                    s.IsZoomControlEnabled = false;
                    s.AreBrowserAcceleratorKeysEnabled = false;
                    s.IsBuiltInErrorPageEnabled = true;

                    // Silent preview (no audio from preview window!)
                    _previewWebView.CoreWebView2.IsMuted = true;

                    await _previewWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                        document.addEventListener('DOMContentLoaded', () => {
                            const style = document.createElement('style');
                            style.innerHTML = 'html, body { overflow: hidden !important; margin: 0 !important; padding: 0 !important; }';
                            document.head.appendChild(style);
                        });
                    ");

                    _isPreviewWebViewReady = true;
                    UpdateLivePreview();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to initialize preview WebView2: {ex.Message}");
                _lblLivePlaceholderSub.Text = "Preview engine error.";
            }
        }

        private void UpdateLivePreview()
        {
            if (!_isPreviewWebViewReady || _previewWebView?.CoreWebView2 == null)
            {
                return;
            }

            string source = _txtLivePath.Text.Trim();
            bool isUrl = _rbWebUrl.Checked;

            if (string.IsNullOrWhiteSpace(source))
            {
                _previewWebView.Visible = false;
                _pnlLivePlaceholder.Visible = true;
                return;
            }

            if (!isUrl && !File.Exists(source))
            {
                _previewWebView.Visible = false;
                _pnlLivePlaceholder.Visible = true;
                return;
            }

            try
            {
                _previewWebView.Visible = true;
                _pnlLivePlaceholder.Visible = false;

                if (isUrl)
                {
                    if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    {
                        _previewWebView.CoreWebView2.Navigate(source);
                    }
                }
                else
                {
                    string folder = Path.GetDirectoryName(source) ?? "";
                    string fileName = Path.GetFileName(source);
                    string ext = Path.GetExtension(source).ToLowerInvariant();

                    _previewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "wallpaper-preview",
                        folder,
                        CoreWebView2HostResourceAccessKind.Allow
                    );

                    if (ext is ".mp4" or ".webm" or ".mov" or ".mkv")
                    {
                        string mime = ext switch
                        {
                            ".webm" => "video/webm",
                            ".mov" => "video/quicktime",
                            ".mkv" => "video/x-matroska",
                            _ => "video/mp4"
                        };
                        string videoSrc = $"https://wallpaper-preview/{Uri.EscapeDataString(fileName)}";
                        string videoHtml = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<style>
  * {{ margin: 0; padding: 0; overflow: hidden; background: #0f172a; }}
  video {{ width: 100vw; height: 100vh; object-fit: cover; position: fixed; top: 0; left: 0; }}
</style>
</head>
<body>
  <video autoplay loop muted playsinline>
    <source src=""{videoSrc}"" type=""{mime}"">
  </video>
</body>
</html>";
                        _previewWebView.CoreWebView2.NavigateToString(videoHtml);
                    }
                    else
                    {
                        _previewWebView.CoreWebView2.Navigate($"https://wallpaper-preview/{Uri.EscapeDataString(fileName)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Exception updating live wallpaper preview: {ex.Message}");
            }
        }

        private void LoadStaticPreviewImage(string imagePath)
        {
            if (string.Equals(_loadedStaticPath, imagePath, StringComparison.OrdinalIgnoreCase) && _staticImage != null)
            {
                _pnlStaticScreen.Invalidate();
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
                {
                    byte[] bytes = File.ReadAllBytes(imagePath);
                    using var ms = new MemoryStream(bytes);
                    var loaded = Image.FromStream(ms);
                    var old = _staticImage;
                    _staticImage = new Bitmap(loaded);
                    old?.Dispose();
                    loaded.Dispose();
                    _loadedStaticPath = imagePath;
                }
                else
                {
                    _staticImage?.Dispose();
                    _staticImage = null;
                    _loadedStaticPath = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to load static preview image: {ex.Message}");
                _staticImage?.Dispose();
                _staticImage = null;
                _loadedStaticPath = null;
            }

            _pnlStaticScreen.Invalidate();
        }

        private void OnPaintStaticScreen(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var bounds = _pnlStaticScreen.ClientRectangle;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Fill monitor screen background
            using (var bgBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillRectangle(bgBrush, bounds);
            }

            if (_staticImage == null)
            {
                using var textBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
                using var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                using var fontSub = new Font("Segoe UI", 8.5f, FontStyle.Regular);

                string title = "🖼️ No Static Wallpaper Selected";
                string sub = "Click 'Choose Image' below to select a photo.";

                var szTitle = g.MeasureString(title, fontTitle);
                var szSub = g.MeasureString(sub, fontSub);

                g.DrawString(title, fontTitle, textBrush, (bounds.Width - szTitle.Width) / 2, bounds.Height / 2 - 20);
                g.DrawString(sub, fontSub, textBrush, (bounds.Width - szSub.Width) / 2, bounds.Height / 2 + 6);
                return;
            }

            string style = _cmbStaticStyle.SelectedItem?.ToString() ?? "Fill";
            DrawWallpaperWithStyle(g, _staticImage, bounds, style);

            // Realistic mini desktop taskbar at bottom
            int taskbarHeight = 14;
            var taskbarRect = new Rectangle(0, bounds.Height - taskbarHeight, bounds.Width, taskbarHeight);
            using (var tbBrush = new SolidBrush(Color.FromArgb(180, 15, 23, 42)))
            {
                g.FillRectangle(tbBrush, taskbarRect);
            }
            using (var tbLinePen = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
            {
                g.DrawLine(tbLinePen, 0, taskbarRect.Top, taskbarRect.Right, taskbarRect.Top);
            }

            // Simulated center icons
            int centerX = taskbarRect.Width / 2;
            int iconSize = 7;
            int tbY = taskbarRect.Y + (taskbarHeight - iconSize) / 2;
            Color[] iconColors = { Color.FromArgb(0, 164, 239), Color.FromArgb(255, 185, 0), Color.FromArgb(127, 186, 0), Color.FromArgb(242, 80, 34) };
            for (int i = 0; i < iconColors.Length; i++)
            {
                int x = centerX - (iconColors.Length * 11) / 2 + i * 11;
                using var b = new SolidBrush(iconColors[i]);
                g.FillRectangle(b, x, tbY, iconSize, iconSize);
            }

            // Resolution badge in top-right
            string resText = $"{_staticImage.Width} × {_staticImage.Height} • {style}";
            using var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            var szBadge = g.MeasureString(resText, badgeFont);
            var badgeRect = new Rectangle(bounds.Width - (int)szBadge.Width - 14, 6, (int)szBadge.Width + 8, (int)szBadge.Height + 4);
            using (var bBrush = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            {
                g.FillRoundedRectangle(bBrush, badgeRect, 4);
            }
            using (var tBrush = new SolidBrush(Color.White))
            {
                g.DrawString(resText, badgeFont, tBrush, badgeRect.X + 4, badgeRect.Y + 2);
            }
        }

        private static void DrawWallpaperWithStyle(Graphics g, Image img, Rectangle destRect, string style)
        {
            int imgW = img.Width;
            int imgH = img.Height;
            int destW = destRect.Width;
            int destH = destRect.Height;

            switch (style)
            {
                case "Fit":
                {
                    float scale = Math.Min((float)destW / imgW, (float)destH / imgH);
                    int drawW = (int)(imgW * scale);
                    int drawH = (int)(imgH * scale);
                    int drawX = destRect.X + (destW - drawW) / 2;
                    int drawY = destRect.Y + (destH - drawH) / 2;
                    g.DrawImage(img, drawX, drawY, drawW, drawH);
                    break;
                }
                case "Stretch":
                {
                    g.DrawImage(img, destRect);
                    break;
                }
                case "Center":
                {
                    int drawX = destRect.X + (destW - imgW) / 2;
                    int drawY = destRect.Y + (destH - imgH) / 2;
                    var clip = g.Clip;
                    g.SetClip(destRect);
                    g.DrawImage(img, drawX, drawY, imgW, imgH);
                    g.Clip = clip;
                    break;
                }
                case "Tile":
                {
                    var clip = g.Clip;
                    g.SetClip(destRect);
                    using var brush = new TextureBrush(img);
                    g.FillRectangle(brush, destRect);
                    g.Clip = clip;
                    break;
                }
                case "Span":
                case "Fill":
                default:
                {
                    float scale = Math.Max((float)destW / imgW, (float)destH / imgH);
                    int drawW = (int)(imgW * scale);
                    int drawH = (int)(imgH * scale);
                    int drawX = destRect.X + (destW - drawW) / 2;
                    int drawY = destRect.Y + (destH - drawH) / 2;

                    var clip = g.Clip;
                    g.SetClip(destRect);
                    g.DrawImage(img, drawX, drawY, drawW, drawH);
                    g.Clip = clip;
                    break;
                }
            }
        }
        #endregion

        #region User Event Handlers
        private void OnLiveSourceModeChanged(object? sender, EventArgs e)
        {
            bool isUrl = _rbWebUrl.Checked;
            _pnlLiveFileInput.Visible = !isUrl;
            _pnlLiveUrlInput.Visible = isUrl;
            UpdateLivePreview();
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
                Title = "Select Live Wallpaper (HTML or Video)",
                Filter = "All Supported Live Wallpapers (*.html;*.htm;*.mp4;*.webm;*.mov;*.mkv)|*.html;*.htm;*.mp4;*.webm;*.mov;*.mkv|HTML Web Wallpapers (*.html;*.htm)|*.html;*.htm|Video Files (*.mp4;*.webm;*.mov;*.mkv)|*.mp4;*.webm;*.mov;*.mkv|All Files (*.*)|*.*",
                InitialDirectory = initDir
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _txtLivePath.Text = dlg.FileName;
                _lblLiveFileBadge.Text = Path.GetFileName(dlg.FileName);
                _lblLiveFileBadge.ForeColor = Color.FromArgb(15, 23, 42);
                _toolTip.SetToolTip(_lblLiveFileBadge, dlg.FileName);
                UpdateLivePreview();
            }
        }

        private void OnBrowseStaticClick(object? sender, EventArgs e)
        {
            string initDir = !string.IsNullOrWhiteSpace(_txtStaticPath.Text) && Directory.Exists(Path.GetDirectoryName(_txtStaticPath.Text))
                ? Path.GetDirectoryName(_txtStaticPath.Text)!
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            using var dlg = new OpenFileDialog
            {
                Title = "Select Static Wallpaper (Image)",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All Files (*.*)|*.*",
                InitialDirectory = initDir
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _txtStaticPath.Text = dlg.FileName;
                _lblStaticFileBadge.Text = Path.GetFileName(dlg.FileName);
                _lblStaticFileBadge.ForeColor = Color.FromArgb(15, 23, 42);
                _toolTip.SetToolTip(_lblStaticFileBadge, dlg.FileName);
                LoadStaticPreviewImage(dlg.FileName);
            }
        }

        private void OnTestLiveClick(object? sender, EventArgs e)
        {
            string source = _txtLivePath.Text.Trim();
            if (string.IsNullOrEmpty(source))
            {
                MessageBox.Show(this, "Please select an HTML / video file or enter a web URL first.", "No Wallpaper Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var uri = _rbLocalFile.Checked ? new Uri(Path.GetFullPath(source)).AbsoluteUri : source;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = uri, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to launch preview: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnTestStaticClick(object? sender, EventArgs e)
        {
            string source = _txtStaticPath.Text.Trim();
            if (string.IsNullOrEmpty(source) || !File.Exists(source))
            {
                MessageBox.Show(this, "Please select a valid static wallpaper image first.", "No Image Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = source, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to open image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSimulationModeChanged(object? sender, EventArgs e)
        {
            string selectedText = _cmbSimulationMode.SelectedItem?.ToString() ?? "Actual (Hardware)";
            string simMode = selectedText.Contains("AC") ? "SimulatedAC" : selectedText.Contains("Battery") ? "SimulatedBattery" : "Actual";

            bool isSimulated = simMode != "Actual";
            _lblSimulationBanner.Visible = isSimulated;

            _settingsManager.CurrentSettings.SimulationMode = simMode;
            _settingsManager.CurrentSettings.SimulatedBatteryPercent = (int)_numSimulatedBattery.Value;
            _powerMonitor.UpdateSettings(_settingsManager.CurrentSettings);
        }

        private async void OnSaveClick(object? sender, EventArgs e)
        {
            string livePath = _txtLivePath.Text.Trim();
            string staticPath = _txtStaticPath.Text.Trim();

            if (_rbLocalFile.Checked && !string.IsNullOrWhiteSpace(livePath) && !File.Exists(livePath))
            {
                MessageBox.Show(this, $"The selected live wallpaper file does not exist:\n{livePath}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.IsNullOrWhiteSpace(staticPath) && !File.Exists(staticPath))
            {
                MessageBox.Show(this, $"The selected static wallpaper file does not exist:\n{staticPath}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int lowBattThresh = _cmbLowBatteryThreshold.SelectedIndex switch
            {
                1 => 10,
                2 => 15,
                3 => 20,
                4 => 25,
                5 => 30,
                6 => 50,
                _ => 0
            };

            string simText = _cmbSimulationMode.SelectedItem?.ToString() ?? "Actual (Hardware)";
            string simMode = simText.Contains("AC") ? "SimulatedAC" : simText.Contains("Battery") ? "SimulatedBattery" : "Actual";

            var updated = new AppSettings
            {
                LiveSourceType = _rbLocalFile.Checked ? "LocalFile" : "WebsiteUrl",
                LiveSourcePath = livePath,
                StaticImagePath = staticPath,
                StaticWallpaperStyle = _cmbStaticStyle.SelectedItem?.ToString() ?? "Fill",
                AutoSwitchEnabled = _chkAutoSwitch.Checked,
                StartWithWindows = _chkStartWithWindows.Checked,
                PauseOnFullscreen = _chkPauseFullscreen.Checked,
                PollingIntervalSeconds = (int)_numPollingInterval.Value,
                PauseBelowBatteryPercent = lowBattThresh,
                SimulationMode = simMode,
                SimulatedBatteryPercent = (int)_numSimulatedBattery.Value
            };

            bool saved = _settingsManager.Save(updated);
            if (saved)
            {
                _powerMonitor.UpdateSettings(_settingsManager.CurrentSettings);
                await _wallpaperManager.ForceApplyCurrentPowerModeAsync();
                _lblBottomStatus.Text = "Settings saved & applied successfully!";
                MessageBox.Show("Your wallpaper settings have been saved and applied.", "DynamicDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            else
            {
                MessageBox.Show("Failed to save settings. Check logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnFormClosingHandler(object? sender, FormClosingEventArgs e)
        {
            _statusRefreshTimer.Stop();
            try { _previewWebView?.Dispose(); } catch { }
            try { _staticImage?.Dispose(); } catch { }
        }
        #endregion

        #region Settings Loading & Real-time Status
        private void LoadSettingsToUI()
        {
            var s = _settingsManager.CurrentSettings;

            _pnlSetupBanner.Visible = !s.HasLiveWallpaper || !s.HasStaticWallpaper;

            if (s.LiveSourceType == "WebsiteUrl")
            {
                _rbWebUrl.Checked = true;
                _pnlLiveFileInput.Visible = false;
                _pnlLiveUrlInput.Visible = true;
            }
            else
            {
                _rbLocalFile.Checked = true;
                _pnlLiveFileInput.Visible = true;
                _pnlLiveUrlInput.Visible = false;
            }

            _txtLivePath.Text = s.LiveSourcePath;
            _lblLiveFileBadge.Text = string.IsNullOrWhiteSpace(s.LiveSourcePath) ? "No file chosen" : Path.GetFileName(s.LiveSourcePath);
            if (!string.IsNullOrWhiteSpace(s.LiveSourcePath)) _toolTip.SetToolTip(_lblLiveFileBadge, s.LiveSourcePath);

            _txtStaticPath.Text = s.StaticImagePath;
            _lblStaticFileBadge.Text = string.IsNullOrWhiteSpace(s.StaticImagePath) ? "No image chosen" : Path.GetFileName(s.StaticImagePath);
            if (!string.IsNullOrWhiteSpace(s.StaticImagePath)) _toolTip.SetToolTip(_lblStaticFileBadge, s.StaticImagePath);

            _toolTip.SetToolTip(_pnlLiveLed, "Live Mode Indicator (Cyan glow when active on desktop)");
            _toolTip.SetToolTip(_pnlStaticLed, "Static Mode Indicator (Green glow when active on desktop)");
            _cmbStaticStyle.SelectedItem = s.StaticWallpaperStyle;
            LoadStaticPreviewImage(s.StaticImagePath);

            _chkAutoSwitch.Checked = s.AutoSwitchEnabled;
            _chkStartWithWindows.Checked = s.StartWithWindows;
            _chkPauseFullscreen.Checked = s.PauseOnFullscreen;

            _cmbLowBatteryThreshold.SelectedIndex = s.PauseBelowBatteryPercent switch
            {
                10 => 1,
                15 => 2,
                20 => 3,
                25 => 4,
                30 => 5,
                50 => 6,
                _ => 0
            };

            _numPollingInterval.Value = Math.Clamp(s.PollingIntervalSeconds, 1, 300);

            _cmbSimulationMode.SelectedIndex = s.SimulationMode switch
            {
                "SimulatedAC" => 1,
                "SimulatedBattery" => 2,
                _ => 0
            };
            _numSimulatedBattery.Value = Math.Clamp(s.SimulatedBatteryPercent, 1, 100);
            _lblSimulationBanner.Visible = s.SimulationMode != "Actual";
        }

        private void RefreshRealtimeStatus()
        {
            var pStatus = _powerMonitor.CurrentStatus;
            bool isAc = pStatus != null && pStatus.IsAcConnected;

            // Power Header Badge
            if (pStatus != null)
            {
                string battStr = pStatus.BatteryPercent.HasValue ? $" ({pStatus.BatteryPercent.Value}%)" : "";
                _lblPowerBadge.Text = isAc ? $"⚡ Plugged In (AC){battStr}" : $"🔋 On Battery{battStr}";
                _lblPowerBadge.ForeColor = isAc ? Color.FromArgb(4, 120, 87) : Color.FromArgb(29, 78, 216);
            }
            else
            {
                _lblPowerBadge.Text = "⚡ Detecting Power...";
            }

            var currentMode = _wallpaperManager.CurrentMode;
            bool isPaused = _wallpaperManager.IsAutomationPaused;

            string modeText = currentMode switch
            {
                WallpaperMode.Live => "Live Wallpaper Active",
                WallpaperMode.Static => "Static Wallpaper Active",
                WallpaperMode.TransitioningToLive => "Switching to Live...",
                WallpaperMode.TransitioningToStatic => "Switching to Static...",
                WallpaperMode.Paused => "Wallpaper Paused",
                _ => "Standby"
            };

            if (isPaused) modeText += " (Paused)";
            _lblActiveModeSub.Text = $"Mode: {modeText}";

            // LED indicators on monitors
            bool liveActive = currentMode == WallpaperMode.Live && !isPaused;
            bool staticActive = currentMode == WallpaperMode.Static && !isPaused;

            _pnlLiveLed.BackColor = liveActive ? Color.FromArgb(0, 240, 255) : Color.FromArgb(71, 85, 105);
            _pnlStaticLed.BackColor = staticActive ? Color.FromArgb(16, 185, 129) : Color.FromArgb(71, 85, 105);
            _pnlLiveLed.Invalidate();
            _pnlStaticLed.Invalidate();

            // Diagnostics Details in Advanced tab
            _lblStatusPower.Text = pStatus != null ? pStatus.StatusSummary : "Detecting...";
            _lblStatusPower.ForeColor = isAc ? Color.FromArgb(4, 120, 87) : Color.FromArgb(180, 80, 0);

            _lblStatusMode.Text = $"{currentMode} {(isPaused ? "(Automation Paused)" : "")}";

            WebWallpaperEngine.IsWebView2RuntimeAvailable(out string wv2);
            _lblStatusWebView2.Text = wv2;

            _lblStatusLastMessage.Text = $"{_wallpaperManager.LastTransitionMessage} ({_wallpaperManager.LastTransitionTime:HH:mm:ss})";
        }
        #endregion

        #region Helpers
        private static void DrawCardBorder(Graphics g, Rectangle clientRect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, clientRect.Width - 1, clientRect.Height - 1);
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1.5f);
            g.DrawRoundedRectangle(pen, rect, 10);
        }
        #endregion
    }

    internal static class GraphicsExtensions
    {
        public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle bounds, int radius)
        {
            using var path = CreateRoundedPath(bounds, radius);
            g.DrawPath(pen, path);
        }

        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle bounds, int radius)
        {
            using var path = CreateRoundedPath(bounds, radius);
            g.FillPath(brush, path);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            // Top-left
            path.AddArc(arc, 180, 90);

            // Top-right
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom-right
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom-left
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
