using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DynamicDesk.UI;
using DynamicDesk.Utils;

namespace DynamicDesk.Core
{
    /// <summary>
    /// Forwards mouse hover, click, and scroll events from the Windows desktop
    /// to the embedded WebView2 wallpaper, enabling full interactivity on the homescreen.
    ///
    /// HOW IT WORKS:
    /// 1. Installs a low-level mouse hook (WH_MOUSE_LL) to capture cursor events across the OS.
    /// 2. When the cursor is over the desktop canvas (SHELLDLL_DefView, SysListView32, WorkerW,
    ///    Progman) or our embedded wallpaper window, the event is converted to the client
    ///    coordinates of the Chromium rendering surface (Chrome_RenderWidgetHostHWND).
    /// 3. Injects the corresponding Win32 messages (WM_SETCURSOR, WM_MOUSEACTIVATE, WM_MOUSEMOVE,
    ///    WM_LBUTTONDOWN, WM_LBUTTONUP, WM_MOUSEWHEEL) directly into Chrome_RenderWidgetHostHWND.
    ///    This feeds Chromium's input pipeline and fires DOM pointer/mouse events (hover, click)
    ///    inside the web page (including HTML5 canvas, PixiJS, buttons, etc.).
    /// </summary>
    public class DesktopInputForwarder : IDisposable
    {
        private const int WH_MOUSE_LL = 14;

        private const int WM_SETFOCUS       = 0x0007;
        private const int WM_SETCURSOR      = 0x0020;
        private const int WM_MOUSEACTIVATE  = 0x0021;
        private const int WM_MOUSEMOVE      = 0x0200;
        private const int WM_LBUTTONDOWN    = 0x0201;
        private const int WM_LBUTTONUP      = 0x0202;
        private const int WM_RBUTTONDOWN    = 0x0204;
        private const int WM_RBUTTONUP      = 0x0205;
        private const int WM_MBUTTONDOWN    = 0x0207;
        private const int WM_MBUTTONUP      = 0x0208;
        private const int WM_MOUSEWHEEL     = 0x020A;

        private const int HTCLIENT = 1;

        private IntPtr _hookId = IntPtr.Zero;
        private WinApiHelpers.HookProc? _hookProc;
        private IntPtr _hostFormHwnd = IntPtr.Zero;
        private IntPtr _targetRenderHwnd = IntPtr.Zero;
        private bool _isRenderWidgetConfirmed;
        private WallpaperHostForm? _hostForm;
        private bool _isDisposed;

        private bool _isLeftDown;
        private bool _isRightDown;
        private bool _isMiddleDown;

        public void Start(WallpaperHostForm hostForm)
        {
            _hostForm = hostForm;
            _hostFormHwnd = hostForm.Handle;
            _targetRenderHwnd = IntPtr.Zero;
            _isRenderWidgetConfirmed = false;

            if (_hookId == IntPtr.Zero)
            {
                _hookProc = HookCallback;
                IntPtr hMod = WinApiHelpers.GetModuleHandle(null);
                _hookId = WinApiHelpers.SetWindowsHookEx(WH_MOUSE_LL, _hookProc, hMod, 0);

                if (_hookId == IntPtr.Zero)
                {
                    Logger.Warn("DesktopInputForwarder: Failed to install low-level mouse hook.");
                }
                else
                {
                    Logger.Info("DesktopInputForwarder: Desktop interaction hook successfully installed.");
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
                _targetRenderHwnd = IntPtr.Zero;
                _isRenderWidgetConfirmed = false;
                Logger.Info("DesktopInputForwarder: Desktop interaction hook uninstalled.");
            }
            _hostForm = null;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _hostForm != null && !_hostForm.IsDisposed && _hostForm.Visible)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_MOUSEMOVE || msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP ||
                    msg == WM_RBUTTONDOWN || msg == WM_RBUTTONUP || msg == WM_MBUTTONDOWN ||
                    msg == WM_MBUTTONUP || msg == WM_MOUSEWHEEL)
                {
                    var hookStruct = Marshal.PtrToStructure<WinApiHelpers.MSLLHOOKSTRUCT>(lParam);

                    // Track physical button states
                    switch (msg)
                    {
                        case WM_LBUTTONDOWN: _isLeftDown = true; break;
                        case WM_LBUTTONUP:   _isLeftDown = false; break;
                        case WM_RBUTTONDOWN: _isRightDown = true; break;
                        case WM_RBUTTONUP:   _isRightDown = false; break;
                        case WM_MBUTTONDOWN: _isMiddleDown = true; break;
                        case WM_MBUTTONUP:   _isMiddleDown = false; break;
                    }

                    // Only forward when cursor is over the desktop canvas or our own wallpaper window
                    IntPtr hwndUnder = WinApiHelpers.WindowFromPoint(hookStruct.pt);
                    if (IsDesktopLayer(hwndUnder) || IsOurWallpaperWindow(hwndUnder))
                    {
                        IntPtr renderHwnd = GetOrFindRenderWidgetHwnd();
                        if (renderHwnd != IntPtr.Zero)
                        {
                            // Convert screen coordinates to client coordinates of the Chromium render widget
                            var pt = new WinApiHelpers.POINT { X = hookStruct.pt.X, Y = hookStruct.pt.Y };
                            WinApiHelpers.ScreenToClient(renderHwnd, ref pt);
                            IntPtr clientLParam = (IntPtr)((pt.Y << 16) | (pt.X & 0xFFFF));

                            ForwardMessage(renderHwnd, msg, hookStruct, clientLParam);
                        }
                    }
                }
            }

