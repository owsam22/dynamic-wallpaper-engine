using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.Core
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

        #endregion

        #region Desktop WorkerW Integration

        /// <summary>
        /// Locates or triggers creation of the WorkerW window immediately behind desktop icons.
        /// NEVER returns Progman as reparenting into Progman freezes Explorer and corrupts desktop rendering.
        /// </summary>
        public static IntPtr GetDesktopWorkerW()
        {
            IntPtr progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                Logger.Warn("Progman window not found.");
                return IntPtr.Zero;
            }

            // Message 0x052C instructs Progman to spawn WorkerW behind SHELLDLL_DefView.
            // 0x0002 is SMTO_ABORTIFHUNG to prevent any UI freezing.
            SendMessageTimeout(progman, WM_SPAWN_WORKER, IntPtr.Zero, IntPtr.Zero, 0x0002, 1000, out _);
            SendMessageTimeout(progman, WM_SPAWN_WORKER, new IntPtr(0xD), new IntPtr(0x1), 0x0002, 1000, out _);

            IntPtr workerW = IntPtr.Zero;

            // Search for WorkerW window
            for (int attempt = 0; attempt < 8; attempt++)
            {
                // Method 1: WorkerW sibling immediately behind the window containing SHELLDLL_DefView
                EnumWindows((topHandle, _) =>
                {
                    IntPtr shellView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shellView != IntPtr.Zero)
                    {
                        IntPtr candidate = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                        if (candidate != IntPtr.Zero)
                        {
                            workerW = candidate;
                            return false; // Found! Stop enumeration
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                if (workerW != IntPtr.Zero)
                {
                    Logger.Info($"Found Desktop WorkerW via sibling Z-order: 0x{workerW.ToInt64():X}");
                    return workerW;
                }

                // Method 2: On some Windows 11 builds, find any top-level WorkerW that does not contain SHELLDLL_DefView
                EnumWindows((topHandle, _) =>
                {
                    var sb = new StringBuilder(256);
                    GetClassName(topHandle, sb, sb.Capacity);
                    if (sb.ToString() == "WorkerW")
                    {
                        IntPtr shellView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (shellView == IntPtr.Zero)
                        {
                            workerW = topHandle;
                            return false; // Found worker window
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                if (workerW != IntPtr.Zero)
                {
                    Logger.Info($"Found Desktop WorkerW via class search: 0x{workerW.ToInt64():X}");
                    return workerW;
                }

                // Retry spawning message if not yet ready
                SendMessageTimeout(progman, WM_SPAWN_WORKER, IntPtr.Zero, IntPtr.Zero, 0x0002, 500, out _);
                Thread.Sleep(60);
            }

            Logger.Warn("Could not locate desktop WorkerW; will layer to HWND_BOTTOM without SetParent.");
            return IntPtr.Zero;
        }

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        /// <summary>
        /// Embeds a Form's HWND into the desktop layer behind icons.
        /// If WorkerW is unavailable, safely layers to HWND_BOTTOM without reparenting.
        /// </summary>
        public static bool EmbedWindowToDesktop(IntPtr childHwnd, Rectangle bounds)
        {
            try
            {
                IntPtr desktopHost = GetDesktopWorkerW();

                // Make sure window has child/popup style and no borders
                long style = GetWindowLongPtr(childHwnd, GWL_STYLE).ToInt64();
                style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU);

                // Make sure extended styles do not show in Alt+Tab and do not steal focus
                long exStyle = GetWindowLongPtr(childHwnd, GWL_EXSTYLE).ToInt64();
                exStyle |= (WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
                exStyle &= ~WS_EX_TOPMOST;
                SetWindowLongPtr(childHwnd, GWL_EXSTYLE, new IntPtr(exStyle));

                if (desktopHost != IntPtr.Zero)
                {
                    style |= (WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS | WS_VISIBLE);
                    SetWindowLongPtr(childHwnd, GWL_STYLE, new IntPtr(style));

                    IntPtr prevParent = SetParent(childHwnd, desktopHost);
                    int setParentError = Marshal.GetLastWin32Error();
                    if (prevParent == IntPtr.Zero && setParentError != 0)
                    {
                        Logger.Warn($"SetParent reported win32 error code {setParentError}.");
                    }

                    SetWindowPos(
                        childHwnd,
                        IntPtr.Zero,
                        bounds.X,
                        bounds.Y,
                        bounds.Width,
                        bounds.Height,
                        SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED
                    );

                    ShowWindow(childHwnd, SW_SHOW);
                    Logger.Info($"Embedded HWND 0x{childHwnd.ToInt64():X} to Desktop WorkerW 0x{desktopHost.ToInt64():X} at {bounds}.");
                    return true;
                }
                else
                {
                    // Fallback: Layer to HWND_BOTTOM behind all normal applications without reparenting
                    Logger.Info("Layering host window to HWND_BOTTOM.");
                    style |= WS_VISIBLE;
                    SetWindowLongPtr(childHwnd, GWL_STYLE, new IntPtr(style));

                    SetWindowPos(
                        childHwnd,
                        HWND_BOTTOM,
                        bounds.X,
                        bounds.Y,
                        bounds.Width,
                        bounds.Height,
                        SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED
                    );

                    ShowWindow(childHwnd, SW_SHOW);
                    return true;
                }
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
