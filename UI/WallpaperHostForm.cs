using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using SmartWallpaperApp.Core;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.UI
{
    public class WallpaperHostForm : Form
    {
        public WebView2 WebView { get; }

        public WallpaperHostForm()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;

            // Target the entire virtual desktop or primary screen
            var bounds = SystemInformation.VirtualScreen;
            Bounds = bounds;

            WebView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.Black
            };

            Controls.Add(WebView);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WinApiHelpers.WS_EX_TOOLWINDOW;
                cp.ExStyle |= WinApiHelpers.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WinApiHelpers.WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)WinApiHelpers.MA_NOACTIVATE;
                return;
            }

            base.WndProc(ref m);
        }

        public bool AttachToDesktop()
        {
            var bounds = SystemInformation.VirtualScreen;
            Bounds = bounds;
            bool success = WinApiHelpers.EmbedWindowToDesktop(Handle, bounds);
            if (!success)
            {
                Logger.Warn("AttachToDesktop: Failed to embed host window into desktop layer.");
            }
            return success;
        }

        public void RefreshBounds()
        {
            var bounds = SystemInformation.VirtualScreen;
            Bounds = bounds;
            WinApiHelpers.SetWindowPos(
                Handle,
                IntPtr.Zero,
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                WinApiHelpers.SWP_NOZORDER | WinApiHelpers.SWP_NOACTIVATE | WinApiHelpers.SWP_SHOWWINDOW
            );
        }
    }
}