            return WinApiHelpers.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void ForwardMessage(IntPtr renderHwnd, int msg, WinApiHelpers.MSLLHOOKSTRUCT hookStruct, IntPtr clientLParam)
        {
            switch (msg)
            {
                case WM_MOUSEMOVE:
                {
                    uint keys = 0;
                    if (_isLeftDown)   keys |= 0x0001; // MK_LBUTTON
                    if (_isRightDown)  keys |= 0x0002; // MK_RBUTTON
                    if (_isMiddleDown) keys |= 0x0010; // MK_MBUTTON

                    // Tell Chromium cursor entered its client area so it can activate hover state
                    WinApiHelpers.SendMessage(renderHwnd, WM_SETCURSOR, renderHwnd, (IntPtr)((WM_MOUSEMOVE << 16) | HTCLIENT));
                    WinApiHelpers.PostMessage(renderHwnd, (uint)msg, (IntPtr)keys, clientLParam);
                    break;
                }

                case WM_LBUTTONDOWN:
                {
                    // Activate and focus Chromium rendering surface so click is processed
                    WinApiHelpers.SendMessage(renderHwnd, WM_MOUSEACTIVATE, _hostFormHwnd, (IntPtr)((WM_LBUTTONDOWN << 16) | HTCLIENT));
                    WinApiHelpers.SendMessage(renderHwnd, WM_SETCURSOR, renderHwnd, (IntPtr)((WM_LBUTTONDOWN << 16) | HTCLIENT));
                    WinApiHelpers.PostMessage(renderHwnd, WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);
                    WinApiHelpers.PostMessage(renderHwnd, (uint)msg, (IntPtr)0x0001 /* MK_LBUTTON */, clientLParam);
                    break;
                }

                case WM_LBUTTONUP:
                {
                    WinApiHelpers.PostMessage(renderHwnd, (uint)msg, IntPtr.Zero, clientLParam);
                    break;
                }

                case WM_RBUTTONDOWN:
                {
                    WinApiHelpers.SendMessage(renderHwnd, WM_MOUSEACTIVATE, _hostFormHwnd, (IntPtr)((WM_RBUTTONDOWN << 16) | HTCLIENT));
                    WinApiHelpers.SendMessage(renderHwnd, WM_SETCURSOR, renderHwnd, (IntPtr)((WM_RBUTTONDOWN << 16) | HTCLIENT));
                    WinApiHelpers.PostMessage(renderHwnd, (uint)msg, (IntPtr)0x0002 /* MK_RBUTTON */, clientLParam);
                    break;
                }

                case WM_RBUTTONUP:
                {
                    WinApiHelpers.PostMessage(renderHwnd, (uint)msg, IntPtr.Zero, clientLParam);
                    break;
                }

                case WM_MBUTTONDOWN:
                {
                    WinApiHelpers.SendMessage(renderHwnd, WM_MOUSEACTIVATE, _hostFormHwnd, (IntPtr)((WM_MBUTTONDOWN << 16) | HTCLIENT));
                    WinApiHelpers.SendMessage(renderHwnd, WM_SETCURSOR, renderHwnd, (IntPtr)((WM_MBUTTONDOWN << 16) | HTCLIENT));
                    WinApiHelpers.PostMessage(renderHwnd, (uint)msg, (IntPtr)0x0010 /* MK_MBUTTON */, clientLParam);
                    break;
                }

                case WM_MBUTTONUP:
                {
                    WinApiHelpers.PostMessage(renderHwnd, (uint)msg, IntPtr.Zero, clientLParam);
                    break;
                }

                case WM_MOUSEWHEEL:
                {
                    // WM_MOUSEWHEEL takes SCREEN coordinates in lParam, not client coordinates
                    IntPtr screenLParam = (IntPtr)((hookStruct.pt.Y << 16) | (hookStruct.pt.X & 0xFFFF));
                    WinApiHelpers.PostMessage(renderHwnd, (uint)msg, (IntPtr)hookStruct.mouseData, screenLParam);
                    break;
                }
            }
        }

