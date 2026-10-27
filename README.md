# DynamicDesk

<p align="center">
  <strong>A power-aware wallpaper experience for Windows.</strong>
  <br />
  Switch between live HTML wallpapers and static backgrounds based on your PC's power state.
  <br /><br />
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest">
    <img src="https://img.shields.io/github/v/release/owsam22/dynamic-wallpaper-engine?style=for-the-badge&label=Latest%20Release" alt="Latest Release" />
  </a>
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases">
</a>
</p>

<p align="center">
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest"><strong>⬇ Download for Windows</strong></a>
  &nbsp; · &nbsp;
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/issues">Report a Bug</a>
  &nbsp; · &nbsp;
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases">All Releases</a>
</p>

---

> [!WARNING]
> **Early development:** DynamicDesk is experimental software. Some features may be incomplete or unstable. Check the release notes before installing.

## Overview

DynamicDesk is a Windows desktop application designed to adapt your wallpaper experience to your computer's power state.

The goal is simple: enjoy a live HTML wallpaper while connected to power and switch to a static wallpaper when running on battery.

### Features

- **Power-aware switching** — designed to change wallpaper modes based on AC power and battery status.
- **Live HTML wallpapers** — render web-based wallpapers using Microsoft Edge WebView2.
- **Static wallpapers** — use image-based backgrounds in the battery mode.
- **System tray integration** — access the application from the Windows notification area.
- **Settings interface** — configure available application options.
- **Native Windows application** — built with C# and Windows Forms.

*Features and behavior may vary by release. See the current release notes for the latest implementation status.*

## Download and Install

**The easiest way to get started is to download the latest release.**

<p align="center">
  <a href="https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest">
    <img src="https://img.shields.io/badge/Download-DynamicDesk%20for%20Windows-2ea44f?style=for-the-badge&logo=windows" alt="Download DynamicDesk for Windows" />
  </a>
</p>

1. Open the [Latest Release](https://github.com/owsam22/dynamic-wallpaper-engine/releases/latest).
2. Download `DynamicDesk-win-x64.zip` from the **Assets** section.
3. Extract the ZIP file.
4. Run the included DynamicDesk executable.
5. Install the Microsoft Edge WebView2 Runtime if required.

### System Requirements

- Compatible 64-bit Windows system.
- Microsoft Edge WebView2 Runtime for HTML wallpaper rendering.
- No Visual Studio installation is needed to run a correctly packaged release.
- The packaged release may include the .NET runtime, depending on how it was published.

If Windows displays a security warning, verify that the download came from this repository and review the release before proceeding.

## How It Works

DynamicDesk is designed around two wallpaper modes:

| Power source | Intended wallpaper mode |
|---|---|
| AC power connected | Live HTML wallpaper |
| Battery power | Static wallpaper |

The application monitors power-state changes and uses them to determine the appropriate wallpaper mode. Actual behavior depends on the features implemented in the installed release.

## Built With

| Technology | Purpose |
|---|---|
| C# | Application logic |
| .NET | Application framework |
| Windows Forms | Desktop interface |
| Microsoft Edge WebView2 | HTML wallpaper rendering |
| Windows APIs | Operating-system integration |

## Run from Source

Want to explore or contribute to the code?

### Prerequisites

- Git
- .NET SDK compatible with the project's target framework
- A compatible Windows development environment
- WebView2 Runtime for testing HTML wallpaper rendering

### Clone the Repository

```bash
git clone https://github.com/owsam22/dynamic-wallpaper-engine.git
cd dynamic-wallpaper-engine
```

### Restore Dependencies

```bash
dotnet restore
```

### Build the Project

```bash
dotnet build -c Release
```

### Run the Application

```bash
dotnet run
```

Run these commands from the directory containing `DynamicDesk.csproj`. The installed .NET SDK must support the framework declared in that file.

## Project Status

DynamicDesk is an actively developing project. The initial release provides a starting point for testing the application and gathering feedback.

Planned improvements may include more reliable wallpaper transitions, better configuration, improved error handling, and a smoother user experience.

These are development goals, not promises that every feature is already implemented.

## Report Issues

Found a bug or have an idea?

- [Report a bug](https://github.com/owsam22/dynamic-wallpaper-engine/issues/new)
- [Browse existing issues](https://github.com/owsam22/dynamic-wallpaper-engine/issues)
- [View releases](https://github.com/owsam22/dynamic-wallpaper-engine/releases)

When reporting a bug, include your Windows version, steps to reproduce it, and any relevant error messages.

## License

See the repository's `LICENSE` file for the applicable terms. If no license file is present, the source code should not be assumed to be open for unrestricted reuse or redistribution.

---

<p align="center">
  <strong>DynamicDesk</strong>
  <br />
  Built by <a href="https://github.com/owsam22">owsam22</a>
  <br />
  <sub>Making the desktop adapt to the way you use your PC.</sub>
</p>