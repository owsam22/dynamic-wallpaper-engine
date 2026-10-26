# SmartWallpaperApp — Windows 11 Power-Based Live Wallpaper Application

>currently building this project, don't install it now , there will be many bugs and errors

A native Windows 11 desktop application that automatically transitions between a dynamic, responsive HTML/web live wallpaper (when connected to AC power) and an energy-efficient static image wallpaper (when operating on battery power).

Built with **C# (.NET 9)**, **Windows Forms**, **Microsoft Edge WebView2**, and **Win32 Shell APIs**.

---

## 1. Application Objective & Core Behavior

- **AC Power Connected:** Displays a configurable live wallpaper rendered via Microsoft Edge WebView2 (supporting local HTML5/CSS3/Canvas/WebGL animations and remote `https://` websites).
- **Battery Power:** Stops all live rendering, disposes and frees WebView2 GPU/CPU resources, and applies a user-selected static wallpaper using the native Windows wallpaper subsystem.
- **Power Changes:** Automatically switches modes without user intervention, debouncing rapid events and executing serialized state transitions.
- **Application Startup:** Detects current hardware power status and applies the correct wallpaper immediately.
- **Unknown Power State Fallback:** Safely treats unknown power status as on-battery / static mode to prevent unintended battery depletion.
- **Desktop Shell Integration:** Live wallpaper is embedded behind desktop icons via the Windows 11 `WorkerW` / `Progman` desktop hierarchy.

---

## 2. Architecture & Component Structure

```
DynamicDesk/
├── Assets/
│   ├── example_wallpaper.html      # Responsive HTML5 canvas wave/particle animated wallpaper (offline)
│   └── default_wallpaper.png       # High-resolution default static wallpaper image
├── Core/
│   ├── WinApiHelpers.cs            # Native Win32 interop, WorkerW desktop embedding, wallpaper registry
│   ├── PowerMonitor.cs             # Power detection (SystemInformation, WM_POWERBROADCAST, debouncing)
│   └── FullscreenDetector.cs       # Automatic detection of foreground fullscreen apps (games/media)
├── Models/
│   └── AppSettings.cs              # Strongly typed settings model
├── Services/
│   ├── SettingsManager.cs          # JSON settings persistence & Windows startup registration
│   ├── StaticWallpaperManager.cs   # SystemParametersInfo SPI_SETDESKWALLPAPER coordinator
│   ├── WebWallpaperEngine.cs       # WebView2 lifecycle, virtual host mapping, suspension
│   └── WallpaperManager.cs         # Central state machine & thread-safe transition coordinator
├── UI/
│   ├── WallpaperHostForm.cs        # Borderless, non-activating window embedded into desktop layer
│   ├── SettingsForm.cs             # Full settings and real-time diagnostic control panel
│   └── MainForm.cs                 # System-tray application host, message receiver, lifecycle coordinator
├── Utils/
│   └── Logger.cs                   # Rolling thread-safe file logger (%LocalAppData%\SmartWallpaperApp\Logs)
├── Program.cs                      # Entry point, single-instance mutex, global exception handling
├── app.manifest                    # PerMonitorV2 DPI awareness & Windows 11 compatibility
├── DynamicDesk.csproj              # .NET 9 Windows Forms project with WebView2
└── README.md
```

---

## 3. Desktop Embedding: WorkerW Technique

In Windows 11, the desktop background and desktop icons are managed by Windows Explorer (`explorer.exe`):
1. **Progman (`Program Manager`):** The primary shell window.
2. **`SHELLDLL_DefView` & `SysListView32`:** Contain desktop icons and user interactions.
3. **`WorkerW` Layering:**
   - Sending `0x052C` (`WM_SPAWN_WORKER`) to `Progman` causes Explorer to fork its window structure into two `WorkerW` layers.
   - The top layer hosts `SHELLDLL_DefView` (desktop icons).
   - The sibling layer directly behind it is the target `WorkerW`.
   - `SmartWallpaperApp` embeds the live host window (`WallpaperHostForm`) as a child of this sibling `WorkerW` using `SetParent`.
   - The window is styled with `WS_CHILD`, `WS_CLIPCHILDREN`, `WS_CLIPSIBLINGS`, and `WS_EX_NOACTIVATE` / `WS_EX_TOOLWINDOW` so it never steals focus, does not appear in Alt+Tab, and leaves desktop icons fully clickable and functional.
