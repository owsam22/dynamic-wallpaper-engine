# DynamicDesk

<p align="center">
  <strong>A smarter wallpaper experience for Windows.</strong>
  <br />
  Switch between interactive live HTML wallpapers and static backgrounds based on your PC's power state.
  <br /><br />
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest">
    <img src="https://img.shields.io/github/v/release/owsam22/dynamic-wallpaper-engine?style=for-the-badge&label=Latest%20Release" alt="Latest Release" />
  </a>
  </p>

<p align="center">
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest"><strong>Download for Windows</strong></a>
  &nbsp; · &nbsp;
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/issues">Report a Bug</a>
  &nbsp; · &nbsp;
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases">All Releases</a>
</p>

---

## Overview

**DynamicDesk** is a Windows desktop application designed to make your desktop more dynamic while adapting to your computer's power state.

Enjoy interactive HTML wallpapers when connected to AC power and switch to static wallpapers when running on battery.

Built with C#, .NET, Windows Forms, and Microsoft Edge WebView2, DynamicDesk combines web-based wallpaper experiences with native Windows integration.

## Features

- **Power-aware wallpaper switching** — switch wallpaper modes according to AC power and battery status.
- **Interactive HTML wallpapers** — display web-based wallpapers with interactive content.
- **Static wallpapers** — use image-based backgrounds for battery mode.
- **Wallpaper interaction** — interact with supported live wallpaper content.
- **System tray integration** — access the application from the Windows notification area.
- **Settings interface** — configure the application's available options.
- **Native Windows application** — built with C# and Windows Forms.
- **WebView2 rendering** — use Microsoft Edge WebView2 for HTML wallpaper content.
 
 >note : live url wallpaper need internet connection to display. and the wallpaper will be display only when the application is running. when the application is closed, the wallpaper will be closed too.
 
## Download and Install

Get the latest Windows build directly from GitHub Releases.

<p align="center">
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest">
    <img src="https://img.shields.io/badge/Download-DynamicDesk%20for%20Windows-2ea44f?style=for-the-badge&logo=windows" alt="Download DynamicDesk for Windows" />
  </a>
</p>

### Installation steps

1. Open the [**Latest DynamicDesk Release**](https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest).
2. Scroll down to the **Assets** section.
3. Download `DynamicDesk-windows-x64.zip` (or the ZIP filename shown in the release).
4. Right-click the downloaded ZIP and select **Extract All**.
5. Open the extracted folder.
6. Run `DynamicDesk.exe`.
7. If prompted or required, install the [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/).

**Important:** Extract the complete ZIP before running the application. Do not move only the executable out of the folder unless the release explicitly provides a tested standalone executable.

You do not need Visual Studio to run a correctly packaged release.

## How to Use

### 1. Launch DynamicDesk

Open the extracted application folder and double-click `DynamicDesk.exe`.

### 2. Open the settings

If DynamicDesk starts in the system tray, find its icon in the Windows notification area. Open the tray menu or double-click the icon to access the settings interface, depending on the available controls.

### 3. Configure your wallpapers

Choose the supported live HTML wallpaper and static wallpaper options using the application's settings.

- **Live wallpaper:** select the HTML wallpaper content you want to display.
- **Static wallpaper:** select an image for the static wallpaper mode.
- **Power behavior:** configure the available options for AC power and battery operation.

The exact settings and supported file formats depend on the installed version.

### 4. Connect or disconnect power

With power-aware switching enabled, DynamicDesk is designed to use the live wallpaper while connected to AC power and the static wallpaper when running on battery.

### 5. Use interactive wallpapers

When a live HTML wallpaper supports interaction, use your mouse to interact with its content. The exact interaction behavior depends on the wallpaper and the application's current implementation.

<p align="center">
  <a href="https://owsam22.github.io/pixel-engineer-live-wallpaper/" target="_blank">
    <strong>Open Pixel Engineer Live Wallpaper ↗</strong>
  </a>
