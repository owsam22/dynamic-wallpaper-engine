using System;
using System.Threading;
using System.Windows.Forms;
using SmartWallpaperApp.UI;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp
{
    internal static class Program
    {
        private const string AppMutexName = @"Global\SmartWallpaperApp_SingleInstance_Mutex_99F";
        private static Mutex? _appMutex;

        [STAThread]
        static void Main()
        {
            // Global Exception Handlers
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                Logger.Error("Unhandled Thread Exception occurred", e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Logger.Error("Unhandled Domain Exception occurred", ex);
                }
            };

            _appMutex = new Mutex(true, AppMutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                Logger.Warn("Another instance of SmartWallpaperApp is already running. Exiting.");
                MessageBox.Show(
                    "Smart Wallpaper App is already running in the system tray.",
                    "Smart Wallpaper",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            try
            {
                Logger.Info("SmartWallpaperApp starting up...");
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
                Logger.Info("SmartWallpaperApp terminated normally.");
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal exception in main application loop", ex);
            }
            finally
            {
                if (_appMutex != null)
                {
                    try { _appMutex.ReleaseMutex(); } catch { }
                    _appMutex.Dispose();
                }
            }
        }
    }
}