4. **Fallback Mechanism:** If the host process is running outside the interactive shell station or if `WorkerW` is unavailable, `SmartWallpaperApp` safely layers the host window to `HWND_BOTTOM` behind all regular windows without covering the taskbar or desktop icons.

---

## 4. Live Wallpaper Support (Local HTML & Remote Websites)

- **Local HTML Files:**
  - Bundled example: `Assets/example_wallpaper.html` contains an animated particle constellation and radiant wave matrix with zero external dependencies.
  - Virtual host mapping: `CoreWebView2.SetVirtualHostNameToFolderMapping("wallpaper-local", folder, ...)` allows local HTML files to load local CSS, JavaScript modules, and images without CORS issues.
- **Remote Websites:**
  - Validates `http://` and `https://` URLs.
  - Enforces certificate security.
  - Automatic scrollbar and margin suppression script injected on document creation.
- **Resource Management:**
  - When switching to battery mode, the live engine and WebView2 runtime resources are completely disposed.
  - When running on AC and a fullscreen game or media player is detected, WebView2 is suspended via `TrySuspendAsync()` to save GPU and CPU resources.

---

## 5. Configuration & Persistence

All application settings are stored in JSON format at:
```
%AppData%\SmartWallpaperApp\settings.json
```

### Configuration Options:
- `LiveSourceType`: `"LocalHtml"` or `"WebsiteUrl"`.
- `LiveSourcePath`: File path or web URL.
- `StaticImagePath`: Path to static image (`.jpg`, `.jpeg`, `.png`, `.bmp`).
- `StaticWallpaperStyle`: `"Fill"`, `"Fit"`, `"Stretch"`, `"Tile"`, `"Center"`, or `"Span"`.
- `AutoSwitchEnabled`: `true` to enable automatic switching.
- `PollingIntervalSeconds`: Fallback polling period (default: `10`s).
- `PauseOnFullscreen`: Suspend WebView2 rendering during fullscreen applications.
- `PauseBelowBatteryPercent`: Switch to static mode if battery is below threshold (e.g. `20%`).
- `SimulationMode`: `"Actual"`, `"SimulatedAC"`, or `"SimulatedBattery"`.
- `SimulatedBatteryPercent`: Custom battery % for simulation testing.
- `StartWithWindows`: Toggles registry entry in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

---

## 6. Power Simulation & Testing Mode

Testing power switching on desktop PCs or laptops plugged into power docks is fully supported:
1. Open **Settings** (or right-click the system tray icon > **Simulation Override**).
2. Choose:
   - **Actual:** Uses real Windows battery and AC power status.
   - **Simulated AC:** Forces AC Connected state.
   - **Simulated Battery:** Forces On Battery state with configurable percentage.
3. The application immediately reacts and transitions wallpapers accordingly, allowing thorough end-to-end verification.

---

## 7. Diagnostics & Logging

Persistent rolling logs are written to:
```
%LocalAppData%\SmartWallpaperApp\Logs\app-YYYYMMDD.log
```
Logs include timestamped records of power changes, state transitions, WebView2 initialization status, native Win32 API calls, display changes, and exceptions. Logs older than 7 days are automatically purged.

---

## 8. Build & Execution

### Prerequisites:
- Windows 10 (1809+) or Windows 11.
- .NET 9.0 SDK or .NET 10.0 SDK.
- Microsoft Edge WebView2 Runtime (pre-installed on Windows 11).

### Build Solution:
```bash
dotnet restore
dotnet build -c Release
```

### Run Application:
```bash
dotnet run
# OR execute directly:
.\bin\Release\net9.0-windows\SmartWallpaperApp.exe
```

The application runs in the **Windows system tray**. Double-click the tray icon to open the Settings & Control Panel.
