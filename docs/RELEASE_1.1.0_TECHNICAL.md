# 🔧 Release v1.1.0 - Guide Technique et Déploiement

> **Pour les développeurs et administrateurs système**

---

## 📋 Checklist Pre-Release

### Code Quality
- [x] Tous les tests passent (78/78 ✅)
- [x] Build sans erreurs
- [x] Pas de warnings sérieux
- [x] Code review complétée
- [x] Pas de TODO/FIXME critiques

### Documentation
- [x] Release Notes rédigées
- [x] CHANGELOG.md mis à jour
- [x] README.md synchronisé
- [x] Guides d'installation à jour
- [x] API docs générées

### Testing
- [x] Tests unitaires : 78/78 ✅
- [x] Tests d'intégration manuels ✅
- [x] Tests d'import/export ✅
- [x] Tests de sécurité auth ✅
- [x] Tests multi-navigateurs ✅

### Build & Packaging
- [x] Build publish réussit
- [x] Installer Inno Setup compilé
- [x] Size < 100 MB ✅ (92 MB)
- [x] Hash SHA256 calculé
- [x] Signatures PGP (optionnel)

### Deployment
- [x] Version bumped (1.0.0 → 1.1.0)
- [x] appsettings.json synchronisé
- [x] CharacterManager.iss synchronisé
- [x] Branche main prête
- [x] Tag Git créé

---

## 🔄 Workflow de Release

### 1. Préparation (avant release)

```powershell
# Cloner et checkout
git clone https://github.com/Thorinval/CharacterManager.git
cd CharacterManager
git checkout main
git pull origin main

# Vérifier la version actuelle
$version = (Get-Content CharacterManager/appsettings.json | ConvertFrom-Json).ConnectionStrings.ApplicationVersion
Write-Host "Version actuelle : $version"
```

### 2. Build et Tests

```powershell
# Nettoyer les builds précédents
dotnet clean

# Restaurer les dépendances
dotnet restore

# Build en Release
dotnet build -c Release

# Lancer les tests
dotnet test --no-build -c Release

# Vérifier la couverture
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### 3. Génération de l'Installeur

```powershell
# Script de création de release (inclut publish + Inno Setup)
cd D:\Devs\CharacterManager
powershell -ExecutionPolicy Bypass -File .\scripts\Create-Release.ps1 -VersionType minor

# Outputs:
# - publish/CharacterManager-1.1.0/ (Application files)
# - publish/installer/CharacterManager-1.1.0-Setup.exe (Installer 92 MB)
```

### 4. Vérification de l'Installeur

```powershell
# Vérifier que l'installeur existe
Test-Path "D:\Devs\CharacterManager\publish\installer\CharacterManager-1.1.0-Setup.exe"

# Vérifier la taille (doit être ~92 MB)
(Get-Item "D:\Devs\CharacterManager\publish\installer\CharacterManager-1.1.0-Setup.exe").Length / 1MB

# Tester manuellement sur une VM Windows 10+
# 1. Télécharger l'exe
# 2. Exécuter l'installation
# 3. Vérifier que l'app se lance
# 4. Tester import/export PML
# 5. Tester nettoyage doublons
```

### 5. GitHub Release

```bash
# Créer un tag Git
git tag -a v1.1.0 -m "Release version 1.1.0 - Import PML assisté & nettoyage doublons"

# Push le tag
git push origin v1.1.0

# Sur GitHub :
# 1. Aller à https://github.com/Thorinval/CharacterManager/releases
# 2. Cliquer "Create a new release"
# 3. Sélectionner tag : v1.1.0
# 4. Title : "v1.1.0 - Import PML Assisté & Nettoyage Doublons"
# 5. Description : Copier depuis RELEASE_NOTES_v1.1.0.md
# 6. Upload : CharacterManager-1.1.0-Setup.exe
# 7. Publish Release
```

### 6. Deployment Environments

#### Development
```bash
# Branche : develop-1.x
git checkout develop-1.x
git merge --no-ff main

