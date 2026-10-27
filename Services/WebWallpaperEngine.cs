using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using SmartWallpaperApp.UI;
using SmartWallpaperApp.Utils;

namespace SmartWallpaperApp.Services
{
    public class WebWallpaperEngine : IDisposable
    {
        private WallpaperHostForm? _hostForm;
        private CoreWebView2Environment? _environment;
        private string? _currentLoadedSource;
        private bool _isSuspended;
        private readonly string _userDataFolder;

        public bool IsRunning => _hostForm != null && !_hostForm.IsDisposed && _hostForm.Visible;
        public string? CurrentSource => _currentLoadedSource;
        public string InitializationStatus { get; private set; } = "Not initialized";

        public WebWallpaperEngine()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _userDataFolder = Path.Combine(localAppData, "SmartWallpaperApp", "WebView2Data");
        }

        public static bool IsWebView2RuntimeAvailable(out string versionOrError)
        {
            try
            {
                string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                versionOrError = $"Available (v{version})";
                return true;
            }
            catch (Exception ex)
            {
                versionOrError = $"WebView2 Runtime not found: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> StartAsync(string sourceType, string sourcePath, bool forceReload = false)
        {
            try
            {
                if (!IsWebView2RuntimeAvailable(out string runtimeDiagnostic))
                {
                    InitializationStatus = runtimeDiagnostic;
                    Logger.Error($"Cannot start WebWallpaperEngine: {runtimeDiagnostic}");
                    return false;
                }

                if (!ValidateSource(sourceType, sourcePath, out string validationError))
                {
                    InitializationStatus = validationError;
                    Logger.Warn($"Source validation failed: {validationError}");
                    return false;
                }

                // If already running with the exact same source, just resume and re-embed if needed
                if (IsRunning && !forceReload && string.Equals(_currentLoadedSource, sourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    Resume();
                    _hostForm?.AttachToDesktop();
                    return true;
                }

                // Clean up previous instance before starting anew
                Stop();

                _hostForm = new WallpaperHostForm();

                if (_environment == null)
                {
                    Directory.CreateDirectory(_userDataFolder);
                    _environment = await CoreWebView2Environment.CreateAsync(
                        browserExecutableFolder: null,
                        userDataFolder: _userDataFolder,
                        options: new CoreWebView2EnvironmentOptions()
                    );
                }

                // Initialize WebView2 before embedding into desktop to prevent message pump deadlocks
                await _hostForm.WebView.EnsureCoreWebView2Async(_environment);

                ConfigureWebViewSettings(_hostForm.WebView.CoreWebView2);

                bool navigated = await NavigateAsync(_hostForm.WebView.CoreWebView2, sourceType, sourcePath);
                if (navigated)
                {
                    _hostForm.Show();
                    bool embedded = _hostForm.AttachToDesktop();
                    if (!embedded)
                    {
                        Logger.Warn("WebWallpaperEngine: Initial desktop embedding failed. Layered to bottom.");
                    }

                    _currentLoadedSource = sourcePath;
                    InitializationStatus = "Running";
                    _isSuspended = false;
                    Logger.Info($"WebWallpaperEngine successfully started with source: {sourcePath}");
                    return true;
                }
                else
                {
                    InitializationStatus = "Navigation failed";
                    Stop();
                    return false;
                }
            }
            catch (Exception ex)
            {
                InitializationStatus = $"Error: {ex.Message}";
                Logger.Error("Exception in WebWallpaperEngine.StartAsync", ex);
                Stop();
                return false;
            }
        }

        private static bool ValidateSource(string sourceType, string sourcePath, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                error = "Source path or URL is empty.";
                return false;
            }

            if (!sourceType.Equals("WebsiteUrl", StringComparison.OrdinalIgnoreCase))
            {
                if (!File.Exists(sourcePath))
                {
                    error = $"Local live wallpaper file does not exist: {sourcePath}";
                    return false;
                }
                string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
                string[] validExts = { ".html", ".htm", ".mp4", ".webm", ".mov", ".mkv" };
                if (Array.IndexOf(validExts, ext) < 0)
                {
                    error = $"Unsupported file extension '{ext}'. Please select an HTML or video file (.html, .htm, .mp4, .webm).";
                    return false;
                }
            }
            else // WebsiteUrl
            {
                if (!Uri.TryCreate(sourcePath, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    error = $"Invalid website URL (must start with http:// or https://): {sourcePath}";
                    return false;
                }
            }

            return true;
        }

        private static void ConfigureWebViewSettings(CoreWebView2 core)
        {
            var settings = core.Settings;
            settings.AreDefaultContextMenusEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.IsZoomControlEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = false;
            settings.IsBuiltInErrorPageEnabled = true;

            // Injected script to suppress scrollbars
            core.AddScriptToExecuteOnDocumentCreatedAsync(@"
                document.addEventListener('DOMContentLoaded', () => {
                    const style = document.createElement('style');
                    style.innerHTML = 'html, body { overflow: hidden !important; margin: 0 !important; padding: 0 !important; }';
                    document.head.appendChild(style);
                });
            ");
        }

        private static Task<bool> NavigateAsync(CoreWebView2 core, string sourceType, string sourcePath)
        {
            var tcs = new TaskCompletionSource<bool>();

            void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                core.NavigationCompleted -= OnNavigationCompleted;
                if (e.IsSuccess)
                {
                    tcs.TrySetResult(true);
                }
                else
                {
                    Logger.Error($"WebView2 navigation failed with status {e.WebErrorStatus} (HTTP status: {e.HttpStatusCode})");
                    tcs.TrySetResult(false);
                }
            }

            core.NavigationCompleted += OnNavigationCompleted;

            try
            {
                if (!sourceType.Equals("WebsiteUrl", StringComparison.OrdinalIgnoreCase))
                {
                    string folder = Path.GetDirectoryName(sourcePath) ?? "";
                    string fileName = Path.GetFileName(sourcePath);
                    string ext = Path.GetExtension(sourcePath).ToLowerInvariant();

                    core.SetVirtualHostNameToFolderMapping(
                        "wallpaper-local",
                        folder,
                        CoreWebView2HostResourceAccessKind.Allow
                    );

                    if (ext == ".mp4" || ext == ".webm" || ext == ".mov" || ext == ".mkv")
                    {
                        string mime = ext switch
                        {
                            ".webm" => "video/webm",
                            ".mov" => "video/quicktime",
                            ".mkv" => "video/x-matroska",
                            _ => "video/mp4"
                        };
                        string videoSrc = $"https://wallpaper-local/{Uri.EscapeDataString(fileName)}";
                        string videoHtml = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<style>
  * {{ margin: 0; padding: 0; overflow: hidden; background: #000; }}
  video {{ width: 100vw; height: 100vh; object-fit: cover; position: fixed; top: 0; left: 0; }}
</style>
</head>
<body>
  <video autoplay loop muted playsinline>
    <source src=""{videoSrc}"" type=""{mime}"">
  </video>
</body>
</html>";
                        core.NavigateToString(videoHtml);
                    }
                    else
                    {
                        core.Navigate($"https://wallpaper-local/{Uri.EscapeDataString(fileName)}");
                    }
                }
                else
                {
                    core.Navigate(sourcePath);
                }
            }
            catch (Exception ex)
            {
                core.NavigationCompleted -= OnNavigationCompleted;
                Logger.Error($"Navigation initiation exception for '{sourcePath}'", ex);
                tcs.TrySetResult(false);
            }

            return tcs.Task;
        }

        public async void Suspend()
        {
            if (_hostForm != null && !_hostForm.IsDisposed &&
                _hostForm.WebView != null && !_hostForm.WebView.IsDisposed &&
                _hostForm.WebView.CoreWebView2 != null && !_isSuspended)
            {
                try
                {
                    await _hostForm.WebView.CoreWebView2.TrySuspendAsync();
                    _isSuspended = true;
                    Logger.Info("WebWallpaperEngine suspended (paused rendering).");
                }
                catch (Exception ex)
                {
                    Logger.Debug($"TrySuspendAsync not available or failed: {ex.Message}");
                }
            }
        }

        public void Resume()
        {
            if (_hostForm != null && !_hostForm.IsDisposed &&
                _hostForm.WebView != null && !_hostForm.WebView.IsDisposed &&
                _hostForm.WebView.CoreWebView2 != null && _isSuspended)
            {
                try
                {
                    _hostForm.WebView.CoreWebView2.Resume();
                    _isSuspended = false;
                    Logger.Info("WebWallpaperEngine resumed.");
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Resume failed: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _currentLoadedSource = null;
            InitializationStatus = "Stopped";
            _isSuspended = false;

            if (_hostForm != null)
            {
                try
                {
                    if (!_hostForm.IsDisposed)
                    {
                        _hostForm.Hide();
                        _hostForm.WebView?.Dispose();
                        _hostForm.Close();
                        _hostForm.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception while stopping WebWallpaperEngine", ex);
                }
                finally
                {
                    _hostForm = null;
                }
            }
        }

        public void Reattach()
        {
            if (IsRunning && _hostForm != null)
            {
                _hostForm.RefreshBounds();
                _hostForm.AttachToDesktop();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
