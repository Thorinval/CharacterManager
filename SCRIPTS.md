# Scripts Organization Guide

## Overview
All PowerShell scripts have been centralized in the `scripts/` folder for better organization and maintainability.

## Quick Start

### Create a New Release
From the project root directory, run:

```powershell
powershell -ExecutionPolicy Bypass -File Create-Release.ps1 -VersionType patch
```

Or with different version types:
```powershell
# Minor version increment
powershell -ExecutionPolicy Bypass -File Create-Release.ps1 -VersionType minor

# Major version increment
powershell -ExecutionPolicy Bypass -File Create-Release.ps1 -VersionType major
```

## Scripts Location

All release and deployment scripts are now located in the `scripts/` folder:

### Release Management
- **scripts/Create-Release.ps1** - Master orchestration script for complete release (wrapper at root)
  - Calls: Sync-InnoSetupVersion → Increment-Version → publish → Build-Installer
  - Generates installer and publication archive automatically

- **scripts/Increment-Version.ps1** - Updates version in appsettings.json and .csproj files
  - Takes parameter: `major`, `minor`, or `patch`
  - Commits changes to git automatically

- **scripts/Sync-InnoSetupVersion.ps1** - Synchronizes version from appsettings.json to CharacterManager.iss

- **scripts/publish.ps1** - Publishes the application with dotnet publish
  - Outputs to: `publish/` folder
  - Supports custom configuration and runtime parameters

- **scripts/Build-Installer.ps1** - Compiles Inno Setup installer
  - Creates: `publish/installer/CharacterManager-X.X.X-Setup.exe`
  - Requires: Inno Setup 6 installed on system

### Testing & Validation
- **scripts/Test-Release.ps1** - Validates release process
- **scripts/Test-Release-Final.ps1** - Final comprehensive testing
- **scripts/check-prerequisites.ps1** - Checks system requirements

### Build & Compilation
- **scripts/Compile-Installer.ps1** - Compiles installer (legacy)
- **scripts/Build-Installer.ps1** - Modern installer builder (recommended)

### Deployment
- **scripts/Deploy-Manager.ps1** - Deploy to manager server
- **scripts/Deploy-GoogleCloud.ps1** - Deploy to Google Cloud Platform
- **scripts/Publish-Setup.ps1** - Setup publication

### Utilities
- **scripts/Create-Icon.ps1** - Generate application icons
- **scripts/Migrate-PersonnageImages.ps1** - Migrate character images
- **scripts/Update-ReleaseNotes.ps1** - Update release notes documentation
- **scripts/SyncReleaseNotesVersion.ps1** - Sync version in release notes

## Path Handling

All scripts have been updated to properly handle paths after relocation:

```powershell
# Scripts determine their own location and calculate root directory
$scriptDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent -Path $scriptDir

# Then use absolute paths
$appSettingsPath = Join-Path $rootDir "CharacterManager\appsettings.json"
$csprojPath = Join-Path $rootDir "CharacterManager\CharacterManager.csproj"
```

## Complete Release Workflow

The `Create-Release.ps1` script automates this 4-step process:

1. **[1/4] Sync Version** - Updates CharacterManager.iss with current version
2. **[2/4] Increment Version** - Bumps version in appsettings.json and commits to git
3. **[3/4] Publish** - Builds and publishes application to `publish/` folder
4. **[4/4] Build Installer** - Compiles Inno Setup installer to `publish/installer/`

## Requirements

- .NET 8 SDK
- PowerShell 5.1+
- Inno Setup 6 (for installer creation)
- Git (for version commits)

## Troubleshooting

### "Cannot load script - execution of scripts is disabled"
Use the `-ExecutionPolicy Bypass` flag:
```powershell
powershell -ExecutionPolicy Bypass -File Create-Release.ps1
```

### "Inno Setup not found"
Install Inno Setup 6 from: https://jrsoftware.org/isdl.php

### "CharacterManager.csproj not found"
Make sure you run scripts from the project root directory, not from the scripts folder.

### Installer not generated
Check that:
1. publish/appsettings.json exists
2. publish/CharacterManager.exe exists
3. publish/installer/ directory was created
4. Inno Setup compilation completed successfully

## Direct Script Usage

You can also call individual scripts directly from the scripts/ folder:

```powershell
cd scripts
powershell -ExecutionPolicy Bypass -File .\Increment-Version.ps1 -Type major
powershell -ExecutionPolicy Bypass -File .\publish.ps1
powershell -ExecutionPolicy Bypass -File .\Build-Installer.ps1
```

## Important Notes

- ✅ All scripts properly handle paths after relocation to scripts/ folder
- ✅ Create-Release.ps1 wrapper available at root for convenience
- ✅ Installer successfully generated at: `publish/installer/CharacterManager-X.X.X-Setup.exe`
- ✅ All sub-scripts use absolute paths via $rootDir variable

---
For detailed information about the complete release process, see [CREATE_RELEASE.md](docs/CREATE_RELEASE.md).