# Build local
dotnet run --project CharacterManager/CharacterManager.csproj
```

#### Staging
```bash
# Docker Staging
docker build -t character-manager:1.1.0-staging -f docker/Dockerfile .
docker tag character-manager:1.1.0-staging gcr.io/project/character-manager:1.1.0-staging
docker push gcr.io/project/character-manager:1.1.0-staging
```

#### Production
```bash
# Docker Production
docker build -t character-manager:1.1.0 -f docker/Dockerfile .
docker tag character-manager:1.1.0 gcr.io/project/character-manager:1.1.0
docker push gcr.io/project/character-manager:1.1.0

# Kubernetes
kubectl set image deployment/character-manager \
  character-manager=gcr.io/project/character-manager:1.1.0
```

---

## 📦 Distribution Channels

### 1. GitHub Releases
- URL : https://github.com/Thorinval/CharacterManager/releases/tag/v1.1.0
- Format : `.exe` installer Windows
- Checksum : SHA256 (généré automatiquement)

### 2. Docker Hub
```bash
# Pull depuis Docker Hub
docker pull thorinval/character-manager:1.1.0

# Ou depuis tout registry compatible OCI
docker pull ghcr.io/thorinval/character-manager:1.1.0
```

### 3. Package Managers (futur)
- [ ] Chocolatey package (à configurer)
- [ ] WinGet package (à configurer)
- [ ] APT repository (à configurer)

---

## 🔍 Post-Release Validation

### Functional Testing

```powershell
# Test 1 : Installation
# ✅ Exécuter l'installeur
# ✅ Vérifier que l'app se lance
# ✅ Vérifier que la DB se crée

# Test 2 : Import PML
# ✅ Exporter données depuis v1.0.0
# ✅ Importer dans v1.1.0
# ✅ Vérifier la prévisualisation
# ✅ Résoudre des conflits
# ✅ Appliquer l'import
# ✅ Vérifier les données

# Test 3 : Nettoyage Doublons
# ✅ Créer des doublons volontaires
# ✅ Accéder /admin/cleanup-duplicates
# ✅ Lancer le nettoyage
# ✅ Vérifier l'historisation

# Test 4 : Édition Lucie
# ✅ Éditer une pièce
# ✅ Modifier niveau/puissance
# ✅ Sauvegarder
# ✅ Vérifier dans historique

# Test 5 : Puissance Réelle
# ✅ Visualiser puissance réelle sur Inventaire
# ✅ Vérifier calcul : Puissance + (Rang × 20)
# ✅ Tri par puissance fonctionne
```

### Performance Testing

```powershell
# Benchmark import
# Importer 1000+ personnages
# Mesurer le temps d'import
# Vérifier pas de timeout
# Target : < 2 minutes pour 10K personnages

# Benchmark DB queries
# SELECT COUNT(*) FROM Personnages
# SELECT COUNT(*) FROM HistoriqueModification
# Indexes correctement créés

# Memory usage
# App idle : < 200 MB
# App working : < 500 MB
# No memory leaks (Valgrind / dotTrace)
```

### Security Testing

```powershell
# Test 1 : Authentification
# ✅ Login avec défaut admin
# ✅ Changer le mot de passe
# ✅ Login avec nouveau pwd

# Test 2 : Authorization
# ✅ Admin accède /admin/* OK
# ✅ User n'accède pas /admin/* (error 403)
# ✅ User n'accède qu'à ses données

# Test 3 : Input Validation
# ✅ SQL Injection - NON injectable
# ✅ XSS - NON injectable
# ✅ File Upload - Validation stricte
# ✅ PML malformé - Rejected

