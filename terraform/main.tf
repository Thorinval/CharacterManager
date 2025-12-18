# Terraform Configuration for Character Manager on Google Cloud
# 
# Déploiement Infrastructure as Code de Character Manager
# Utilise: Terraform 1.0+
#
# Usage:
#   terraform init
#   terraform plan
#   terraform apply
#   terraform destroy (pour supprimer)

terraform {
  required_version = ">= 1.0"
  
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }

  # Décommenter pour utiliser Google Cloud Storage comme backend
  # backend "gcs" {
  #   bucket = "character-manager-terraform-state"
  #   prefix = "terraform/state"
  # }
}

# Provider Configuration
provider "google" {
  project = var.gcp_project_id
  region  = var.gcp_region
}

# ═══════════════════════════════════════════════════════════════════════
# Variables
# ═══════════════════════════════════════════════════════════════════════

variable "gcp_project_id" {
  description = "GCP Project ID"
  type        = string
  default     = "character-manager-prod"
}

variable "gcp_region" {
  description = "GCP Region"
  type        = string
  default     = "europe-west1"
}

variable "gcp_zone" {
  description = "GCP Zone"
  type        = string
  default     = "europe-west1-b"
}

variable "app_name" {
  description = "Application Name"
  type        = string
  default     = "character-manager"
}

variable "app_version" {
  description = "Application Version"
  type        = string
  default     = "0.2.0"
}

variable "deployment_type" {
  description = "Type de déploiement: cloud_run ou compute_engine"
  type        = string
  default     = "cloud_run"
  
  validation {
    condition     = contains(["cloud_run", "compute_engine"], var.deployment_type)
    error_message = "deployment_type doit être 'cloud_run' ou 'compute_engine'"
  }
}

variable "cloud_run_memory" {
  description = "Memory allocation for Cloud Run"
  type        = string
  default     = "512Mi"
}

variable "cloud_run_cpu" {
  description = "CPU allocation for Cloud Run"
  type        = string
  default     = "1"
}

variable "gce_machine_type" {
  description = "Machine type pour Compute Engine"
  type        = string
  default     = "e2-medium"
}

variable "environment_variables" {
  description = "Environment variables for the application"
  type        = map(string)
  default = {
    ASPNETCORE_ENVIRONMENT = "Production"
    Logging__LogLevel__Default = "Information"
  }
}

# ═══════════════════════════════════════════════════════════════════════
# Outputs
# ═══════════════════════════════════════════════════════════════════════

output "cloud_run_url" {
  description = "URL de Cloud Run"
  value       = try(google_cloud_run_service.character_manager[0].status[0].url, "")
}

output "compute_engine_ip" {
  description = "Adresse IP publique de Compute Engine"
  value       = try(google_compute_instance.character_manager[0].network_interface[0].access_config[0].nat_ip, "")
}

output "artifact_registry_url" {
  description = "URL de l'Artifact Registry"
  value       = google_artifact_registry_repository.character_manager.repository_config[0].docker_config[0].docker_repository
}

# ═══════════════════════════════════════════════════════════════════════
# APIs à Activer
# ═══════════════════════════════════════════════════════════════════════

resource "google_project_service" "run" {
  service            = "run.googleapis.com"
  disable_on_destroy = false
}

resource "google_project_service" "artifactregistry" {
  service            = "artifactregistry.googleapis.com"
  disable_on_destroy = false
}

resource "google_project_service" "sqladmin" {
  service            = "sqladmin.googleapis.com"
  disable_on_destroy = false
}

resource "google_project_service" "compute" {
  service            = "compute.googleapis.com"
  disable_on_destroy = false
}

# ═══════════════════════════════════════════════════════════════════════
# Artifact Registry (Docker Image Storage)
# ═══════════════════════════════════════════════════════════════════════

resource "google_artifact_registry_repository" "character_manager" {
  location      = var.gcp_region
  repository_id = var.app_name
  format        = "DOCKER"
  description   = "Docker repository for ${var.app_name}"

  depends_on = [google_project_service.artifactregistry]
}

# ═══════════════════════════════════════════════════════════════════════
# Cloud Run (Option A: Serverless)
# ═══════════════════════════════════════════════════════════════════════

resource "google_cloud_run_service" "character_manager" {
  count    = var.deployment_type == "cloud_run" ? 1 : 0
  name     = var.app_name
  location = var.gcp_region

  template {
    spec {
      containers {
        image = "${var.gcp_region}-docker.pkg.dev/${var.gcp_project_id}/${google_artifact_registry_repository.character_manager.repository_id}/app:latest"

        resources {
          limits = {
            memory = var.cloud_run_memory
            cpu    = var.cloud_run_cpu
          }
        }

        env {
          name = "ASPNETCORE_ENVIRONMENT"
          value = "Production"
        }

        env {
          name = "ASPNETCORE_URLS"
          value = "http://+:8080"
        }

        dynamic "env" {
          for_each = var.environment_variables
          content {
            name  = env.key
            value = env.value
          }
        }
      }

      service_account_name = google_service_account.cloud_run.email
    }

    metadata {
      annotations = {
        "autoscaling.knative.dev/maxScale" = "100"
        "autoscaling.knative.dev/minScale" = "1"
      }
    }
  }

  traffic {
    percent         = 100
    latest_revision = true
  }

  depends_on = [
    google_project_service.run,
    google_artifact_registry_repository.character_manager
  ]
}

