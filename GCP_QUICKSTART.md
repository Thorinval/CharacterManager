# Guide de Démarrage Rapide - Google Cloud

> ⚡ Déployez Character Manager sur Google Cloud en 10 minutes

## 📋 Prérequis (5 minutes)

### 1. Créer un compte Google Cloud
- Aller sur https://console.cloud.google.com/
- Créer un projet → `character-manager-prod`
- Activer la facturation (gratuit jusqu'à certains seuils)

### 2. Installer les outils
```powershell
# Windows
# Télécharger et installer Google Cloud SDK
# https://cloud.google.com/sdk/docs/install-windows

# Vérifier l'installation
gcloud --version              # v.x.x.x
dotnet --version              # 9.0+

# Optionnel : Docker Desktop (seulement si build local)
docker --version              # Optionnel
```

**Note** : Docker n'est pas obligatoire ! Google Cloud Build peut construire l'image directement.

### 3. Configurer gcloud
```bash
gcloud init
# Sélectionner le projet: character-manager-prod
# Région: europe-west1 (Belgique/Pays-Bas)

# Vérifier la config
gcloud config list
```

---

## 🚀 Déploiement en 1 Commande (5 minutes)

### Option A : Déploiement Automatisé (Recommandé)

```powershell
# Dans le répertoire du projet
.\scripts\Deploy-GoogleCloud.ps1 `
  -ProjectId "character-manager-prod" `
  -Region "europe-west1" `
  -DeploymentType "CloudRun"

# Le script va:
# ✅ Vérifier les prérequis
# ✅ Configurer le projet GCP
# ✅ Créer l'Artifact Registry
# ✅ Compiler l'application
# ✅ Construire l'image Docker
# ✅ Pousser vers GCP
# ✅ Déployer sur Cloud Run
```

**Résultat** : L'URL de votre application sera affichée à la fin
```
🌐 https://character-manager-xxxxx-ew.a.run.app
```

### Option B : Déploiement Manuel (Étape par étape)

#### 1. Compiler l'application
```bash
dotnet publish CharacterManager/CharacterManager.csproj `
    --configuration Release `
    --output publish
```

#### 2. Créer le projet et les APIs
```bash
# Créer le projet
gcloud projects create character-manager-prod --name="Character Manager"
gcloud config set project character-manager-prod

# Activer les APIs
gcloud services enable run.googleapis.com artifactregistry.googleapis.com
```

#### 3. Configurer Artifact Registry
```bash
gcloud artifacts repositories create character-manager `
  --repository-format=docker `
  --location=europe-west1

gcloud auth configure-docker europe-west1-docker.pkg.dev
```

#### 4. Construire et pousser l'image Docker

**Option 4a : Avec Cloud Build (sans Docker local)**
```bash
# Build directement sur Google Cloud
$PROJECT_ID = "character-manager-prod"

gcloud builds submit --tag europe-west1-docker.pkg.dev/$PROJECT_ID/character-manager/app:latest
```

**Option 4b : Avec Docker local**
```bash
$PROJECT_ID = "character-manager-prod"
$IMAGE = "europe-west1-docker.pkg.dev/$PROJECT_ID/character-manager/app"

docker build -t "$IMAGE:latest" .
docker push "$IMAGE:latest"
```

#### 5. Déployer sur Cloud Run
```bash
gcloud run deploy character-manager `
  --image="$IMAGE:latest" `
  --region=europe-west1 `
  --allow-unauthenticated `
  --memory=512Mi `
  --cpu=1
```

#### 6. Récupérer l'URL
```bash
gcloud run services describe character-manager --region=europe-west1
```

---

## 📱 Accéder à votre Application

### URL Cloud Run (automatique)
```
https://character-manager-xxxxx-ew.a.run.app
```
✅ Accessible de partout avec HTTPS automatique

### Avec Domaine Personnalisé (optionnel)

```bash
# 1. Ajouter le domaine à Cloud Run
gcloud run domain-mappings create `
  --service=character-manager `
  --domain=monapp.com `
  --region=europe-west1

# 2. Chez votre registrar DNS, ajouter:
# Type: CNAME
# Nom: monapp.com
# Valeur: goog-managed-ssl.run.app

# 3. Attendre 5-10 minutes pour le certificat SSL
# 4. Accédez à https://monapp.com
```

---

## 🔒 Sécuriser l'Accès (optionnel)

### Ajouter une Authentification Google

```bash
# Créer un compte de service
gcloud iam service-accounts create character-manager-sa `
  --display-name="Character Manager Service"

# Limiter l'accès à vos utilisateurs
gcloud run services add-iam-policy-binding character-manager `
  --member="user:votremail@gmail.com" `
  --role="roles/run.invoker" `
  --region=europe-west1
```

---

## 📊 Consulter les Logs

```bash
# Voir les erreurs
gcloud logging read --limit 50

# Suivre en temps réel
gcloud logging read --follow

# Filtrer par service
gcloud logging read "resource.labels.service_name=character-manager" --limit 20
```

---

## 📈 Monitoring

### Dashboard Cloud Monitoring

```bash
# Ouvrir automatiquement le dashboard
gcloud console
```

Dashboard inclus :
- 📊 Nombre de requêtes
- ⚠️ Taux d'erreurs
- ⏱️ Temps de réponse
- 🖥️ Utilisation des ressources

### Créer une Alerte

```bash
# Alerte si erreur > 5%
gcloud alpha monitoring policies create `
  --notification-channels=YOUR_CHANNEL_ID `
  --display-name="Character Manager Error Alert"
```

---

## 💰 Vérifier les Coûts

```bash
# Dashboard coûts
gcloud billing accounts list
gcloud billing budgets create --billing-account=YOUR_ACCOUNT
```

**Estimé** pour une petite utilisation :
- Cloud Run : **gratuit** (2M requêtes/mois)
- Cloud Storage : ~$0.50/mois
- **Total : $0-5 USD/mois**

---

## ⚠️ Problèmes Courants

### Application redémarre constamment
```bash
# Voir l'erreur
gcloud logging read --limit 10 --format=json | jq '.[] | .jsonPayload'

# Augmenter les ressources
gcloud run deploy character-manager `
  --cpu=2 `
  --memory=1Gi
```

### Impossible de se connecter
```bash
# Vérifier le service est actif
gcloud run services list

# Vérifier les permissions
gcloud run services describe character-manager
```

### Lent / Timeout
```bash
# Ajouter une instance "warm"
gcloud run deploy character-manager `
  --min-instances=1
```

---

## 🛠️ Mise à Jour de l'Application

```powershell
# 1. Faire les changements localement
# 2. Committer et pusher sur GitHub
# 3. Relancer le déploiement

.\scripts\Deploy-GoogleCloud.ps1
```

---

## 📚 Documentation Complète

Voir [DEPLOYMENT.md](./DEPLOYMENT.md) pour :
- ✅ Toutes les options de déploiement (Cloud Run, Compute Engine, App Engine)
- ✅ Configuration de la base de données (Cloud SQL)
- ✅ Configuration des domaines personnalisés
- ✅ Monitoring et alertes
- ✅ Troubleshooting avancé
- ✅ Estimations de coûts détaillées

---

## ❓ Support

**Besoin d'aide ?**

- 📖 [Google Cloud Documentation](https://cloud.google.com/docs)
- 📖 [Cloud Run Guide](https://cloud.google.com/run/docs/quickstarts/build-and-deploy)
- 💬 [Stack Overflow - google-cloud-run](https://stackoverflow.com/questions/tagged/google-cloud-run)
- 🐛 [GitHub Issues](https://github.com/Thorinval/CharacterManager/issues)

---

## ✅ Checklist

- [ ] Compte Google Cloud créé
- [ ] Gcloud CLI installé et configuré
- [ ] Projet GCP créé (`character-manager-prod`)
- [ ] APIs activées
- [ ] Script de déploiement exécuté
- [ ] Application accessible via HTTPS
- [ ] Logs consultables
- [ ] Monitoring configuré
- [ ] Domaine personnalisé (optionnel)
- [ ] Équipe invitée (IAM)

---

**Dernière mise à jour** : v0.2.0 (2025)
