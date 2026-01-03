# 🎯 Compilation de l'Installateur - Guide Rapide

## ⭐ NOUVEAU: Script de Release Complet

Un seul script pour créer une nouvelle version! **Create-Release.ps1** automatise tout le processus:

```powershell
# Créer une nouvelle version patch (défaut)
.\Create-Release.ps1

# Créer une nouvelle version minor (nouvelles fonctionnalités)
.\Create-Release.ps1 -VersionType minor

# Créer une nouvelle version major (ruptures majeures)
.\Create-Release.ps1 -VersionType major
```

**Ce que fait le script:**
1. ✅ Incrémente le numéro de version (patch/minor/major)
2. ✅ Synchronise la version avec Inno Setup
3. ✅ Publie l'application
4. ✅ Compile l'installateur
5. ✅ Affiche un résumé détaillé

**Résultat:** `publish/installer/CharacterManager-Setup.exe`

---

## ✅ Solution pour la Commande `iscc`

Le problème : `iscc CharacterManager.iss` ne fonctionnait pas car le chemin vers le compilateur Inno Setup n'était pas accessible directement.

**Solution**: J'ai créé un script PowerShell qui trouve automatiquement `iscc.exe` et le compile.

---

## 🚀 Comment Compiler l'Installateur

### Option 1: Script PowerShell (Recommandé)
```powershell
.\Build-Installer.ps1
```

Le script :
- ✅ Trouve automatiquement Inno Setup
- ✅ Compile le fichier `.iss`
- ✅ Génère l'exe dans `publish\installer\`
- ✅ Affiche le chemin d'accès

### Option 2: Manuellement (Chemin Complet)
```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "CharacterManager.iss"
```

### Option 3: Ajouter au PATH
Si vous voulez que `iscc` fonctionne partout:
1. **Ouvrir**: Settings → System → Advanced System Settings
2. **Cliquer**: Environment Variables
3. **Modifier**: `PATH`
4. **Ajouter**: `C:\Program Files (x86)\Inno Setup 6`
5. **Redémarrer** PowerShell/Terminal

Après ça, vous pouvez utiliser: `iscc CharacterManager.iss`

---

## 📦 Résultat

L'installateur a été généré avec succès:

```
📁 publish/installer/
└── CharacterManager-Setup.exe (76 MB)
```

---

## 🧪 Tester l'Installateur

```powershell
# Exécuter l'installateur
.\publish\installer\CharacterManager-Setup.exe
```

Ou cliquez simplement deux fois sur le fichier.

---

## 📝 Fichiers Créés

Pour faciliter la compilation:

1. **Build-Installer.ps1** - Script PowerShell principal
   - Utilise le chemin complet vers `iscc.exe`
   - Détecte automatiquement Inno Setup 5 ou 6
   - Affiche des messages clairs

2. **Compile-Installer.bat** - Alternative Batch
   - Même logique en batch
   - Pour ceux qui préfèrent cmd

---

## ✨ Maintenant à Faire

```powershell
# 1. Compiler avec le script
.\Build-Installer.ps1

# 2. Tester l'installation
.\publish\installer\CharacterManager-Setup.exe

# 3. Suivre l'assistant d'installation

# 4. Lancer l'application depuis le menu Démarrer
```

---

**C'est fait !** L'installateur est prêt pour la distribution. 🎉

---

## 🔄 Workflow Complet de Release

### Approche Simple: Un Seul Script
```powershell
# Depuis la racine du projet
.\Create-Release.ps1 -VersionType minor
```

Le script automatise:
1. **Increment-Version.ps1** → Incrémente version
2. **Sync-InnoSetupVersion.ps1** → Synchronise Inno Setup
3. **publish.ps1** → Publie l'app
4. **Build-Installer.ps1** → Compile installateur

### Approche Manuelle: Étape par Étape

Si vous voulez plus de contrôle:

```powershell
# 1. Incrémenter la version
.\Increment-Version.ps1 minor

# 2. Synchroniser Inno Setup
.\Sync-InnoSetupVersion.ps1

# 3. Publier
.\publish.ps1

# 4. Compiler installateur
.\Build-Installer.ps1
```

---

## 📊 Structure des Versions

```
MAJOR.MINOR.PATCH

Exemples:
- patch:  0.12.0 → 0.12.1  (corrections)
- minor:  0.12.0 → 0.13.0  (nouvelles fonctionnalités)
- major:  0.12.0 → 1.0.0   (ruptures majeures)
```

---

## ✅ Checklist avant Release

- [ ] Tester l'application localement
- [ ] Mettre à jour CHANGELOG.md
- [ ] Valider les tests: `dotnet test`
- [ ] Commiter les changements Git
- [ ] Lancer: `.\Create-Release.ps1`
- [ ] Valider l'installateur généré
- [ ] Tester l'installation complète
- [ ] Créer tag Git: `git tag v0.X.Y`
- [ ] Pousser vers GitHub: `git push --tags`

---

## 🐛 Troubleshooting

### L'installateur ne se compile pas?

```powershell
# Vérifier que Inno Setup est installé
Get-Command iscc -ErrorAction SilentlyContinue

# Chemins possibles:
# C:\Program Files (x86)\Inno Setup 6\ISCC.exe
# C:\Program Files (x86)\Inno Setup 5\ISCC.exe
```

### Erreur de permission?

```powershell
# Autoriser les scripts PowerShell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Version ne s'incrémente pas?

Vérifiez que `CharacterManager\appsettings.json` existe et a la clé `AppInfo.Version`.

---
