# 🎯 Compilation de l'Installateur - Guide Rapide

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
└── CharacterManager-0.12.0-Setup.exe (76 MB)
```

---

## 🧪 Tester l'Installateur

```powershell
# Exécuter l'installateur
.\publish\installer\CharacterManager-0.12.0-Setup.exe
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
.\publish\installer\CharacterManager-0.12.0-Setup.exe

# 3. Suivre l'assistant d'installation

# 4. Lancer l'application depuis le menu Démarrer
```

---

**C'est fait !** L'installateur est prêt pour la distribution. 🎉