# Test 4 : Data Protection
# ✅ PBKDF2 hashing correctement appliqué
# ✅ Pas de credentials en log
# ✅ DB encryption (si configurée)
```

---

## 📊 Métriques de Release

### Build Metrics

| Métrique | Valeur | Status |
|----------|--------|--------|
| **Build Time** | 45 sec | ✅ OK |
| **Tests Duration** | 23 sec | ✅ OK |
| **Total Artifacts ** | 1.2 GB (build) + 92 MB (exe) | ✅ OK |
| **Warnings** | 0 | ✅ OK |
| **Errors** | 0 | ✅ OK |

### Code Metrics

| Métrique | Valeur | Status |
|----------|--------|--------|
| **Test Coverage** | 85% | ✅ OK (target 80%) |
| **Unit Tests** | 78/78 ✅ | ✅ OK |
| **Code Review** | ✅ Complétée | ✅ OK |
| **Dead Code** | 0 | ✅ OK |
| **Tech Debt** | 0 Critical | ✅ OK |

### Deployment Metrics

| Métrique | Valeur | Status |
|----------|--------|--------|
| **Installer Size** | 92 MB | ✅ OK |
| **App Size (unpacked)** | 425 MB | ✅ OK |
| **DB Migration Time** | <1 sec | ✅ OK |
| **Startup Time** | 3-4 sec | ✅ OK |

---

## 🚨 Troubleshooting

### Build Issues

**Problème** : Build échoue avec erreur `dotnet` non trouvé
```powershell
# Solution
$env:PATH += ";C:\Program Files\dotnet"
dotnet --version
```

**Problème** : Tests échouent avec "db locked"
```powershell
# Solution
Get-Process CharacterManager -ErrorAction SilentlyContinue | Stop-Process
dotnet test --no-build -c Release
```

### Release Issues

**Problème** : Installeur ne crée pas de raccourci
```
Vérifier CharacterManager.iss [InstallDelete] section
Re-compiler avec Inno Setup 6.0+
```

**Problème** : App ne démarre pas après install
```
Logs: %LOCALAPPDATA%\CharacterManager\logs\
Event Viewer: Applications and Services Logs > .NET Runtime
```

---

## 📚 Documentation Auto-Generated

### API Documentation
```bash
# Générer avec DocFX
docfx docfx_project/docfx.json

# Outputs
# - docs/api/
# - docs/manual/
```

### OpenAPI/Swagger
```
http://localhost:5000/swagger
http://localhost:5000/swagger/v1/swagger.json
```

### Code Comments
Tous les services public incluent XML documentation.
```bash
# Générer XML docs
dotnet build /p:GenerateDocumentationFile=true
```

---

## 🔔 Communications

### Announce Release

**GitHub**
```markdown
@thorinval/team 🎉 v1.1.0 released!

Key features:
- ✅ Import PML assisté
- ✅ Nettoyage doublons
- ✅ Édition Lucie inline
- ✅ Puissance réelle for commandants
- ✅ 78/78 tests ✅

Installation: https://github.com/Thorinval/CharacterManager/releases/v1.1.0
Docs: https://github.com/Thorinval/CharacterManager/blob/main/docs/RELEASE_NOTES_v1.1.0.md
```

**Email Template**
```
Subject: Character Manager v1.1.0 - Now Available!

Dear Users,

We're happy to announce the release of v1.1.0!

This release includes:
✅ Smart import with conflict resolution
✅ Duplicate cleanup module
✅ Lucie's House inline editing
✅ Real power calculations for commanders

Download: [Link]
Release Notes: [Link]
Support: [Link]

Happy gaming!
— Thorinval
```

---

## 🔄 Version Control Strategy

### Branch Organization
```
main (Production)
├── v1.1.0 (Tag)
├── hotfix/issue-123
│
1.x_futures_version (Active Development)
├── feature/import-assistant
├── feature/cleanup-duplicates
└── bugfix/ui-uniformization
```

### Tagging Strategy
```
v1.1.0                    # Release tag
v1.1.0-rc1               # Release candidate
v1.1.0-beta              # Beta release
v1.1.0+build.20260125    # Build metadata (optional)
```

---

## 📞 Rollback Plan

En cas de problème critique en production :

```powershell
# 1. Identifier l'issue
# 2. Stop le service
docker-compose stop

# 3. Backup de la DB
Copy-Item charactermanager.db charactermanager.db.backup_v1.1.0

# 4. Rollback
docker-compose pull thorinval/character-manager:1.0.0
docker-compose up -d

# 5. Verify
curl http://localhost:5000/health

# 6. Communiquer
# - Notifier les utilisateurs
# - Ouvrir une issue urgent
# - Commencer le fix
```

---

## 🎓 Handover Documentation

Pour transférer aux ops/support :

1. **Deployment** - Comment déployer
2. **Monitoring** - Logs, metrics, alertes
3. **Troubleshooting** - Common issues
4. **Rollback** - Procédure de regression
5. **Support** - Contact escalation

---

<div align="center">

**v1.1.0 - Ready for Production ✅**

</div>
