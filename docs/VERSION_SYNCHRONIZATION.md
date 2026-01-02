# Version Synchronization Automation

## Overview

Starting with v0.12.1, the project includes automated version synchronization between the application configuration and the installer setup file.

## How It Works

### Source of Truth
- **Version Source**: `CharacterManager/appsettings.json` (`AppInfo.Version`)
- This is the single source of truth for the application version

### Automated Synchronization
The version from `appsettings.json` is automatically synchronized to `CharacterManager.iss` by the `Sync-InnoSetupVersion.ps1` script.

**Script Location**: `scripts/Sync-InnoSetupVersion.ps1`

**Updated Files**:
- `AppVersion` in CharacterManager.iss
- `OutputBaseFilename` in CharacterManager.iss (setup filename)

## Usage

### Manual Execution
```powershell
cd d:\Devs\CharacterManager
.\Sync-InnoSetupVersion.ps1
```

Or with execution policy bypass:
```powershell
powershell -ExecutionPolicy Bypass -File .\Sync-InnoSetupVersion.ps1
```

### Automated Workflow
1. Update version in `CharacterManager/appsettings.json`:
   ```json
   "AppInfo": {
     "Version": "0.12.2"
   }
   ```

2. Run the sync script:
   ```powershell
   .\Sync-InnoSetupVersion.ps1
   ```

3. Build and deploy:
   ```powershell
   dotnet publish -c Release
   ```

## Example Output

```
=== Synchronizing Inno Setup Version ===

Version from appsettings.json: 0.12.1

Current version in CharacterManager.iss: 0.12.0

Updated CharacterManager.iss:
  AppVersion: 0.12.0 --> 0.12.1
  OutputBaseFilename: CharacterManager-0.12.0-Setup --> CharacterManager-0.12.1-Setup

Synchronization complete!
```

## Benefits

- ✅ Single source of truth for version information
- ✅ Prevents version mismatches between app and installer
- ✅ Setup.exe filename always matches the application version
- ✅ Automated process reduces manual errors
- ✅ Easy to integrate into CI/CD pipelines

## Integration with Build Process

### Build-Installer.ps1
Add version sync before generating the installer:
```powershell
# Sync version from appsettings.json to Inno Setup
.\Sync-InnoSetupVersion.ps1

# Generate installer
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "CharacterManager.iss"
```

### CI/CD Pipelines
Include the sync step in your build pipeline before calling `iscc.exe`

## Troubleshooting

### Script not executing
If you get "Impossible de charger le fichier", use:
```powershell
powershell -ExecutionPolicy Bypass -File .\Sync-InnoSetupVersion.ps1
```

### Version not updating
1. Verify `appsettings.json` has the correct version in `AppInfo.Version`
2. Check that `CharacterManager.iss` exists in the project root
3. Ensure the script can write to `CharacterManager.iss` (check file permissions)

### Manually fixing versions
If needed, manually edit:
- `AppVersion=0.12.1` in CharacterManager.iss (line 7)
- `OutputBaseFilename=CharacterManager-0.12.1-Setup` in CharacterManager.iss (line 14)

## Version History

- **v0.12.1**: Automated version synchronization introduced
  - `Sync-InnoSetupVersion.ps1` script created
  - Inno Setup now synchronized from application version source