</p>

<p align="center">
  <code>https://owsam22.github.io/pixel-engineer-live-wallpaper/</code>
</p>

## System Requirements

- A compatible 64-bit Windows system.
- Microsoft Edge WebView2 Runtime for HTML wallpaper rendering.
- The .NET runtime is included if the release is published as a self-contained build.
- A compatible HTML wallpaper and image file for the wallpaper modes you want to use.

Refer to the release notes for version-specific requirements.

## Built With

| Technology | Purpose |
|---|---|
| C# | Application logic |
| .NET | Application framework |
| Windows Forms | Desktop interface |
| Microsoft Edge WebView2 | HTML wallpaper rendering |
| Windows APIs | Power-state monitoring and Windows integration |

## Run from Source

Developers can build and run DynamicDesk locally.

### Prerequisites

- Git
- A .NET SDK compatible with the project's target framework
- Windows development environment
- Microsoft Edge WebView2 Runtime

### Clone the repository

```bash
git clone https://github.com/owsam22/dynamic-wallpaper-engine.git
cd dynamic-wallpaper-engine
```

### Restore dependencies

```bash
dotnet restore
```

### Build the application

```bash
dotnet build -c Release
```

### Run the application

```bash
dotnet run
```

Run these commands from the directory containing `DynamicDesk.csproj`. The installed .NET SDK must support the target framework specified in the project file.

## Updates

To get a newer version:

1. Visit the [GitHub Releases page](https://github.com/owsam22/dynamic-wallpaper-engine/releases).
2. Download the latest release ZIP.
3. Extract it into a new folder.
4. Run the new version.

Keep your personal wallpaper files and any settings you want to preserve before replacing an older version.

## Troubleshooting

**The application does not start**
- Extract the entire ZIP before running the executable.
- Check that your Windows version is supported.
- Review any error message and verify that you downloaded the correct release.

**HTML wallpapers do not render**
- Install or repair the Microsoft Edge WebView2 Runtime.
- Verify that the HTML wallpaper and its required assets are available.

**Wallpaper switching does not work**
- Check the available power-state settings.
- Test while connected to AC power and while running on battery.
- Report the issue with your Windows version and steps to reproduce it.

**Windows displays a security warning**
- Verify that the download came from the official repository's Releases page.
- Review the release and its source before deciding whether to run it.

## Report Issues and Contribute

Found a bug or have an idea for an improvement?

- [Report a bug](https://github.com/owsam22/dynamic-wallpaper-engine/issues/new)
- [Browse existing issues](https://github.com/owsam22/dynamic-wallpaper-engine/issues)
- [View all releases](https://github.com/owsam22/dynamic-wallpaper-engine/releases)
- [Explore the source code](https://github.com/owsam22/dynamic-wallpaper-engine)

When reporting a bug, include your Windows version, the application version, steps to reproduce the issue, and relevant error messages.

Contributions and constructive feedback are welcome.

## About the Developer

DynamicDesk is developed by **Samarpan Jayswal (owsam22)**.

- **Portfolio:** [portfolio.owsam22.com](https://portfolio.owsam22.com/)
- **Website:** [owsam22.com](https://owsam22.com/)
- **GitHub:** [github.com/owsam22](https://github.com/owsam22)
- **Contact:** [contact@owsam22.com](mailto:contact@owsam22.com)

For questions, feedback, collaboration, or project-related inquiries, feel free to get in touch.

---

<p align="center">
  <strong>DynamicDesk</strong>
  <br />
  Built by <a href="https://github.com/owsam22">owsam22</a>
  <br /><br />
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest">Download</a>
  &nbsp; · &nbsp;
  <a href="https://portfolio.owsam22.com/">Portfolio</a>
  &nbsp; · &nbsp;
  <a href="https://owsam22.com/">Website</a>
  <br /><br />
  <sub>Make your desktop adapt to the way you use your PC.</sub>
</p>