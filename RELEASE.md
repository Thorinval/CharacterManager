# Guide de Publication et Déploiement

## 🚀 Démarrage Rapide

### Publier localement

```powershell
# Publication automatique pour Windows
.\publish.ps1

# L'archive ZIP sera créée: CharacterManager-v1.0.0-win-x64.zip
```

### Tester avec Docker

```bash
# Construire l'image
docker build -t character-manager .

# Lancer
docker-compose up -d

# Accéder à: http://localhost:5269
```

## 📦 Créer une Release GitHub

### 1. Préparer la version

Mettez à jour le numéro de version dans `appsettings.json`:

```json
{
  "AppInfo": {
    "Version": "1.0.1"
  }
}
```

### 2. Créer le tag Git

```bash
# Commiter les changements
git add .
git commit -m "Préparer version 1.0.1"

# Créer le tag
git tag -a v1.0.1 -m "Version 1.0.1 - Description des changements"

# Pousser vers GitHub
git push origin main
git push origin v1.0.1
```

### 3. Release automatique

GitHub Actions va automatiquement :
- ✅ Compiler pour Windows x64 et Linux x64
- ✅ Créer les archives ZIP/TAR.GZ
- ✅ Publier la release sur GitHub
- ✅ Construire et publier l'image Docker

### 4. Vérifier la release

Allez sur : https://github.com/Thorinval/CharacterManager/releases

Vous verrez :
- CharacterManager-1.0.1-win-x64.zip
- CharacterManager-1.0.1-linux-x64.tar.gz
- Notes de version générées automatiquement

## 🔄 Système de Mise à Jour Automatique

Une fois déployée, l'application :
1. ✅ Vérifie automatiquement les nouvelles versions au démarrage
2. ✅ Affiche une notification colorée en haut à droite si une mise à jour est disponible
3. ✅ Permet de voir les notes de version
4. ✅ Fournit un lien direct de téléchargement

Configuration requise : Le paramètre `GitHubRepo` doit être configuré dans `appsettings.json` (déjà fait).

## 📋 Checklist avant Release

- [ ] Tester l'application en local
- [ ] Mettre à jour le numéro de version dans `appsettings.json`
- [ ] Rédiger les notes de version
- [ ] Créer et pousser le tag Git
- [ ] Vérifier que GitHub Actions termine avec succès
- [ ] Tester le téléchargement et l'installation depuis GitHub Releases

## 🐳 Utilisation Docker

### Pour les utilisateurs finaux

```bash
# Télécharger et lancer
docker pull ghcr.io/thorinval/charactermanager:latest
docker run -d -p 5269:8080 -v ./data:/app/data ghcr.io/thorinval/charactermanager:latest
```

### Pour le développement

```bash
# Avec docker-compose (recommandé)
docker-compose up -d

# Voir les logs
docker-compose logs -f

# Arrêter
docker-compose down
```

## 🛠️ Dépannage

### GitHub Actions échoue

1. Vérifier les permissions :
   - Settings → Actions → General
   - Cocher "Read and write permissions"

2. Vérifier les secrets :
   - GITHUB_TOKEN est automatique
   - Pas besoin de secrets supplémentaires

### La notification de mise à jour ne s'affiche pas

1. Vérifier `appsettings.json` :
   ```json
   "GitHubRepo": "Thorinval/CharacterManager"
   ```

2. Vérifier la connexion internet de la machine

3. Consulter les logs de l'application

### Docker build échoue

```bash
# Nettoyer et reconstruire
docker-compose down -v
docker system prune -a
docker-compose build --no-cache
docker-compose up -d
```

## 📞 Plus d'informations

Consultez [DEPLOYMENT.md](DEPLOYMENT.md) pour un guide complet de déploiement incluant :
- Installation sur serveurs Windows/Linux
- Configuration de production
- Sauvegarde et restauration
- Monitoring et logs
- Solutions aux problèmes courants
