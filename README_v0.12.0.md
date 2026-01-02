# Character Manager v0.12.0

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![Tests](https://img.shields.io/badge/tests-61%2F61%20passing-brightgreen)]()
[![Version](https://img.shields.io/badge/version-0.12.0-blue)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()

A modern web-based application for managing characters, equipment, capacities, and rankings. Features a resource-embedded architecture with standalone executables and Windows installer support.

## 🌟 Features

### Core Features
- **Personnages (Characters)** - Create and manage game characters
- **Capacités (Capacities)** - 28 game abilities with icons and full CRUD
- **Inventaires (Inventories)** - Manage equipment and items
- **Historiques (History)** - Track character history and changes
- **Classements (Rankings)** - Create and manage rankings

### Technical Features
- ✅ **Resource DLL Architecture** - All images embedded in a dedicated DLL
- ✅ **REST API** - `/api/resources/interface` endpoints for resource serving
- ✅ **PML Import/Export** - Full support for PML format with 6 export types
- ✅ **SQLite Database** - Local, file-based database
- ✅ **Bootstrap 5 UI** - Modern responsive interface
- ✅ **Bootstrap Icons** - 28+ game ability icons
- ✅ **Multi-language** - French and English localization
- ✅ **Offline Support** - Works completely offline

## 🚀 Quick Start

### Option 1: Windows Installer (Recommended)
```batch
1. Download: CharacterManager-Setup.exe
2. Run installer
3. Launch from Start Menu
4. Open: http://localhost:5000
```

### Option 2: Portable (No installation)
```batch
1. Extract publish/ folder
2. Run: CharacterManager.exe
3. Open: http://localhost:5000
```

### Option 3: Development
```powershell
.\Deploy-Manager.ps1 -Action run
# Or simply:
.\Deploy-Local.bat
```

## 📋 Requirements

### For Users
- **Windows 7+** (64-bit) / macOS 10.14+ / Linux (Ubuntu 18.04+)
- 200 MB free disk space
- No additional software needed (Runtime included)

### For Developers
- **.NET 9 SDK**
- **Visual Studio Code** or **Visual Studio 2022**
- **PowerShell 5.1+** (Windows) or **Bash** (Linux/macOS)
- **(Optional)** Inno Setup 6.x for building installers

## 📦 Installation

### For End Users
See [QUICK_START.md](./QUICK_START.md) for 30-second setup.

### For Developers

#### Clone Repository
```bash
git clone https://github.com/Thorinval/CharacterManager.git
cd CharacterManager
```

#### Build & Run
```powershell
# Quick start (development)
.\Deploy-Local.bat

# Or PowerShell (with options)
.\Deploy-Manager.ps1 -Action run -Port 6000
```

#### Create Release Build
```powershell
.\Deploy-Manager.ps1 -Action all
```

## 🏗️ Architecture

### Projects
```
CharacterManager/                    # Main web application
├── Components/                      # Razor components
├── Server/                          # API controllers
├── Models/                          # Data models
└── Services/                        # Business logic

CharacterManager.Resources.Interface/ # Resource DLL
├── Images/                          # 25 embedded images
└── InterfaceResourceManager.cs      # Resource service

CharacterManager.Tests/              # Unit tests (61 tests)
```

### Resource Management
- **25 images** embedded in `CharacterManager.Resources.Interface.dll`
- Served via `/api/resources/interface/{fileName}`
- MIME type detection (png, jpg, gif, webp, svg)
- Zero external file dependencies

### Database
- **SQLite** local database
- **Entity Framework Core** for ORM
- Auto-migrations on startup
- Location: `charactermanager.db`

## 🔧 Deployment

### Development
```powershell
# Run with hot reload
.\Deploy-Manager.ps1 -Action run

# Run on custom port
.\Deploy-Manager.ps1 -Action run -Port 3000
```

### Production (Installer)
```powershell
# Create installer
.\Deploy-Manager.ps1 -Action all

# Result: publish\installer\CharacterManager-Setup.exe
```

### Production (Portable)
```powershell
# Create portable version
.\Publish-Setup.ps1

# Deploy publish/ folder to any location
# Run: CharacterManager.exe
```

## 📊 Available Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `Deploy-Manager.ps1` | Complete deployment | `.\Deploy-Manager.ps1 -Action [build\|test\|publish\|run\|all]` |
| `Deploy-Local.bat` | Quick local run | `.\Deploy-Local.bat [port]` |
| `Deploy-Local.sh` | Unix quick run | `./Deploy-Local.sh [port]` |
| `Publish-Setup.ps1` | Publish only | `.\Publish-Setup.ps1 [-Version "0.13.0"]` |
| `Check-Release.bat` | Pre-release check | `.\Check-Release.bat` |

For detailed information, see [SCRIPTS.md](./SCRIPTS.md).

## 📖 Documentation

- **[QUICK_START.md](./QUICK_START.md)** - 30-second setup guide
- **[INSTALLATION_GUIDE.md](./INSTALLATION_GUIDE.md)** - Complete installation guide
- **[DEPLOYMENT.md](./DEPLOYMENT.md)** - Deployment options and workflows
- **[RELEASE_0.12.0.md](./RELEASE_0.12.0.md)** - v0.12.0 Release notes
- **[SCRIPTS.md](./SCRIPTS.md)** - Available scripts and tools
- **[docs/RELEASE_NOTES.md](./docs/RELEASE_NOTES.md)** - Complete release history
- **[docs/ROADMAP.md](./docs/ROADMAP.md)** - Future roadmap

## 🧪 Testing

### Run Tests
```powershell
.\Deploy-Manager.ps1 -Action test
```

### Test Results
```
61 / 61 tests passing ✅
100% pass rate
Average runtime: 570 ms
```

## 🔐 Security & Privacy

- ✅ All data stored **locally** - No cloud storage
- ✅ **Offline capable** - No internet connection required
- ✅ **No telemetry** - No usage tracking
- ✅ **No registration** - Instant use
- ✅ **Self-contained** - No external dependencies

## 🛠️ Available Commands

### PowerShell
```powershell
# Build
dotnet build -c Release

# Test
dotnet test -c Release

# Run
dotnet run -c Release

# Publish
dotnet publish -c Release --self-contained

# Clean
dotnet clean
```

### Using Scripts
```powershell
# Development
.\Deploy-Local.bat

# Full pipeline
.\Deploy-Manager.ps1 -Action all

# Validation
.\Check-Release.bat
```

## 📈 Version History

### v0.12.0 (Current)
- ✅ Resource DLL architecture
- ✅ 28 Capacities with Bootstrap Icons
- ✅ REST API for resources
- ✅ Windows Installer (Inno Setup)
- ✅ Portable standalone deployment
- ✅ 61/61 tests passing

### Previous Versions
See [docs/RELEASE_NOTES.md](./docs/RELEASE_NOTES.md)

## 🤝 Contributing

### Development Workflow
1. Create feature branch: `git checkout -b feature/my-feature`
2. Make changes
3. Run tests: `.\Deploy-Manager.ps1 -Action test`
4. Commit: `git commit -am "Add my feature"`
5. Push: `git push origin feature/my-feature`
6. Create Pull Request

### Code Standards
- .NET conventions
- C# async/await patterns
- Blazor component best practices
- Entity Framework Core migrations

## 📞 Support

### For Users
1. Check [QUICK_START.md](./QUICK_START.md)
2. See [INSTALLATION_GUIDE.md](./INSTALLATION_GUIDE.md)
3. Review [docs/FAQ.md](./docs/FAQ.md)

### For Developers
1. Check [DEPLOYMENT.md](./DEPLOYMENT.md)
2. See [SCRIPTS.md](./SCRIPTS.md)
3. Review code comments

### Report Issues
- Create GitHub issue with:
  - Application version
  - OS and version
  - Steps to reproduce
  - Screenshots (if applicable)

## 📄 License

This project is licensed under the **MIT License** - see [LICENSE](./LICENSE) file for details.

## 🎉 Acknowledgments

- **Framework**: ASP.NET Core 9.0 with Blazor
- **UI**: Bootstrap 5 + Bootstrap Icons
- **Database**: SQLite + Entity Framework Core
- **Icons**: Bootstrap Icons (28+ game abilities)
- **Localization**: French & English

## 🔗 Links

- **GitHub**: [https://github.com/Thorinval/CharacterManager](https://github.com/Thorinval/CharacterManager)
- **Issues**: [https://github.com/Thorinval/CharacterManager/issues](https://github.com/Thorinval/CharacterManager/issues)
- **.NET**: [https://dotnet.microsoft.com](https://dotnet.microsoft.com)
- **Bootstrap**: [https://getbootstrap.com](https://getbootstrap.com)

---

**Version**: 0.12.0  
**Last Updated**: 2025-01-02  
**Status**: ✅ Production Ready

**Quick Launch**: `.\Deploy-Local.bat` or download [CharacterManager-Setup.exe](./publish/installer/)

