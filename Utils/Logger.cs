using System;
using System.IO;
using System.Threading;

namespace SmartWallpaperApp.Utils
{
    public static class Logger
    {
        private static readonly string LogDirectory;
        private static readonly object FileLock = new();

        static Logger()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                LogDirectory = Path.Combine(localAppData, "SmartWallpaperApp", "Logs");
                Directory.CreateDirectory(LogDirectory);
                CleanOldLogs();
            }
            catch
            {
                LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(LogDirectory);
            }
        }

        public static string CurrentLogFile
        {
            get
            {
                string fileName = $"app-{DateTime.Now:yyyyMMdd}.log";
                return Path.Combine(LogDirectory, fileName);
            }
        }

        public static void Info(string message) => Log("INFO", message);
        public static void Warn(string message) => Log("WARN", message);
        public static void Error(string message, Exception? ex = null)
        {
            string fullMsg = ex != null ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}" : message;
            Log("ERROR", fullMsg);
        }
        public static void Debug(string message) => Log("DEBUG", message);

        private static void Log(string level, string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level,-5}] [{Thread.CurrentThread.ManagedThreadId:D2}] {message}";

            lock (FileLock)
            {
                try
                {
                    File.AppendAllText(CurrentLogFile, logLine + Environment.NewLine);
                }
                catch
                {
                    // Avoid crashing on logging errors
                }
            }
        }

        private static void CleanOldLogs()
        {
            try
            {
                if (!Directory.Exists(LogDirectory)) return;

                var directory = new DirectoryInfo(LogDirectory);
                var files = directory.GetFiles("app-*.log");
                DateTime cutoff = DateTime.Now.AddDays(-7);

                foreach (var file in files)
                {
                    if (file.LastWriteTime < cutoff)
                    {
                        try { file.Delete(); } catch { }
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
