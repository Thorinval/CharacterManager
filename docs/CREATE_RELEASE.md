# 📦 Script de Release - Create-Release.ps1

## ⚡ Utilisation Rapide

```powershell
# Créer une nouvelle version patch (défaut)
.\Create-Release.ps1

# Créer une nouvelle version minor (nouvelles fonctionnalités)
.\Create-Release.ps1 -VersionType minor

# Créer une nouvelle version major (ruptures majeures)
.\Create-Release.ps1 -VersionType major
```

## 🎯 Objectif

Un seul script pour automatiser TOUT le processus de création d'une nouvelle version:

1. ✅ **Increment-Version.ps1** - Incrémente le numéro de version
2. ✅ **Sync-InnoSetupVersion.ps1** - Synchronise la version avec Inno Setup
3. ✅ **publish.ps1** - Publie l'application
4. ✅ **Build-Installer.ps1** - Compile l'installateur Windows

## 📊 Types de Version

```
MAJOR.MINOR.PATCH

Exemples:
- patch:  0.12.0 → 0.12.1  (corrections de bugs)
- minor:  0.12.0 → 0.13.0  (nouvelles fonctionnalités)
- major:  0.12.0 → 1.0.0   (ruptures majeures)
```

### Quand utiliser?

| Type | Cas d'usage | Exemple |
|------|-----------|---------|
| **patch** | Corrections de bugs, optimisations | 0.12.0 → 0.12.1 |
| **minor** | Nouvelles fonctionnalités rétrocompatibles | 0.12.0 → 0.13.0 |
| **major** | Changements majeurs, breaking changes | 0.12.0 → 1.0.0 |

## 📋 Prérequis

- ✅ PowerShell 5.1+ ou PowerShell Core
- ✅ Inno Setup 6 installé
- ✅ .NET SDK 9.0+
- ✅ Git configuré
- ✅ Droits administrateur (optionnel mais recommandé)

## 🚀 Workflow Complet

### Avant de relancer

1. **Tester l'application**
   ```powershell
   dotnet run --project CharacterManager
   ```

2. **Valider les tests**
   ```powershell
   dotnet test
   ```

3. **Vérifier le build**
   ```powershell
   dotnet build
   ```

4. **Mettre à jour CHANGELOG.md**
   - Ajouter les changements pour la nouvelle version
   - Utiliser le format Keep a Changelog

### Lancer la release

```powershell
# Exemple: créer version 0.13.0 (minor)
.\Create-Release.ps1 -VersionType minor
```

### Après la release

1. **Valider les fichiers générés**
   ```
   publish/                    - App publiée
   publish/installer/          - Installateur compilé
   ```

2. **Tester l'installateur**
   ```powershell
   .\publish\installer\CharacterManager-Setup.exe
   ```

3. **Créer un tag Git** (optionnel)
   ```powershell
   git tag v0.13.0
   git push --tags
   ```

## 📂 Résultats

Après exécution réussie:

```
✅ Version incrémentée
   CharacterManager/appsettings.json: Version changée

✅ Inno Setup synchronisé
   CharacterManager.iss: Version mise à jour

✅ Application publiée
   publish/
   ├── bin/
   ├── wwwroot/
   └── ...

✅ Installateur compilé
   publish/installer/
   └── CharacterManager-Setup.exe (~150-200 MB)
```

## 🐛 Troubleshooting

### Le script ne démarre pas?

```powershell
# Vérifier les droits d'exécution
Get-ExecutionPolicy

# Autoriser si nécessaire
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Erreur "iscc not found"?

```powershell
# Vérifier que Inno Setup est installé
ls "C:\Program Files (x86)\Inno Setup 6\"

# Relancer le script
.\Create-Release.ps1
```

### Erreur de compilation?

1. Vérifier la syntaxe C# : `dotnet build`
2. Vérifier les tests : `dotnet test`
3. Vérifier appsettings.json existe
4. Relancer le script

### Erreur lors de la publication?

```powershell
# Nettoyer le build
dotnet clean

# Relancer
.\Create-Release.ps1 -VersionType patch
```

## 💡 Tips & Tricks

### Afficher les informations avant de relancer

```powershell
# Voir la version actuelle
$json = Get-Content .\CharacterManager\appsettings.json | ConvertFrom-Json
$json.AppInfo.Version
```

### Automatiser avec Task Scheduler (Windows)

Créer une tâche planifiée pour relancer automatiquement à une heure donnée:

```powershell
# Dans Task Scheduler:
# Déclencheur: À 22:00 le dimanche
# Action: powershell.exe -ExecutionPolicy Bypass -File Create-Release.ps1 -VersionType patch
```

### CI/CD avec GitHub Actions

```yaml
# .github/workflows/release.yml
name: Release
on:
  schedule:
    - cron: '0 22 * * 0'  # Dimanche 22:00

jobs:
  release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v2
      - name: Create Release
        run: .\Create-Release.ps1 -VersionType patch
```

## 📚 Fichiers Associés

| Fichier | Description |
|---------|-------------|
| **Create-Release.ps1** | Script principal (ce fichier) |
| **Increment-Version.ps1** | Incrémente la version |
| **Sync-InnoSetupVersion.ps1** | Synchronise Inno Setup |
| **publish.ps1** | Publie l'app |
| **Build-Installer.ps1** | Compile installateur |
| **CHANGELOG.md** | Historique des versions |
| **RELEASE_NOTES.md** | Notes de release |

## ✅ Checklist Avant Release

- [ ] Tous les tests passent
- [ ] Build sans erreurs
- [ ] CHANGELOG.md mis à jour
- [ ] Commits Git effectués
- [ ] Code revisualisé
- [ ] Pas de TODOs bloquants
- [ ] Documentation à jour

## 🎉 C'est prêt!

Vous pouvez maintenant créer une nouvelle version en une seule commande:

```powershell
.\Create-Release.ps1
```

L'application sera automatiquement:
1. ✅ Versionnée
2. ✅ Publiée
3. ✅ Packagée avec Inno Setup
4. ✅ Prête pour la distribution

**Bonne release!** 🚀
