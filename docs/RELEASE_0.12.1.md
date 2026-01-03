# Character Manager v0.12.1 - Release Notes

**Release Date**: January 3, 2026  
**Version**: 0.12.1  
**Status**: ✅ Stable

## Overview

Character Manager v0.12.1 introduces **centralized management of character images** through a dedicated DLL (`CharacterManager.Resources.Personnages`), replacing the scattered file-based approach with a clean, organized embedded resources architecture.

## 🎯 Major Features

### 1. **Embedded Resources Architecture**
- All character images now packaged in `CharacterManager.Resources.Personnages.dll`
- Images organized by character in nested folders
- Support for up to 4 images per character:
  - Detail image (full-size)
  - Header image (optional)
  - Small portrait (UI thumbnails)
  - Small select (selected state)

### 2. **REST API for Resources**
- New endpoint: `/api/resources/personnages/{personnage}/{fichier}`
- Efficient image delivery with HTTP caching (1 hour)
- Direct streaming from embedded resources
- Examples:
  - `GET /api/resources/personnages/Alexa/alexa_small_portrait.png`
  - `GET /api/resources/personnages/Hunter/hunter_small_select.png`
  - `GET /api/resources/personnages/list` (debug endpoint)

### 3. **Smart Resource Management**
- `PersonnageResourceManager` for programmatic access
- `PersonnageImageUrlHelper` for URL generation
- Automatic normalization of character names (PascalCase)
- Support for special characters (hyphens, underscores, apostrophes)

## 📊 Statistics

- **126 character images** migrated from file system
- **86 unique characters** identified and organized
- **4+ images per character** (up to 4 types)
- **~130 MB** total images embedded in DLL
- **100% compatibility** with v0.12.0

## ✨ Improvements

### Performance
- Compiled resources = faster startup
- No file system I/O for image retrieval
- HTTP caching reduces bandwidth
- Streaming reduces memory footprint

### Organization
- Clear folder structure per character
- Easy to add/update/remove images
- Self-documenting file layout
- Supports future content types

### Maintainability
- Single DLL for all character resources
- Easier deployment (fewer files to copy)
- Better version control integration
- Reduced wwwroot folder size

## 🔄 Migration

### Automatic Migration Script
A PowerShell script was provided to migrate existing images:

```powershell
.\scripts\Migrate-PersonnageImages.ps1 -WhatIf  # Preview changes
.\scripts\Migrate-PersonnageImages.ps1          # Execute migration
```

### Results
- ✅ 126 files successfully migrated
- ✅ All files grouped by character
- ✅ Proper naming conventions applied
- ✅ No data loss

## 🛠 Technical Details

### New Projects
- `CharacterManager.Resources.Personnages` - Embedded resources library
- `CharacterManager.Resources.Personnages.Tests` - Resource validation tests

### New Services
- `PersonnageResourcesController` - API endpoint implementation
- `PersonnageResourceManager` - Resource access helper
- `PersonnageImageUrlHelper` - URL generation utility

### Modified Files
- `Personnage.cs` - Updated to use new resource URLs
- `PersonnageService.cs` - Uses helper for URL generation
- `AppConstants.cs` - New path constants for resources API

## 📋 Testing

All functionality has been verified:

✅ **126 resources** successfully embedded in DLL  
✅ **Resource retrieval** works correctly  
✅ **Image existence** can be verified  
✅ **URL generation** produces correct paths  
✅ **Character grouping** functions properly  

## 🚀 Installation & Upgrade

### New Installation
1. Download `CharacterManager-0.12.1-Setup.exe`
2. Run installer
3. Application includes all character images

### Upgrade from v0.12.0
1. Backup existing installation
2. Run `CharacterManager-0.12.1-Setup.exe`
3. Database compatible - no migration needed
4. All images automatically available

## 📝 Breaking Changes

**None** - Fully backward compatible with v0.12.0

## 🔮 Future Enhancements

### v0.12.2
- Support for adult content folder structure
- Image metadata management
- Character image administration UI

### v0.13.0
- Video support (MP4, WebM)
- Audio resources (character voices)
- Costume variants
- Rich metadata system

## 🙏 Acknowledgments

This release modernizes the resource management system, providing a solid foundation for future enhancements and improvements to the Character Manager application.

---

**Download**: [CharacterManager-0.12.1-Setup.exe](https://github.com/Thorinval/CharacterManager/releases)  
**GitHub**: https://github.com/Thorinval/CharacterManager  
**Documentation**: See [docs/](../docs/) folder for detailed information
