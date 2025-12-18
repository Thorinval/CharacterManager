# Terraform Configuration for Character Manager

Infrastructure as Code (IaC) pour déployer Character Manager sur Google Cloud automatiquement.

## 📋 Prérequis

1. **Terraform 1.0+**

  ```bash
   # Installer Terraform
   # Windows: https://www.terraform.io/downloads.html
   
   # Vérifier
   terraform --version
   ```

1. **Google Cloud SDK**

  ```bash
   gcloud --version
   gcloud auth login
   gcloud config set project character-manager-prod
   ```

1. **Permissions Google Cloud**
   - Editor role sur le projet GCP
   - Ou permissions manuelles sur : Cloud Run, Compute Engine, Cloud SQL, Artifact Registry

## 🚀 Démarrage Rapide

### 1. Configurer les Variables

```bash
# Copier le fichier example
cp terraform.tfvars.example terraform.tfvars

# Éditer avec vos valeurs
# Adapter:
# - gcp_project_id
# - deployment_type (cloud_run ou compute_engine)
# - Région (europe-west1, us-central1, etc.)
```

### 2. Initialiser Terraform

```bash
terraform init
```

Cela va télécharger les providers Google Cloud nécessaires.

### 3. Vérifier le Plan

```bash
terraform plan
```

Affiche toutes les ressources qui vont être créées.

### 4. Appliquer la Configuration

```bash
terraform apply
```

Confirmez avec `yes` pour créer les ressources.

### 5. Récupérer les Outputs

```bash
terraform output

# Ou une variable spécifique
terraform output cloud_run_url
```

## 🗑️ Nettoyer (Supprimer Toutes les Ressources)

```bash
terraform destroy
```

Confirmez avec `yes`.

---

## 📝 Structure des Fichiers

```text
terraform/
├── main.tf                  # Configuration principale (providers, ressources)
├── terraform.tfvars.example # Exemple de variables
├── terraform.tfvars         # Variables (à ne pas commiter)
├── terraform.lock.hcl       # Lock file (généré automatiquement)
└── .terraform/              # Répertoire caché (providers téléchargés)
```

---

## 🔧 Configuration

### Variables Principales

#### GCP Configuration

- `gcp_project_id` : ID du projet GCP
- `gcp_region` : Région (ex: europe-west1, us-central1)
- `gcp_zone` : Zone (ex: europe-west1-b)

#### Application

- `app_name` : Nom de l'application (défaut: character-manager)
- `app_version` : Version de l'app (défaut: 0.2.0)

#### Type de Déploiement

- `deployment_type` : "cloud_run" (serverless) ou "compute_engine" (VMs)

#### Cloud Run

- `cloud_run_memory` : RAM (128Mi, 256Mi, 512Mi, 1Gi, 2Gi, 4Gi, 6Gi, 8Gi)
- `cloud_run_cpu` : CPU (1, 2, 4, 6, 8)

#### Compute Engine

- `gce_machine_type` : Type de VM (e2-small, e2-medium, e2-large, n1-standard-1)

### Ressources Créées

**Toujours créées** :

- ✅ Artifact Registry (Docker images)
- ✅ APIs activées (Cloud Run, Compute Engine, SQL Admin, etc.)

**Si deployment_type = "cloud_run"** :

- ✅ Cloud Run Service
- ✅ Service Account
- ✅ IAM Policy (accès public)

**Si deployment_type = "compute_engine"** :

- ✅ Compute Engine Instance
- ✅ Persistent Disks (data + images)
- ✅ Firewall Rules

**Optionnel** :

- ⚠️ Cloud SQL (décommentez dans main.tf)
- ⚠️ Cloud Storage Bucket (décommentez dans main.tf)
- ⚠️ Monitoring Alerts (décommentez dans main.tf)

---

## 📊 Outputs

Après `terraform apply`, vous pouvez récupérer :

```bash
# Tous les outputs
terraform output

# Output spécifique
terraform output cloud_run_url
terraform output compute_engine_ip
terraform output artifact_registry_url
```

---

## 🔐 Sécurité

### Remote State Storage (Production)

Pour partager la configuration avec une équipe, utiliser Google Cloud Storage :

1. **Créer un bucket**

  ```bash
   gsutil mb gs://character-manager-terraform-state
   gsutil versioning set on gs://character-manager-terraform-state
   ```

1. **Décommenter dans main.tf**

  ```hcl
   backend "gcs" {
     bucket = "character-manager-terraform-state"
     prefix = "terraform/state"
   }
   ```

1. **Re-initialiser**

  ```bash
   terraform init  # Confirmez la migration
   ```

### Variables Sensibles

Ne pas commiter `terraform.tfvars` dans Git !

```bash
# .gitignore
terraform.tfvars
*.tfvars
.terraform/
.terraform.lock.hcl
```

Utiliser des variables d'environnement pour les secrets :

```bash
export TF_VAR_gcp_project_id="your-project"
```

---

## 🔄 Workflow CI/CD (GitHub Actions)

Exemple `.github/workflows/terraform.yml` :

```yaml
name: Terraform

on:
  push:
    branches: [main]
    paths: ['terraform/**']

jobs:
  terraform:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - uses: hashicorp/setup-terraform@v2
        with:
          terraform_version: 1.5.0
      
      - name: Terraform Init
        run: cd terraform && terraform init
        env:
          GOOGLE_CREDENTIALS: ${{ secrets.GCP_CREDENTIALS }}
      
      - name: Terraform Plan
        run: cd terraform && terraform plan
        env:
          GOOGLE_CREDENTIALS: ${{ secrets.GCP_CREDENTIALS }}
      
      - name: Terraform Apply
        if: github.ref == 'refs/heads/main'
        run: cd terraform && terraform apply -auto-approve
        env:
          GOOGLE_CREDENTIALS: ${{ secrets.GCP_CREDENTIALS }}
```

---

## 📚 Commandes Utiles

```bash
# Voir l'état actuel
terraform show

# Voir les ressources créées
terraform state list

# Détails d'une ressource
terraform state show google_cloud_run_service.character_manager[0]

# Valider la configuration
terraform validate

# Formater le code
terraform fmt -recursive

# Importer une ressource existante
terraform import google_cloud_run_service.character_manager projects/PROJECT_ID/locations/REGION/services/character-manager

# Supprimer une ressource
terraform destroy -target=google_cloud_run_service.character_manager
```

---

## 🐛 Troubleshooting

### Erreur : "Permission denied"

```bash
gcloud auth login
gcloud config set project character-manager-prod
terraform init -upgrade
```

### Erreur : "Resource already exists"

```bash
# Importer la ressource existante
terraform import google_cloud_run_service.character_manager projects/PROJECT_ID/locations/europe-west1/services/character-manager

# Ou supprimer et recréer
terraform destroy -target=google_cloud_run_service.character_manager
```

### Stateful File Conflicts

```bash
# Réinitialiser le state
terraform state rm google_cloud_run_service.character_manager
terraform import google_cloud_run_service.character_manager projects/PROJECT_ID/locations/europe-west1/services/character-manager
```

---

## 📖 Documentation

- [Terraform Google Cloud Provider](https://registry.terraform.io/providers/hashicorp/google/latest/docs)
- [Google Cloud Run Terraform](https://registry.terraform.io/providers/hashicorp/google/latest/docs/resources/cloud_run_service)
- [Terraform Best Practices](https://www.terraform.io/language)

---

**Version** : Terraform 1.0+ avec Google Cloud Provider 5.0+

**Dernière mise à jour** : 2025 - Character Manager v0.2.0
