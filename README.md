# ⚡ DynamicDesk

> *First make it exist, then make it perfect.* ⚡

<p align="center">
  <strong>A smarter wallpaper experience for Windows.</strong>
  <br />
  Automatically switch between interactive live HTML wallpapers and static backgrounds based on your PC's power state.
  <br /><br />
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest">
    <img src="https://img.shields.io/github/v/release/owsam22/dynamic-wallpaper-engine?style=for-the-badge&label=Latest%20Release&color=0ea5e9" alt="Latest Release" />
  </a>
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine">
  </a>
 </p>

<p align="center">
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest"><strong>Download DynamicDesk</strong></a>
  &nbsp; · &nbsp;
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/issues">Report a Bug</a>
  &nbsp; · &nbsp;
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases">All Releases</a>
</p>

---

## 🚀 What's New in v1.1.0?

DynamicDesk has received a major user interface redesign!

This update focuses on making the application easier to use, navigate, and configure.

- **Completely redesigned interface** — a refreshed, modern, and more user-friendly experience.
- **Improved navigation** — easier access to application settings and features.
- **Better usability** — clearer layouts and simplified controls.
- **Visual refinements** — improved spacing, styling, and interface consistency.

**Current Version:** `v1.1.0`

For previous changes, visit the [GitHub Releases](https://github.com/owsam22/dynamic-wallpaper-engine/releases) page.

---

## 📖 Overview

**DynamicDesk** is a Windows desktop application that makes your desktop more dynamic while adapting to your computer's power state.

Enjoy interactive HTML wallpapers when your PC is connected to AC power and automatically switch to static backgrounds when running on battery.

Instead of manually changing wallpapers every time you unplug your laptop, DynamicDesk handles the transition for you.

Built using C#, .NET, Windows Forms, and Microsoft Edge WebView2, DynamicDesk combines web-based wallpaper experiences with native Windows integration.

## ✨ Features

| Feature | Description |
|---|---|
| ⚡ Power-aware switching | Automatically switches wallpaper modes based on AC power and battery status. |
| 🌐 Live HTML wallpapers | Display interactive, web-based wallpaper content. |
| 🖼️ Static wallpapers | Use image-based backgrounds when running on battery. |
| 🖱️ Wallpaper interaction | Interact with supported live wallpaper content. |
| 🎨 Modern interface | Configure and manage the application through its redesigned UI. |
| 📌 System tray integration | Access DynamicDesk from the Windows notification area. |
| 🔋 Power monitoring | Detect changes between AC and battery power. |
| 🧪 Simulation mode | Test power-state behavior without physically changing your power source. |
| ⏯️ Automation controls | Pause, resume, and manually reapply wallpaper modes. |
| 🖥️ Fullscreen detection | Includes fullscreen-aware wallpaper behavior. |
| 📝 Logging | Access application logs for troubleshooting. |

## 💡 How Does It Work?

DynamicDesk monitors your computer's power source and selects the appropriate wallpaper mode.

<p align="center">
  <strong>Connected to AC Power</strong>
  <br />
  ⚡ Live HTML Wallpaper
  <br /><br />
  ⬇️
  <br /><br />
  <strong>Running on Battery</strong>
  <br />
  🔋 Static Wallpaper
</p>

The goal is simple: enjoy interactive desktop experiences when plugged in while using a static background when you need to conserve battery.

**Important notes:**
- URL-based live wallpapers require an internet connection.
- Local HTML wallpapers can use their available local assets.
- Wallpapers are displayed only while DynamicDesk is running.
- Closing DynamicDesk stops its wallpaper functionality.

## 📥 Download and Installation

Get the latest version from the official GitHub Releases page.

<p align="center">
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest">
    <img src="https://img.shields.io/badge/Download-DynamicDesk%20for%20Windows-2ea44f?style=for-the-badge&logo=windows" alt="Download DynamicDesk for Windows" />
  </a>
</p>

### Installation Steps

1. Visit the [Latest DynamicDesk Release](https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest).
2. Scroll down to the **Assets** section.
3. Download `DynamicDesk-windows-x64.zip` or the ZIP filename provided in the release.
4. Right-click the downloaded ZIP and select **Extract All**.
5. Open the extracted folder.
6. Double-click `DynamicDesk.exe`.
7. Configure your wallpapers using the application interface.

If required, install the [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/).

> **Important:** Extract the complete ZIP before running the application. Do not move only the executable out of the extracted folder unless the release explicitly provides a tested standalone executable.

You do not need Visual Studio to run a correctly packaged release.

## 🛠️ How to Use

### 1. Launch DynamicDesk

Open the extracted application folder and run `DynamicDesk.exe`.

### 2. Access the Application

DynamicDesk runs in the background and provides access through the Windows system tray.

Find the DynamicDesk icon near the Windows clock and use its available controls to access the application.

### 3. Configure Your Wallpapers

Use the settings interface to configure your wallpaper preferences.

- **Live wallpaper:** Choose your supported HTML wallpaper or URL.
- **Static wallpaper:** Select an image to use in static mode.
- **Power behavior:** Configure the available AC and battery options.

### 4. Let DynamicDesk Handle the Switching

Once configured, DynamicDesk monitors your power source and automatically switches between the selected wallpaper modes.

### 5. Explore Live Wallpapers

Try the example wallpaper created for DynamicDesk:

<p align="center">
  <a href="https://owsam22.github.io/pixel-engineer-live-wallpaper/">
    <strong>⚡ Open Pixel Engineer Live Wallpaper ↗</strong>
  </a>
  <br /><br />
  <a href="https://owsam22.github.io/pixel-engineer-live-wallpaper/">
    https://owsam22.github.io/pixel-engineer-live-wallpaper/
  </a>
</p>

This is an example of the kind of web-based wallpaper experience DynamicDesk can display.

## 💻 System Requirements

- Compatible 64-bit Windows system.
- Microsoft Edge WebView2 Runtime for HTML wallpaper rendering.
- .NET runtime is included when using the self-contained release.
- Internet connection for URL-based wallpapers.
- A supported image or HTML wallpaper.

Refer to the individual release notes for version-specific requirements.

## 🧰 Built With

| Technology | Purpose |
|---|---|
| C# | Application logic |
| .NET | Application framework |
| Windows Forms | Desktop application interface |
| Microsoft Edge WebView2 | HTML wallpaper rendering |
| Windows APIs | Power monitoring and Windows integration |

## 🔄 Updates

To update DynamicDesk:

1. Visit the [GitHub Releases page](https://github.com/owsam22/dynamic-wallpaper-engine/releases).
2. Download the latest release.
3. Extract the new version into a separate folder.
4. Run the updated executable.

Keep your personal wallpaper files and any settings you want to preserve before replacing an older version.

Always check the release notes for changes and version-specific instructions.

## 🛠️ Troubleshooting

### Application does not start

- Extract the complete ZIP before running the executable.
- Verify that you downloaded the correct Windows x64 release.
- Check whether Windows has blocked the downloaded file.
- Review any error messages and application logs.

### Live wallpapers do not render

- Install or repair the Microsoft Edge WebView2 Runtime.
- Check your internet connection for URL-based wallpapers.
- Verify that the HTML wallpaper and its required assets are accessible.

### Wallpaper switching does not work

- Check the available power-state settings.
- Verify that automation is enabled.
- Test while connected to AC power and while running on battery.
- Use the simulation mode to help diagnose switching behavior.

### Windows displays a security warning

- Verify that the installer or archive came from the official GitHub repository.
- Review the release and its source code before deciding whether to run it.

## 🐛 Report Issues and Contribute

Found a bug or have an idea for improvement?

- [Report a bug](https://github.com/owsam22/dynamic-wallpaper-engine/issues/new)
- [Browse existing issues](https://github.com/owsam22/dynamic-wallpaper-engine/issues)
- [View all releases](https://github.com/owsam22/dynamic-wallpaper-engine/releases)
- [Explore the source code](https://github.com/owsam22/dynamic-wallpaper-engine)

When reporting a bug, please include:

- Windows version.
- DynamicDesk version.
- Steps to reproduce the issue.
- Relevant error messages or logs.

Contributions, suggestions, and constructive feedback are welcome.

## 👨‍💻 About the Developer

DynamicDesk is independently developed by **Samarpan Jayswal (owsam22)**.

<p align="center">
  <strong>Built with curiosity, code, and a desire to make everyday desktop experiences better.</strong>
  <br /><br />
  <a href="https://portfolio.owsam22.com/">Portfolio</a>
  &nbsp; · &nbsp;
  <a href="https://owsam22.com/">Website</a>
  &nbsp; · &nbsp;
  <a href="https://github.com/owsam22">GitHub</a>
  &nbsp; · &nbsp;
  <a href="mailto:contact@owsam22.com">Contact</a>
</p>

---

<p align="center">
  <strong>⚡ DynamicDesk</strong>
  <br />
  Make your desktop adapt to the way you use your PC.
  <br /><br />
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest">Download Latest Version</a>
  &nbsp; · &nbsp;
  <a href="https://portfolio.owsam22.com/">Developer Portfolio</a>
  <br /><br />
  <sub>Developed by <a href="https://github.com/owsam22">owsam22</a> · Current Version v1.1.0</sub>
</p>