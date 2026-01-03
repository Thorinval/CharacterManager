# 🛠️ Character Manager - Available Scripts & Tools

## 📋 Table of Contents

1. [Deployment Scripts](#deployment-scripts)
2. [Build & Release Scripts](#build--release-scripts)
3. [Utility Scripts](#utility-scripts)
4. [Documentation](#documentation)

---

## 🚀 Deployment Scripts

### Deploy-Manager.ps1
**Type**: PowerShell (Main Manager)  
**Best for**: Complete deployment workflow  

```powershell
# Development - Run locally
.\Deploy-Manager.ps1 -Action run

# Compilation only
.\Deploy-Manager.ps1 -Action build

# Compile + Test
.\Deploy-Manager.ps1 -Action test

# Compile + Test + Publish
.\Deploy-Manager.ps1 -Action publish

# Full pipeline (build + test + publish + installer)
.\Deploy-Manager.ps1 -Action all

# Clean all build artifacts
.\Deploy-Manager.ps1 -Action clean

# Custom port
.\Deploy-Manager.ps1 -Action run -Port 6000
```

**Features**:
- ✅ Prerequisite checking
- ✅ Detailed error handling
- ✅ Progress indicators
- ✅ Colorized output
- ✅ Multiple actions

---

### Deploy-Local.bat
**Type**: Batch (Windows)  
**Best for**: Quick local development  

```batch
# Run with default port (5000)
Deploy-Local.bat

# Run with custom port
Deploy-Local.bat 6000
```

**Features**:
- ✅ One-command launch
- ✅ Auto-compile
- ✅ Auto-publish
- ✅ Simple output

---

### Deploy-Local.sh
**Type**: Shell Script (Linux/macOS)  
**Best for**: Unix systems  

```bash
chmod +x Deploy-Local.sh

# Run with default port (5000)
./Deploy-Local.sh

# Run with custom port
./Deploy-Local.sh 6000
```

**Features**:
- ✅ Cross-platform
- ✅ Same functionality as .bat
- ✅ Bash compatible

---

## 📦 Build & Release Scripts

### Publish-Setup.ps1
**Type**: PowerShell  
**Best for**: Publishing before installer creation  

```powershell
# Publish with default version
.\Publish-Setup.ps1

# Publish with custom version
.\Publish-Setup.ps1 -Version "0.13.0"

# Publish in Debug mode
.\Publish-Setup.ps1 -Configuration "Debug"
```

**Features**:
- ✅ Cleans old publications
- ✅ Runs dotnet publish
- ✅ Creates installer directory
- ✅ Shows next steps

---

### Increment-Version.ps1
**Type**: PowerShell  
**Best for**: Version bumping  

```powershell
# Increment version automatically
.\Increment-Version.ps1
```

**Features**:
- ✅ Updates .csproj
- ✅ Updates .iss file
- ✅ Git commit (optional)

---

## 🧪 Validation Scripts

### Check-Release.bat
**Type**: Batch  
**Best for**: Pre-release validation  

```batch
Check-Release.bat
```

**Validates**:
- ✅ Project files
- ✅ Build output
- ✅ Test compilation
- ✅ Publication
- ✅ Resources
- ✅ Documentation
- ✅ Deployment scripts

**Output**: PASS/FAIL checklist

---

### Test-Release.ps1
**Type**: PowerShell (advanced)  
**Best for**: Detailed pre-release verification  

```powershell
powershell -ExecutionPolicy Bypass -File Test-Release.ps1 -Verbose
```

---

## 🔧 Utility Scripts

### publish.ps1
**Type**: PowerShell  
**Best for**: Simple publication  

```powershell
.\publish.ps1
```

---

### scripts/Deploy-GoogleCloud.ps1
**Type**: PowerShell  
**Best for**: Google Cloud deployment  

```powershell
.\scripts\Deploy-GoogleCloud.ps1
```

---

### scripts/Update-ReleaseNotes.ps1
**Type**: PowerShell  
**Best for**: Release notes automation  

```powershell
.\scripts\Update-ReleaseNotes.ps1
```

---

### scripts/SyncReleaseNotesVersion.ps1
**Type**: PowerShell  
**Best for**: Version synchronization  

```powershell
.\scripts\SyncReleaseNotesVersion.ps1
```

---

## 📖 Documentation

### Installation & Setup
| Document | Purpose | Audience |
|----------|---------|----------|
| [QUICK_START.md](./QUICK_START.md) | 30-second setup | End users |
| [INSTALLATION_GUIDE.md](./INSTALLATION_GUIDE.md) | Detailed installation | End users |
| [DEPLOYMENT.md](./DEPLOYMENT.md) | Deployment options | Developers |

### Release & Version
| Document | Purpose |
|----------|---------|
| [RELEASE_0.12.0.md](./RELEASE_0.12.0.md) | v0.12.0 Release notes |
| [RELEASE_CHECKLIST.md](./RELEASE_CHECKLIST.md) | Pre-release verification |
| [VERSION_MANAGEMENT.md](./VERSION_MANAGEMENT.md) | Version management |
| [docs/RELEASE_NOTES.md](./docs/RELEASE_NOTES.md) | Complete release history |
| [docs/ROADMAP.md](./docs/ROADMAP.md) | Future roadmap |

---

## 🎯 Common Workflows

### 1. Development Loop
```powershell
# Edit code...
# Then:
.\Deploy-Manager.ps1 -Action test
.\Deploy-Manager.ps1 -Action run
# Test manually at http://localhost:5000
```

### 2. Before Release
```powershell
# Verify everything
.\Check-Release.bat

# Run all checks
.\Deploy-Manager.ps1 -Action all
```

### 3. Create Installer
```powershell
# Publish
.\Publish-Setup.ps1

# Compile installer (requires Inno Setup)
iscc CharacterManager.iss

# Result: publish\installer\CharacterManager-Setup.exe
```

### 4. Quick Local Test
```batch
.\Deploy-Local.bat
# Application starts on http://localhost:5000
```

### 5. Production Deployment
```powershell
# Option 1: Use installer
# Run CharacterManager-Setup.exe

# Option 2: Use portable
# Copy publish/ folder to any location
# Run CharacterManager.exe
```

---

## 📊 Script Comparison

| Script | Type | Speed | Ease | Features |
|--------|------|-------|------|----------|
| Deploy-Manager.ps1 | PowerShell | Medium | ⭐⭐⭐ | Complete |
| Deploy-Local.bat | Batch | Fast | ⭐⭐ | Basic |
| Deploy-Local.sh | Shell | Fast | ⭐⭐ | Basic |
| Publish-Setup.ps1 | PowerShell | Medium | ⭐ | Publish only |
| Check-Release.bat | Batch | Fast | ⭐⭐ | Validation |

---

## 🔗 Related Files

### Configuration
- `appsettings.json` - Application settings
- `appsettings.Development.json` - Development settings
- `CharacterManager.csproj` - Project configuration
- `CharacterManager.sln` - Solution file

### Deployment
- `CharacterManager.iss` - Inno Setup installer script
- `docker/docker-compose.yml` - Docker configuration
- `cloudbuild.yaml` - Cloud Build configuration
- `.github/workflows/` - CI/CD pipelines

### Database
- `CreateLucieHouseTables.sql` - Database initialization
- `Heritage.sql` - Database schema

---

## ✅ Quick Reference

```
DEVELOPMENT:
  .\Deploy-Manager.ps1 -Action run
  
TESTING:
  .\Deploy-Manager.ps1 -Action test
  
RELEASE:
  .\Deploy-Manager.ps1 -Action all
  
VALIDATION:
  .\Check-Release.bat
  
PUBLISH:
  .\Publish-Setup.ps1
  
LOCAL RUN:
  .\Deploy-Local.bat
```

---

## 🆘 Troubleshooting

### Script won't run
```powershell
# Allow script execution:
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser

# Or use:
powershell -ExecutionPolicy Bypass -File script.ps1
```

### PowerShell vs Batch
- Use PowerShell for advanced features
- Use Batch for simple tasks
- Use Shell for Unix systems

### Need help?
See [DEPLOYMENT.md](./DEPLOYMENT.md) or [QUICK_START.md](./QUICK_START.md)

---

**Version**: 0.12.0  
**Last Updated**: 2025-01-02  
**Status**: All scripts tested and ready ✅
