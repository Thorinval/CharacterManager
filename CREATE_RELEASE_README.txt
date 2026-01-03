# 🚀 QUICK START - Create Release

Créer une nouvelle version en une commande:

```powershell
.\Create-Release.ps1 [-VersionType <patch|minor|major>]
```

## Examples

```powershell
# Patch release (défaut) - corrections
.\Create-Release.ps1

# Minor release - nouvelles fonctionnalités
.\Create-Release.ps1 -VersionType minor

# Major release - changements majeurs
.\Create-Release.ps1 -VersionType major
```

## Ce que ça fait

Le script automatise automatiquement:

1. ✅ Incrémente le numéro de version
2. ✅ Synchronise avec Inno Setup
3. ✅ Publie l'application
4. ✅ Compile l'installateur

## Résultat

```
publish/installer/CharacterManager-Setup.exe
```

## Documentation complète

Voir: `docs/CREATE_RELEASE.md`
