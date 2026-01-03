# 🚀 Character Manager - Déploiement et Installation

## 📌 Vue d'ensemble

Ce document décrit les différentes façons de déployer et d'installer **Character Manager** v0.12.1.

---

## 🎯 Choix rapides

| Cas d'usage | Commande | Détails |
|---|---|---|
| **Développement local** | `.\Deploy-Manager.ps1 -Action run` | Lance l'app sur http://localhost:5000 |
| **Préparation Release** | `.\Deploy-Manager.ps1 -Action all` | Build + Tests + Publish + Installer |
| **Test rapide** | `.\Deploy-Local.bat` | Compile et lance directement |
| **Utilisateur final** | `CharacterManager-Setup.exe` | Installateur Windows auto-contenu |
| **Déploiement portable** | Copier le dossier `publish/` | Aucune installation requise |

---

## 🛠️ Scripts de déploiement

### 1️⃣ PowerShell - Deploy-Manager.ps1 (Recommandé)

**Le plus complet et flexible.**

#### Usage:
```powershell
# Compiler la solution
.\Deploy-Manager.ps1 -Action build

# Compiler + Tester
.\Deploy-Manager.ps1 -Action test

# Compiler + Tester + Publier
.\Deploy-Manager.ps1 -Action publish

# Compiler + Tester + Publier + Créer l'installateur
.\Deploy-Manager.ps1 -Action all

# Lancer l'application en développement
.\Deploy-Manager.ps1 -Action run -Port 6000

# Nettoyer tous les fichiers générés
.\Deploy-Manager.ps1 -Action clean
```

#### Avantages:
✅ Vérification automatique des prérequis  
✅ Gestion complète du cycle de déploiement  
✅ Messages détaillés  
✅ Gestion d'erreurs robuste  

---

### 2️⃣ Batch - Deploy-Local.bat (Simple, Windows)

**Pour lancer rapidement l'application en développement.**

#### Usage:
```batch
# Lancer sur le port par défaut (5000)
Deploy-Local.bat

# Lancer sur un port personnalisé
Deploy-Local.bat 6000
```

#### Ce qu'il fait:
1. Compile la solution en `Release`
2. Publie dans le dossier `publish/`
3. Lance `CharacterManager.exe`

---

### 3️⃣ PowerShell - Publish-Setup.ps1 (Publication seule)

**Prépare uniquement la publication pour l'installateur.**

#### Usage:
```powershell
# Publier avec la version par défaut
.\Publish-Setup.ps1

# Publier avec une version personnalisée
.\Publish-Setup.ps1 -Version "0.13.0"
```

#### Ce qu'il fait:
1. Nettoie les anciennes publications
2. Lance `dotnet publish`
3. Crée le dossier `publish/installer/`
4. Affiche les prochaines étapes

---

### 4️⃣ Shell - Deploy-Local.sh (Linux/Mac)

**Version équivalente pour environnements Unix.**

#### Usage:
```bash
chmod +x Deploy-Local.sh
./Deploy-Local.sh
# ou avec port personnalisé
./Deploy-Local.sh 6000
```

---

## 📦 Options d'installation

### Option A: Installateur Windows (Recommandé pour utilisateurs)

```
CharacterManager-Setup.exe
```

**Avantages:**
- ✅ Interface graphique intuitive
- ✅ Installation dans Program Files
- ✅ Création de raccourcis automatiques
- ✅ Support complet de la désinstallation
- ✅ Auto-contenu (.NET inclus)

**Création:**
```powershell
.\Deploy-Manager.ps1 -Action all
```

Puis exécuter l'exe généré dans `publish/installer/`

---

### Option B: Déploiement Portable (Développeurs)

Copier simplement le dossier `publish/` sur toute machine Windows:

```
C:\Apps\CharacterManager\
├── CharacterManager.exe
├── CharacterManager.dll
├── wwwroot/
└── ... autres fichiers
```

Lancer directement: `CharacterManager.exe`

