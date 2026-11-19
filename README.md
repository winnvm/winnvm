# WinNvm

[![.NET Build](https://github.com/winnvm/winnvm/actions/workflows/build.yml/badge.svg)](https://github.com/winnvm/winnvm/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**WinNvm** is a lightweight, high-performance Node.js Version Manager for Windows built with modern **.NET 10**. It enables seamless installation, listing, switching, and removal of Node.js versions without requiring administrative privileges.

Release binaries are published as **self-contained, single-file executables** with native support for both **x64** and **ARM64** architectures—no separate .NET runtime installation required.

---

## Features

- **Self-Contained & Zero Dependencies:** Packaged as a single `.exe` containing the required runtime libraries.
- **Multi-Architecture Support:** Native builds for Windows `x64` and `ARM64` (e.g., Surface Pro, Snapdragon X Elite devices). Node.js downloads automatically match host architecture.
- **Integrity Verification:** Verifies official SHA-256 checksums (`SHASUMS256.txt`) prior to extraction.
- **Safe Directory Junctions:** Uses Windows directory junctions (`mklink /J`) for instantaneous switching without touching existing system Node configurations.
- **Active Version Protection:** Safeguards the currently active version from accidental removal.
- **Flexible CLI Parser:** Supports `-`, `--`, and `/` option switches, inline arguments (`--install=22.1.0`, `-i:22.1.0`), and optional `v` version prefixes.
- **WiX v5 MSI Installers:** Interactive per-user installation with customizable `NVM_HOME` and `NVM_SYMLINK` paths and automatic environment variable configuration.

---

## Quick Start

### Installation

#### Option 1: MSI Installer (Recommended)
Download the `.msi` package for your architecture (`win-x64` or `win-arm64`) from [Releases](https://github.com/winnvm/winnvm/releases). The installer automatically configures:
- `NVM_HOME` (`%LocalAppData%\WinNvm` by default)
- `NVM_SYMLINK` (`%LocalAppData%\nodejs` by default)
- User `PATH` updates for both `winnvm` and the active Node symlink

#### Option 2: Portable ZIP / Standalone Executable
1. Download `WinNvm-<version>-<rid>.zip` or standalone `.exe` from [Releases](https://github.com/winnvm/winnvm/releases).
2. Extract or move `WinNvm.exe` to a directory on your system.
3. Configure user environment variables:
   - `NVM_HOME`: Path where Node.js versions are downloaded (e.g. `%LocalAppData%\WinNvm`)
   - `NVM_SYMLINK`: Path for the active Node.js junction link (e.g. `%LocalAppData%\nodejs`)
   - Append both the `WinNvm.exe` folder and `%NVM_SYMLINK%` to your `PATH`.

---

## CLI Usage

```text
Usage: winnvm [OPTIONS]

Options:
    -i, --install <version>    Install a new version of Node.js
    -l, --list                 List all installed versions of Node.js
    -u, --use <version>        Switch to the specified version of Node.js
    -r, --remove <version>     Uninstall a version of Node.js
    -h, --help                 Display help message
    -v, --version              Display WinNvm version
```

### Examples

```cmd
:: Install Node.js v22.11.0
winnvm --install 22.11.0

:: List installed versions (* indicates the currently active version)
winnvm --list

:: Switch active Node.js version
winnvm --use 22.11.0

:: Remove a version
winnvm --remove 20.10.0

:: Check version
winnvm -v
```

### Configuration (`.winnvmrc`)

You can place a `.winnvmrc` JSON file in your current working directory or user home directory (`%USERPROFILE%`) to configure custom download mirrors:

```json
{
  "NodeMirror": "https://nodejs.org/dist/"
}
```

---

## Build Instructions

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or later)
- Git
- For MSI installer builds: Windows OS and WiX v5 toolset (restored automatically via NuGet)

### Cloning the Repository

```bash
git clone https://github.com/winnvm/winnvm.git
cd winnvm
```

### Standard Build

Build the solution or project in Release configuration:

```bash
# Build the entire solution
dotnet build WinNvm.sln -c Release

# Or build the CLI project directly
dotnet build WinNvm.csproj -c Release
```

### Running Locally

```bash
dotnet run --project WinNvm.csproj -- --help
```

---

## Cross-Compilation Instructions

Because WinNvm is built on modern .NET, you can **cross-compile** and produce target Windows binaries (`win-x64` and `win-arm64`) from **any platform** (Windows, macOS, or Linux).

### Single-File Self-Contained Publishing

Self-contained publishing bundles the .NET runtime and libraries directly into the single executable so end users do not need .NET installed on their system.

#### 1. Cross-Compiling for Windows x64 (`win-x64`)

```bash
dotnet publish WinNvm.csproj \
  -c Release \
  -r win-x64 \
  -p:PublishSingleFile=true \
  --self-contained \
  -o ./publish/win-x64
```

On Windows Command Prompt / PowerShell:
```powershell
dotnet publish WinNvm.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained -o ./publish/win-x64
```

Output: `./publish/win-x64/WinNvm.exe`

#### 2. Cross-Compiling for Windows ARM64 (`win-arm64`)

```bash
dotnet publish WinNvm.csproj \
  -c Release \
  -r win-arm64 \
  -p:PublishSingleFile=true \
  --self-contained \
  -o ./publish/win-arm64
```

On Windows Command Prompt / PowerShell:
```powershell
dotnet publish WinNvm.csproj -c Release -r win-arm64 -p:PublishSingleFile=true --self-contained -o ./publish/win-arm64
```

Output: `./publish/win-arm64/WinNvm.exe`

### Publish Arguments Explained

| Flag / Property | Description |
|---|---|
| `-c Release` | Compiles using the optimized Release configuration with compiler optimizations enabled. |
| `-r <RID>` | Specifies target Runtime Identifier: `win-x64` for 64-bit AMD/Intel or `win-arm64` for 64-bit ARM. |
| `-p:PublishSingleFile=true` | Packages the application and all dependencies into a single binary (`WinNvm.exe`). |
| `--self-contained` | Embeds the .NET runtime into the executable (zero prerequisite runtime dependencies for users). |
| `-p:EnableCompressionInSingleFile=true` | Built into `WinNvm.csproj` to compress assemblies inside the single-file package. |
| `-o <directory>` | Specifies the output folder for the published binaries. |

### Framework-Dependent Build (Optional)

If you prefer smaller executables and expect users to have the .NET 10 Desktop/Core runtime already installed:

```bash
dotnet publish WinNvm.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -o ./publish/win-x64-fx
```

---

## Building the MSI Installers (WiX v5)

Building Windows Installer (`.msi`) packages requires a Windows host with .NET SDK. The project utilizes WiX v5 via `WixToolset.Sdk`.

### 1. Publish Binaries First

Generate the published binaries for the desired architecture:

```powershell
dotnet publish WinNvm.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained -o ./publish/win-x64
dotnet publish WinNvm.csproj -c Release -r win-arm64 -p:PublishSingleFile=true --self-contained -o ./publish/win-arm64
```

### 2. Build the Installer Project

```powershell
# Resolve absolute path to publish directory
$root = (Get-Location).Path

# Build x64 MSI
dotnet build installer/WinNvm.Installer.wixproj -c Release `
  -p:InstallerPlatform=x64 `
  -p:Version=2.0.0.0 `
  -p:PublishDir="$root\publish\win-x64" `
  -o ./installer-out/win-x64

# Build ARM64 MSI
dotnet build installer/WinNvm.Installer.wixproj -c Release `
  -p:InstallerPlatform=arm64 `
  -p:Version=2.0.0.0 `
  -p:PublishDir="$root\publish\win-arm64" `
  -o ./installer-out/win-arm64
```

Generated packages:
- `./installer-out/win-x64/WinNvm.msi`
- `./installer-out/win-arm64/WinNvm.msi`

---

## CI / CD Workflows

Automated build and release workflows are defined under [`.github/workflows`](.github/workflows):
- `build.yml`: Validates standard solution builds, single-file publish, and MSI generation on Windows runners for every pull request and push.
- `release.yaml`: Triggered on tag push (`v*`). Builds self-contained single-file binaries and MSI packages for `win-x64` and `win-arm64`, packages `.zip` archives, auto-generates changelogs, and creates GitHub releases.

---

## License

This project is licensed under the [MIT License](LICENSE).
