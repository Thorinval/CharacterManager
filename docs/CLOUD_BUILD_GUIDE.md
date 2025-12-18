# Google Cloud Build - Alternative à Docker Desktop

> Déployez sur Google Cloud **sans installer Docker** !

---

## ✨ Pourquoi Cloud Build ?

**Avantages** :

- ✅ Pas besoin d'installer Docker Desktop localement
- ✅ Build plus rapide (infrastructure Google)
- ✅ Gratuit jusqu'à 120 minutes/jour
- ✅ Build parallélisés automatiques
- ✅ Cache intelligent

**Inconvénients** :

- ❌ Nécessite connexion internet
- ❌ Premier build plus lent (téléchargement du code)

---

## 🚀 Utilisation

### Option 1 : Build Simple

```bash
# Dans le répertoire du projet
gcloud builds submit --tag europe-west1-docker.pkg.dev/PROJECT_ID/character-manager/app:latest
```

### Option 2 : Build avec Configuration Personnalisée

Créer un fichier `cloudbuild.yaml` :

```yaml
steps:
  # Build l'application .NET
  - name: 'mcr.microsoft.com/dotnet/sdk:9.0'
    args:
      - 'publish'
      - 'CharacterManager/CharacterManager.csproj'
      - '--configuration'
      - 'Release'
      - '--output'
      - 'publish'
  
  # Build l'image Docker
  - name: 'gcr.io/cloud-builders/docker'
    args:
      - 'build'
      - '-t'
      - 'europe-west1-docker.pkg.dev/$PROJECT_ID/character-manager/app:latest'
      - '-t'
      - 'europe-west1-docker.pkg.dev/$PROJECT_ID/character-manager/app:$SHORT_SHA'
      - '.'
  
  # Push vers Artifact Registry
  - name: 'gcr.io/cloud-builders/docker'
    args:
      - 'push'
      - 'europe-west1-docker.pkg.dev/$PROJECT_ID/character-manager/app:latest'

images:
  - 'europe-west1-docker.pkg.dev/$PROJECT_ID/character-manager/app:latest'
  - 'europe-west1-docker.pkg.dev/$PROJECT_ID/character-manager/app:$SHORT_SHA'

options:
  machineType: 'E2_HIGHCPU_8'
  logging: CLOUD_LOGGING_ONLY
```

Puis lancer :

```bash
gcloud builds submit --config cloudbuild.yaml
```

---

## 📊 Comparaison

| Critère | Docker Desktop | Cloud Build |
|---------|----------------|-------------|
| **Installation** | Lourde (4+ GB) | Aucune |
| **Vitesse** | Dépend du PC | Infrastructure Google |
| **Coût** | Gratuit | 120 min/jour gratuit |
| **Internet** | Pas nécessaire | Requis |
| **Compatibilité** | Windows/Mac/Linux | Tous OS |

---

## 💰 Coûts Cloud Build

- ✅ **Gratuit** : 120 minutes de build/jour
- Au-delà : $0.003/minute build (~$0.18/heure)

**Exemple** :

- Build CharacterManager : ~3-5 minutes
- 10 builds/jour : 30-50 minutes → **Gratuit** ✅

---

## 🔧 Script de Déploiement avec Cloud Build

Le script `Deploy-GoogleCloud.ps1` détecte automatiquement si Docker est installé :

```powershell
# Si Docker installé → build local
# Si Docker absent → utilise Cloud Build

.\scripts\Deploy-GoogleCloud.ps1 -ProjectId "character-manager-prod"
```

---

## ⚠️ Troubleshooting

### Erreur : "Cloud Build API not enabled"

```bash
gcloud services enable cloudbuild.googleapis.com
```

### Erreur : "Permission denied"

```bash
# Ajouter les permissions
gcloud projects add-iam-policy-binding PROJECT_ID \
  --member="user:VOTRE_EMAIL@gmail.com" \
  --role="roles/cloudbuild.builds.builder"
```

### Build trop lent

```yaml
# Augmenter la machine dans cloudbuild.yaml
options:
  machineType: 'E2_HIGHCPU_32'  # Plus rapide
```

---

## 📚 Documentation

- 📖 [Cloud Build Documentation](https://cloud.google.com/build/docs)
- 📖 [Cloud Build Pricing](https://cloud.google.com/build/pricing)
- 📖 [cloudbuild.yaml Reference](https://cloud.google.com/build/docs/build-config-file-schema)

---

**Recommandation** : Utilisez Cloud Build pour déployer sans Docker Desktop ! 🚀
