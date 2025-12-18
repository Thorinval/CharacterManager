# Configuration GitHub Actions pour Google Cloud

> Guide pour configurer GitHub Actions et déployer automatiquement sur Google Cloud

---

## 🔐 Configuration des Secrets GitHub

### 1. Créer un Service Account Google Cloud

```bash
# Créer le service account
gcloud iam service-accounts create github-actions-sa \
  --display-name="GitHub Actions Service Account"

# Assigner les rôles nécessaires
gcloud projects add-iam-policy-binding CHARACTER-MANAGER-PROD \
  --member="serviceAccount:github-actions-sa@CHARACTER-MANAGER-PROD.iam.gserviceaccount.com" \
  --role="roles/run.admin"

gcloud projects add-iam-policy-binding CHARACTER-MANAGER-PROD \
  --member="serviceAccount:github-actions-sa@CHARACTER-MANAGER-PROD.iam.gserviceaccount.com" \
  --role="roles/storage.admin"

gcloud projects add-iam-policy-binding CHARACTER-MANAGER-PROD \
  --member="serviceAccount:github-actions-sa@CHARACTER-MANAGER-PROD.iam.gserviceaccount.com" \
  --role="roles/artifactregistry.admin"

# Créer une clé JSON
gcloud iam service-accounts keys create key.json \
  --iam-account=github-actions-sa@CHARACTER-MANAGER-PROD.iam.gserviceaccount.com
```

### 2. Ajouter le Secret à GitHub

1. Aller sur GitHub : **Settings** → **Secrets and variables** → **Actions**
2. Cliquer sur **New repository secret**
3. **Name** : `GCP_CREDENTIALS`
4. **Value** : Contenu du fichier `key.json` (copier-coller)
5. Cliquer sur **Add secret**

### 3. Ajouter d'autres Secrets (Optionnel)

```text
GCP_PROJECT_ID          = character-manager-prod
SLACK_WEBHOOK_URL       = https://hooks.slack.com/services/... (optionnel)
REGION                  = europe-west1
```

### 4. Protéger la Clé

```bash
# Supprimer la clé locale
rm key.json

# Vérifier que le secret est stocké sur GitHub
# (Ne pas commiter key.json dans git !)
```

---

## 📝 Fichier GitHub Actions

**Fichier** : `.github/workflows/deploy-gcp.yml`

### Jobs

#### 1. **build**

- Checkout code
- Setup .NET 9.0
- Restore & Build
- Run tests
- Publish

#### 2. **docker**

- Build image Docker
- Push vers Artifact Registry
- Tags : `latest`, `sha`, `branch`

#### 3. **deploy-staging**

- Triggered par : `git push origin develop`
- Environment : staging
- URL : `character-manager-staging-xxx.run.app`

#### 4. **deploy-production**

- Triggered par : `git push origin main`
- Environment : production
- URL : `character-manager.run.app`
- Min instances : 1 (warm start)

#### 5. **release** (Optionnel)

- Triggered par : tags `v*`
- Update Release Notes

#### 6. **notify**

- Slack webhook (si configuré)

---

## 🚀 Utilisation

### Déploiement Automatique (Staging)

```bash
# 1. Créer une branche feature
git checkout -b feature/new-feature

# 2. Faire les changements
# ... votre code ...

# 3. Commit & push vers develop
git add .
git commit -m "Add new feature"
git push origin develop

# ✅ GitHub Actions déploie automatiquement sur staging
# 📊 Vérifier le statut : GitHub → Actions tab
```

### Déploiement en Production

```bash
# 1. Merging dans main
git checkout main
git merge feature/new-feature

# 2. Push vers main
git push origin main

# ✅ GitHub Actions déploie automatiquement en production
# 🌐 https://character-manager.run.app
```

### Déploiement Manuel (Release)

```bash
# 1. Créer un tag
git tag -a v0.3.0 -m "Version 0.3.0"

# 2. Push le tag
git push origin v0.3.0

# ✅ GitHub Actions déploie + met à jour release notes
```

---

## 📊 Monitoring

### GitHub Actions Dashboard

1. Aller sur votre repository GitHub
2. Cliquer sur **Actions** tab
3. Voir l'historique des déploiements

### Voir les Logs

```bash
# Dans GitHub UI
# Cliquer sur le workflow → Voir les logs détaillés

# Ou via gcloud
gcloud run deployments describe CHARACTER-MANAGER-PROD
gcloud logging read "resource.type=cloud_run_revision" --limit 50
```