        /// <summary>
        /// Locates and caches the handle of the innermost Chrome_RenderWidgetHostHWND rendering window.
        /// Chromium renders inside this handle; sending mouse messages here ensures DOM and Canvas
        /// events are triggered properly.
        /// </summary>
        private IntPtr GetOrFindRenderWidgetHwnd()
        {
            if (_targetRenderHwnd != IntPtr.Zero && WinApiHelpers.IsWindow(_targetRenderHwnd))
            {
                if (_isRenderWidgetConfirmed)
                    return _targetRenderHwnd;

                var sb = new StringBuilder(256);
                WinApiHelpers.GetClassName(_targetRenderHwnd, sb, sb.Capacity);
                if (sb.ToString() == "Chrome_RenderWidgetHostHWND")
                {
                    _isRenderWidgetConfirmed = true;
                    return _targetRenderHwnd;
                }
            }

            _targetRenderHwnd = WinApiHelpers.FindRenderWidgetHostHwnd(_hostFormHwnd);
            if (_targetRenderHwnd != IntPtr.Zero && WinApiHelpers.IsWindow(_targetRenderHwnd))
            {
                var sb = new StringBuilder(256);
                WinApiHelpers.GetClassName(_targetRenderHwnd, sb, sb.Capacity);
                if (sb.ToString() == "Chrome_RenderWidgetHostHWND")
                {
                    _isRenderWidgetConfirmed = true;
                    Logger.Info($"DesktopInputForwarder: Discovered Chrome_RenderWidgetHostHWND handle 0x{_targetRenderHwnd.ToInt64():X}");
                }
            }

            return _targetRenderHwnd;
        }

        /// <summary>
        /// Returns true if <paramref name="hwnd"/> is our host form itself or any descendant window
        /// (e.g. the WebView2 Chrome render widget).
        /// </summary>
        private bool IsOurWallpaperWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || _hostFormHwnd == IntPtr.Zero) return false;
            if (hwnd == _hostFormHwnd) return true;

            IntPtr ancestor = hwnd;
            for (int i = 0; i < 16; i++)
            {
                ancestor = WinApiHelpers.GetParent(ancestor);
                if (ancestor == IntPtr.Zero) break;
                if (ancestor == _hostFormHwnd) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true when <paramref name="hwnd"/> is a standard Windows desktop shell window
        /// that sits above our wallpaper (Progman, WorkerW, SHELLDLL_DefView, SysListView32).
        /// </summary>
        private static bool IsDesktopLayer(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;

            var sb = new StringBuilder(64);
            WinApiHelpers.GetClassName(hwnd, sb, sb.Capacity);
            string cls = sb.ToString();

            return cls == "Progman"
                || cls == "WorkerW"
                || cls == "SHELLDLL_DefView"
                || cls == "SysListView32";
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
