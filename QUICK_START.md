# 🚀 Character Manager - Quick Start Guide

## ⚡ 30-Second Setup

### Windows - Installateur (Recommandé)
```batch
1. Double-click: CharacterManager-0.12.0-Setup.exe
2. Follow installer wizard
3. Click "Launch" at the end
4. Open browser to: http://localhost:5000
```

### Windows - Portable (Sans installation)
```batch
1. Extract: publish/ folder
2. Run: CharacterManager.exe
3. Open browser to: http://localhost:5000
```

### Linux / macOS
```bash
chmod +x Deploy-Local.sh
./Deploy-Local.sh
# Or specify custom port:
./Deploy-Local.sh 6000
```

---

## 🎮 First Launch

### What to expect:
1. ✅ Application window opens
2. ✅ Database created (charactermanager.db)
3. ✅ Web interface loads on http://localhost:5000
4. ✅ All images display correctly
5. ✅ Navigation menu visible

### Default Credentials:
- No login required
- Local database only
- Offline compatible

---

## 🎯 Key Features to Try

1. **Personnages** - Manage characters
2. **Capacités** - View 28 game capacities
3. **Inventaires** - Manage equipment
4. **Historiques** - View history
5. **Classements** - Manage rankings

---

## 📁 Installation Paths

### Windows Installer
```
C:\Program Files\CharacterManager\
├── CharacterManager.exe
├── wwwroot/
├── charactermanager.db
└── ... all dependencies
```

### Windows Portable
```
Any folder you choose:
├── CharacterManager.exe
├── wwwroot/
├── charactermanager.db
└── ... all dependencies
```

### Linux/macOS
```
~/Apps/CharacterManager/
├── CharacterManager
├── wwwroot/
├── charactermanager.db
└── ... all dependencies
```

---

## ⚙️ Configuration

### Change Port
Edit `appsettings.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:6000"
      }
    }
  }
}
```
Restart application.

### Data Location
Database: `charactermanager.db` (same folder as exe)
Backup/Move it anytime to preserve data.

---

## 🆘 Troubleshooting

### Port Already Used
```powershell
# Find process:
netstat -ano | findstr :5000

# Kill process (if needed):
taskkill /PID <PID> /F

# Or change port in appsettings.json
```

### Database Issues
```powershell
# Delete database (will be recreated):
rm charactermanager.db

# Restart application
```

### Missing DLL Error
Solution: Delete old installation, reinstall with latest version.

### Cannot Connect
1. Check port 5000 is open
2. Try: http://127.0.0.1:5000
3. Check Windows Firewall

---

## 🔄 Updates

### Installing New Version
```
Windows Installer:
1. Uninstall current version
2. Run new installer
3. Your data is preserved

Portable:
1. Backup: charactermanager.db
2. Replace all files
3. Restore: charactermanager.db
```

---

## 📊 System Requirements

### Minimum
- Windows 7+ / macOS 10.14+ / Linux (Ubuntu 18.04+)
- 200 MB free disk space
- 512 MB RAM available

### Recommended
- Windows 10+ / macOS 11+ / Ubuntu 20.04+
- 500 MB free disk space
- 2 GB RAM

### No External Dependencies
- ✅ .NET Runtime included
- ✅ Database (SQLite) included
- ✅ All libraries included
- ✅ Works offline

---

## 🎓 Getting Help

### Application Issues
1. Check the Logs (in installation folder)
2. Consult [INSTALLATION_GUIDE.md](./INSTALLATION_GUIDE.md)
3. See [DEPLOYMENT.md](./DEPLOYMENT.md)

### Reporting Bugs
Include:
- Windows/Mac/Linux version
- Application version
- Steps to reproduce
- Screenshot if possible

---

## 🔐 Privacy & Security

- ✅ All data stored locally
- ✅ No internet connection required
- ✅ No telemetry or analytics
- ✅ No registration needed
- ✅ Database not encrypted (but not needed for local use)

---

## 📝 License

See LICENSE file in installation directory.

---

## 🚀 Ready to Go!

👉 **Run**: `CharacterManager.exe` or `Deploy-Local.bat`  
👁️ **Open**: `http://localhost:5000`  
🎉 **Enjoy!**

---

**Version**: 0.12.0  
**Last Updated**: 2025-01-02  
**Status**: Ready for Use ✅
