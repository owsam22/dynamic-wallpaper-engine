using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.Core
{
    /// <summary>
    /// Forwards mouse hover, click, and scroll events from the Windows desktop
    /// to the embedded WebView2 wallpaper, enabling full interactivity on the homescreen.
    /// </summary>
    public class DesktopInputForwarder : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MOUSEWHEEL = 0x020A;

        private IntPtr _hookId = IntPtr.Zero;
        private WinApiHelpers.HookProc? _hookProc;
        private IntPtr _hostFormHwnd = IntPtr.Zero;
        private IntPtr _targetRenderHwnd = IntPtr.Zero;
        private Form? _hostForm;
        private bool _isDisposed;

        public void Start(Form hostForm)
        {
            _hostForm = hostForm;
            _hostFormHwnd = hostForm.Handle;
            _targetRenderHwnd = WinApiHelpers.GetInnermostWindow(_hostFormHwnd);

            if (_hookId == IntPtr.Zero)
            {
                _hookProc = HookCallback;
                IntPtr hMod = WinApiHelpers.GetModuleHandle(null);
                _hookId = WinApiHelpers.SetWindowsHookEx(WH_MOUSE_LL, _hookProc, hMod, 0);

                if (_hookId == IntPtr.Zero)
                {
                    Logger.Warn("Failed to install low-level mouse hook for desktop interaction.");
                }
                else
                {
                    Logger.Info("Desktop interaction hook successfully installed.");
                }
            }
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                WinApiHelpers.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                _hookProc = null;
                Logger.Info("Desktop interaction hook uninstalled.");
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _hostForm != null && !_hostForm.IsDisposed && _hostForm.Visible)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_MOUSEMOVE || msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP ||
                    msg == WM_RBUTTONDOWN || msg == WM_RBUTTONUP || msg == WM_MOUSEWHEEL)
                {
                    var hookStruct = Marshal.PtrToStructure<WinApiHelpers.MSLLHOOKSTRUCT>(lParam);

                    // Check if mouse is hovering over any desktop shell window
                    IntPtr hwndUnder = WinApiHelpers.WindowFromPoint(hookStruct.pt);
                    if (IsDesktopLayer(hwndUnder))
                    {
                        var pt = new WinApiHelpers.POINT { X = hookStruct.pt.X, Y = hookStruct.pt.Y };
                        WinApiHelpers.ScreenToClient(_hostFormHwnd, ref pt);

                        IntPtr msgLParam = (IntPtr)((pt.Y << 16) | (pt.X & 0xFFFF));
                        IntPtr msgWParam = (IntPtr)(msg == WM_MOUSEWHEEL ? hookStruct.mouseData : 0x0001);

                        if (_targetRenderHwnd == IntPtr.Zero || !WinApiHelpers.IsWindow(_targetRenderHwnd))
                        {
                            _targetRenderHwnd = WinApiHelpers.GetInnermostWindow(_hostFormHwnd);
                        }

                        // Post directly to the Chromium rendering surface
                        WinApiHelpers.PostMessage(_targetRenderHwnd, (uint)msg, msgWParam, msgLParam);
                    }
                }
            }

            return WinApiHelpers.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static bool IsDesktopLayer(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;

            var sb = new StringBuilder(64);
            WinApiHelpers.GetClassName(hwnd, sb, sb.Capacity);
            string cls = sb.ToString();

            return cls == "Progman" || cls == "WorkerW" || cls == "SHELLDLL_DefView" || cls == "SysListView32";
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _isDisposed = true;
            }
        }
    }
}