**Avantages:**
- ✅ Aucune installation requise
- ✅ Peut fonctionner depuis une clé USB
- ✅ Aucune dépendance système

---

### Option C: Démarrage en Développement

```powershell
.\Deploy-Manager.ps1 -Action run
```

Ou depuis VS Code: `F5` (avec configuration launch)

---

## 🔄 Cycle de développement typique

```
1. Modifier le code
   ↓
2. .\Deploy-Manager.ps1 -Action test
   (compile + exécute les tests)
   ↓
3. .\Deploy-Manager.ps1 -Action run
   (teste manuellement l'app)
   ↓
4. Répéter jusqu'à satisfaction
   ↓
5. .\Deploy-Manager.ps1 -Action all
   (prépare la release complète)
```

---

## 🏗️ Pipeline de build automatique

Le dossier `scripts/` contient des scripts supplémentaires:

| Script | Usage |
|---|---|
| `Increment-Version.ps1` | Incrémenter la version |
| `Update-ReleaseNotes.ps1` | Mettre à jour les notes de release |
| `Deploy-GoogleCloud.ps1` | Déployer sur Google Cloud |

---

## 📋 Structure après build

```
publish/
├── CharacterManager.exe              (Application)
├── CharacterManager.dll              (Core)
├── CharacterManager.Resources.Interface.dll
├── wwwroot/                          (Assets web)
│   ├── css/
│   ├── i18n/
│   └── ...
├── appsettings.json                  (Configuration)
└── ... autres fichiers .NET

publish/installer/
└── CharacterManager-Setup.exe (Installateur)
```

---

## 🐛 Dépannage

### "Port 5000 déjà utilisé"
```powershell
# Utiliser un port différent
.\Deploy-Manager.ps1 -Action run -Port 6000

# Ou trouver le processus:
netstat -ano | findstr :5000
```

### "Inno Setup non trouvé"
L'installateur n'est pas créé, mais l'app fonctionne en mode portable:
```powershell
# Installer Inno Setup depuis: https://jrsoftware.org/
# Puis réessayer:
.\Deploy-Manager.ps1 -Action installer
```

### "Tests échouent"
```powershell
# Voir le détail complet:
dotnet test CharacterManager.sln -c Release -v detailed
```

---

## 📊 Versions de déploiement

- **Debug** (Développement): Port 5269, logs détaillés
- **Release** (Production): Port 5000, optimisé

---

## 🎁 Pour l'utilisateur final

### Installation

1. Télécharger `CharacterManager-Setup.exe`
2. Double-cliquer pour exécuter
3. Suivre l'assistant
4. L'app se lance automatiquement

### Utilisation

- URL: `http://localhost:5000`
- Dossier d'installation: `C:\Program Files\CharacterManager\`
- Base de données: `C:\Program Files\CharacterManager\charactermanager.db`

### Désinstallation

Via Windows → Paramètres → Applications → Applications installées → Character Manager → Désinstaller

---

## 📚 Voir aussi

- [INSTALLATION_GUIDE.md](./INSTALLATION_GUIDE.md) - Guide détaillé pour utilisateurs
- [VERSION_MANAGEMENT.md](./VERSION_MANAGEMENT.md) - Gestion des versions
- [docs/RELEASE_NOTES.md](./docs/RELEASE_NOTES.md) - Notes de release

---

## ✅ Checklist avant release

- [ ] Tous les tests passent: `.\Deploy-Manager.ps1 -Action test`
- [ ] Version mise à jour dans `CharacterManager.csproj`
- [ ] Version mise à jour dans `CharacterManager.iss`
- [ ] Notes de release mises à jour
- [ ] Changelog complété
- [ ] Pas de fichiers temporaires/secrets committés
- [ ] Installer testé: `CharacterManager-Setup.exe`
- [ ] Application portable testée
- [ ] Base de données se crée correctement au premier lancement
- [ ] Pas de logs d'erreur en mode Release

---

**Dernière mise à jour**: v0.12.1 - 2026-01-03