# Cloud Run Service Account
resource "google_service_account" "cloud_run" {
  account_id   = "${var.app_name}-sa"
  display_name = "Service Account for ${var.app_name}"
}

# Cloud Run IAM Policy (Allow unauthenticated access)
resource "google_cloud_run_service_iam_member" "run_public" {
  count   = var.deployment_type == "cloud_run" ? 1 : 0
  service = google_cloud_run_service.character_manager[0].name
  role    = "roles/run.invoker"
  member  = "allUsers"
}

# ═══════════════════════════════════════════════════════════════════════
# Compute Engine (Option B: VMs)
# ═══════════════════════════════════════════════════════════════════════

# Persistent Disk for Data
resource "google_compute_disk" "data" {
  count = var.deployment_type == "compute_engine" ? 1 : 0
  name  = "${var.app_name}-data-disk"
  size  = 20
  type  = "pd-standard"
  zone  = var.gcp_zone
}

# Persistent Disk for Images
resource "google_compute_disk" "images" {
  count = var.deployment_type == "compute_engine" ? 1 : 0
  name  = "${var.app_name}-images-disk"
  size  = 50
  type  = "pd-standard"
  zone  = var.gcp_zone
}

# Firewall Rules
resource "google_compute_firewall" "allow_http_https" {
  count   = var.deployment_type == "compute_engine" ? 1 : 0
  name    = "${var.app_name}-allow-web"
  network = "default"

  allow {
    protocol = "tcp"
    ports    = ["80", "443", "5269"]
  }

  source_ranges = ["0.0.0.0/0"]
  target_tags   = [var.app_name]
}

# Compute Engine Instance
resource "google_compute_instance" "character_manager" {
  count        = var.deployment_type == "compute_engine" ? 1 : 0
  name         = var.app_name
  machine_type = var.gce_machine_type
  zone         = var.gcp_zone

  boot_disk {
    initialize_params {
      image = "debian-cloud/debian-11"
      size  = 30
    }
  }

  attached_disk {
    source = google_compute_disk.data[0].id
  }

  attached_disk {
    source = google_compute_disk.images[0].id
  }

  network_interface {
    network = "default"
    access_config {}
  }

  metadata = {
    startup-script = file("${path.module}/startup-script.sh")
    enable-oslogin = "FALSE"
  }

  metadata_startup_script = file("${path.module}/startup-script.sh")

  tags = [var.app_name]

  depends_on = [
    google_project_service.compute,
    google_compute_disk.data,
    google_compute_disk.images
  ]
}

# ═══════════════════════════════════════════════════════════════════════
# Cloud SQL (Optional: Database)
# ═══════════════════════════════════════════════════════════════════════

resource "google_sql_database_instance" "character_manager_db" {
  name             = "${var.app_name}-db"
  database_version = "POSTGRES_15"
  region           = var.gcp_region

  settings {
    tier              = "db-f1-micro"
    availability_type = "REGIONAL"
    backup_configuration {
      enabled                        = true
      start_time                     = "03:00"
      transaction_log_retention_days = 7
    }
  }

  deletion_protection = true

  depends_on = [google_project_service.sqladmin]
}

# Cloud SQL Database
resource "google_sql_database" "character_manager" {
  name     = "character_manager"
  instance = google_sql_database_instance.character_manager_db.name
}

# Cloud SQL User
resource "google_sql_user" "app_user" {
  name     = "app_user"
  instance = google_sql_database_instance.character_manager_db.name
  password = random_password.db_password.result
}

# Random password for DB
resource "random_password" "db_password" {
  length  = 32
  special = true
}

# ═══════════════════════════════════════════════════════════════════════
# Cloud Storage Bucket (Images, si needed)
# ═══════════════════════════════════════════════════════════════════════

resource "google_storage_bucket" "images" {
  name          = "${var.gcp_project_id}-${var.app_name}-images"
  location      = var.gcp_region
  force_destroy = false

  uniform_bucket_level_access = true

  versioning {
    enabled = true
  }

  lifecycle_rule {
    condition {
      num_newer_versions = 5
    }
    action {
      type = "Delete"
    }
  }
}

# ═══════════════════════════════════════════════════════════════════════
# Cloud Monitoring Alert Policy (Optional)
# ═══════════════════════════════════════════════════════════════════════

# Uncomment to enable alerts
# resource "google_monitoring_alert_policy" "character_manager_error_rate" {
#   display_name = "${var.app_name} - High Error Rate"
#   combiner     = "OR"

#   conditions {
#     display_name = "Error Rate > 5%"
#     condition_threshold {
#       filter          = "resource.type = \"cloud_run_revision\" AND metric.type = \"run.googleapis.com/request_count\""
#       comparison      = "COMPARISON_GT"
#       threshold_value = 0.05
#       duration        = "300s"
#     }
#   }

#   notification_channels = []  # Ajouter les channel IDs
# }