---

## 🔄 Workflow Complet

```text
┌─────────────────────────────────────────┐
│  Développeur fait un changement         │
└─────────────┬───────────────────────────┘
              │
              ▼
    ┌─────────────────────┐
    │  git push origin    │
    │  develop / main     │
    └──────────┬──────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  GitHub Actions Triggered            │
    │  - Build & Test (.NET)              │
    │  - Build Docker image               │
    │  - Push vers Artifact Registry      │
    └──────────┬──────────────────────────┘
              │
         ┌────┴─────────────────────┐
         │                           │
         ▼                           ▼
    Développer (staging)    Main (production)
    │                           │
    ▼                           ▼
Deploy staging             Deploy production
character-manager-staging  character-manager
    │                           │
    ▼                           ▼
Tester                     Utilisateurs finaux
```

---

## ⚠️ Troubleshooting

### Erreur : "Authentication failed"

```bash
# Vérifier que GCP_CREDENTIALS est configuré
# GitHub Settings → Secrets → GCP_CREDENTIALS

# Régénérer la clé
gcloud iam service-accounts keys create new-key.json \
  --iam-account=github-actions-sa@PROJECT.iam.gserviceaccount.com
```

### Erreur : "Permission denied"

```bash
# Vérifier les rôles du service account
gcloud projects get-iam-policy CHARACTER-MANAGER-PROD \
  --flatten="bindings[].members" \
  --format='table(bindings.role)' \
  --filter="bindings.members:github-actions-sa*"

# Ajouter les rôles manquants
gcloud projects add-iam-policy-binding CHARACTER-MANAGER-PROD \
  --member="serviceAccount:github-actions-sa@PROJECT.iam.gserviceaccount.com" \
  --role="roles/run.admin"
```

### Erreur : "Docker push failed"

```bash
# Vérifier que Artifact Registry est configuré
gcloud artifacts repositories list --location=europe-west1

# Vérifier la permission
gcloud projects get-iam-policy CHARACTER-MANAGER-PROD \
  --member="serviceAccount:github-actions-sa@PROJECT.iam.gserviceaccount.com" \
  --filter="artifactregistry"
```

---

## 📊 Coûts

### GitHub Actions Gratuit

- ✅ 2000 minutes/mois pour les dépôts publics
- ✅ 3000 minutes/mois pour les dépôts privés (avec compte Pro)

### Google Cloud (Build & Push)

- Artifact Registry : ~$0.10/Go
- Cloud Run invocations : $0.40 / M requêtes

---

## 🔐 Best Practices

### Secrets Management

- ✅ Ne pas commiter les secrets
- ✅ Rotate les clés tous les 90 jours
- ✅ Utiliser des rôles limités (least privilege)
- ✅ Audit les accès via Cloud Logging

### Déploiement

- ✅ Toujours tester sur staging d'abord
- ✅ Utiliser des tags sémantiques (v1.0.0)
- ✅ Écrire des tests avant déployer
- ✅ Monitorer après déploiement

### Sécurité

- ✅ Limiter les permissions du service account
- ✅ Activer Cloud Armor pour DDoS
- ✅ Utiliser des certificats SSL (automatique)
- ✅ Monitorer les logs d'authentification

---

## 📚 Documentation

- 📖 [GitHub Actions Documentation](https://docs.github.com/en/actions)
- 📖 [Authenticate to Google Cloud](https://github.com/google-github-actions/auth)
- 📖 [Google Cloud Security Best Practices](https://cloud.google.com/security/best-practices)
- 📖 [Service Accounts IAM Roles](https://cloud.google.com/iam/docs/understanding-service-accounts)

---

## ✅ Checklist Setup

- [ ] Service Account créé
- [ ] Rôles assignés (run.admin, storage.admin, etc.)
- [ ] Clé JSON générée
- [ ] Secret `GCP_CREDENTIALS` configuré sur GitHub
- [ ] Secret `GCP_PROJECT_ID` configuré (optionnel)
- [ ] Workflow `.github/workflows/deploy-gcp.yml` committé
- [ ] Branche `develop` → teste le déploiement staging
- [ ] Branche `main` → teste le déploiement production
- [ ] Vérifier les logs GitHub Actions
- [ ] Application accessible sur Cloud Run

---

**Prêt pour CI/CD automatisé !** 🚀
