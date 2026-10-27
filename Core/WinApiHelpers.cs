using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using DynamicDesk.Utils;

namespace DynamicDesk.Core
{
    public static class WinApiHelpers
    {
        #region Constants

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const int WS_VISIBLE = 0x10000000;
        public const int WS_CHILD = 0x40000000;
        public const int WS_CLIPCHILDREN = 0x02000000;
        public const int WS_CLIPSIBLINGS = 0x04000000;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_SYSMENU = 0x00080000;

        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOPMOST = 0x00000008;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOREDRAW = 0x0008;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const uint SPI_SETDESKWALLPAPER = 0x0014;
        public const uint SPIF_UPDATEINIFILE = 0x01;
        public const uint SPIF_SENDCHANGE = 0x02;

        public const uint WM_SPAWN_WORKER = 0x052C;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int MA_NOACTIVATE = 3;

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        #endregion

        #region Native Methods

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(uint uAction, uint uParam, string? lpvParam, uint fuWinIni);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        /// Recursively searches for the actual Chromium rendering surface window (Chrome_RenderWidgetHostHWND).
        /// Chromium ignores mouse messages posted to parent container windows like Chrome_WidgetWin_0.
        /// They must be sent directly to Chrome_RenderWidgetHostHWND.
        /// </summary>
        public static IntPtr FindRenderWidgetHostHwnd(IntPtr parentHwnd)
        {
            if (parentHwnd == IntPtr.Zero || !IsWindow(parentHwnd)) return IntPtr.Zero;

            IntPtr found = IntPtr.Zero;

            // Pass 1: Look specifically for Chrome_RenderWidgetHostHWND
            EnumChildWindows(parentHwnd, (child, _) =>
            {
                var sb = new StringBuilder(256);
                GetClassName(child, sb, sb.Capacity);
                string cls = sb.ToString();
                if (cls == "Chrome_RenderWidgetHostHWND")
                {
                    found = child;
                    return false; // Found exact render widget! Stop search.
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero) return found;

            // Pass 2: Fallback to any child containing RenderWidgetHostHWND
            EnumChildWindows(parentHwnd, (child, _) =>
            {
                var sb = new StringBuilder(256);
                GetClassName(child, sb, sb.Capacity);
                string cls = sb.ToString();
                if (cls.Contains("RenderWidgetHostHWND"))
                {
                    found = child;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero) return found;

            // Pass 3: Fallback to Chrome_WidgetWin
            EnumChildWindows(parentHwnd, (child, _) =>
            {
                var sb = new StringBuilder(256);
                GetClassName(child, sb, sb.Capacity);
                string cls = sb.ToString();
                if (cls.StartsWith("Chrome_WidgetWin"))
                {
                    found = child;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return found != IntPtr.Zero ? found : parentHwnd;
        }

        public static IntPtr GetInnermostWindow(IntPtr parentHwnd) => FindRenderWidgetHostHwnd(parentHwnd);

        #endregion

        #region Desktop Shell Integration

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        /// <summary>
        /// Embeds a Form's HWND into the desktop layer behind icons.
        /// Handles both Windows 11 (Progman + SHELLDLL_DefView Z-order) and Windows 10 (WorkerW sibling).
        /// </summary>
        public static bool EmbedWindowToDesktop(IntPtr childHwnd, Rectangle bounds)
        {
            try
            {
                IntPtr progman = FindWindow("Progman", null);
                if (progman == IntPtr.Zero)
                {
                    Logger.Warn("Progman window not found.");
                    return false;
                }

                // Send 0x052C to Progman to initialize WorkerW structure
                SendMessageTimeout(progman, WM_SPAWN_WORKER, IntPtr.Zero, IntPtr.Zero, 0x0002, 1000, out _);

                // Prepare window style: child, visible, no caption, no borders
                long style = GetWindowLongPtr(childHwnd, GWL_STYLE).ToInt64();
                style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU);
                style |= (WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS | WS_VISIBLE);
                SetWindowLongPtr(childHwnd, GWL_STYLE, new IntPtr(style));

                // ToolWindow style prevents appearance in Alt+Tab; allow clicks so wallpaper is interactive
                long exStyle = GetWindowLongPtr(childHwnd, GWL_EXSTYLE).ToInt64();
                exStyle |= WS_EX_TOOLWINDOW;
                exStyle &= ~WS_EX_TOPMOST;
                exStyle &= ~WS_EX_NOACTIVATE;
                SetWindowLongPtr(childHwnd, GWL_EXSTYLE, new IntPtr(exStyle));

                // Check Case 1: Windows 11 (23H2/24H2) where SHELLDLL_DefView resides inside Progman
                IntPtr shellViewInProgman = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellViewInProgman != IntPtr.Zero)
                {
                    Logger.Info($"SHELLDLL_DefView is inside Progman (0x{progman.ToInt64():X}). Embedding behind desktop icons.");

                    // Add WS_CLIPCHILDREN to Progman so it does not draw over the wallpaper
                    long progStyle = GetWindowLongPtr(progman, GWL_STYLE).ToInt64();
                    progStyle |= WS_CLIPCHILDREN;
                    SetWindowLongPtr(progman, GWL_STYLE, new IntPtr(progStyle));

                    SetParent(childHwnd, progman);

                    // Position childHwnd directly behind shellViewInProgman
                    SetWindowPos(
                        childHwnd,
                        shellViewInProgman,
                        bounds.X,
                        bounds.Y,
                        bounds.Width,
                        bounds.Height,
                        SWP_SHOWWINDOW | SWP_FRAMECHANGED
                    );

                    ShowWindow(childHwnd, SW_SHOW);
                    ShowWindow(progman, SW_SHOW);
                    Logger.Info($"Successfully embedded HWND 0x{childHwnd.ToInt64():X} behind SHELLDLL_DefView inside Progman.");
                    return true;
                }

                // Check Case 2: Windows 10 / earlier Windows 11 where SHELLDLL_DefView is in a WorkerW
                IntPtr targetWorkerW = IntPtr.Zero;
                EnumWindows((topHandle, _) =>
                {
                    IntPtr defView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (defView != IntPtr.Zero)
                    {
                        targetWorkerW = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                        if (targetWorkerW != IntPtr.Zero)
                        {
                            return false; // Found sibling
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                if (targetWorkerW != IntPtr.Zero)
                {
                    ShowWindow(targetWorkerW, SW_SHOW);
                    SetParent(childHwnd, targetWorkerW);

                    SetWindowPos(
                        childHwnd,
                        IntPtr.Zero,
                        bounds.X,
                        bounds.Y,
                        bounds.Width,
                        bounds.Height,
                        SWP_NOZORDER | SWP_SHOWWINDOW | SWP_FRAMECHANGED
                    );

                    ShowWindow(childHwnd, SW_SHOW);
                    Logger.Info($"Successfully embedded HWND 0x{childHwnd.ToInt64():X} into WorkerW 0x{targetWorkerW.ToInt64():X}.");
                    return true;
                }

                // Check Case 3: Fallback layer to HWND_BOTTOM
                Logger.Info("Fallback: Layering host window to HWND_BOTTOM.");
                style |= WS_VISIBLE;
                style &= ~WS_CHILD;
                SetWindowLongPtr(childHwnd, GWL_STYLE, new IntPtr(style));

                SetWindowPos(
                    childHwnd,
                    HWND_BOTTOM,
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    bounds.Height,
                    SWP_SHOWWINDOW | SWP_FRAMECHANGED
                );

                ShowWindow(childHwnd, SW_SHOW);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during EmbedWindowToDesktop", ex);
                return false;
            }
        }

        #endregion

        #region Wallpaper Registry Configuration

        public static void SetWallpaperStyle(string style)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                if (key == null) return;

                switch (style.ToLowerInvariant())
                {
                    case "fit":
                        key.SetValue("WallpaperStyle", "6");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case "stretch":
                        key.SetValue("WallpaperStyle", "2");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case "tile":
                        key.SetValue("WallpaperStyle", "0");
                        key.SetValue("TileWallpaper", "1");
                        break;
                    case "center":
                        key.SetValue("WallpaperStyle", "0");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case "span":
                        key.SetValue("WallpaperStyle", "22");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case "fill":
                    default:
                        key.SetValue("WallpaperStyle", "10");
                        key.SetValue("TileWallpaper", "0");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update wallpaper style in registry for '{style}'", ex);
            }
        }

        #endregion
    }
}
