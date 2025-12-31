# CI/CD Setup Guide - GitHub Actions

> **Last Updated**: December 19, 2025  
> **Status**: ✅ Ready for deployment

## 📋 Overview

This guide provides step-by-step instructions to configure GitHub Actions CI/CD for automated deployment to Google Cloud Platform.

## ✅ Pre-requisites

- Repository: [Thorinval/CharacterManager](https://github.com/Thorinval/CharacterManager)
- Deployed workflows: `.github/workflows/deploy-gcp.yml`
- Local verification: Docker Desktop + `act` CLI

## 🔐 Required GitHub Secrets

Configure these secrets in your repository settings:

### 1. **GCP_CREDENTIALS** (Required)

Service account JSON credentials for Google Cloud Platform authentication.

**Steps to create:**

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Navigate to `IAM & Admin` → `Service Accounts`
3. Create a new service account or select existing one
4. Grant roles:
   - `roles/artifactregistry.admin` (push Docker images)
   - `roles/run.admin` (deploy to Cloud Run)
   - `roles/compute.serviceAccountUser` (use service account)
5. Create a JSON key:
   - Click on service account
   - Go to `Keys` → `Add Key` → `Create new key` → `JSON`
6. Copy the entire JSON content
7. Add to GitHub repository secrets:
   - Repository → Settings → Secrets and variables → Actions
   - New repository secret
   - Name: `GCP_CREDENTIALS`
   - Value: Paste the entire JSON

### 2. **GCP_PROJECT_ID** (Required)

Your Google Cloud project ID.

**Steps to configure:**

1. From Google Cloud Console, copy your project ID
2. Add to GitHub repository secrets:
   - Name: `GCP_PROJECT_ID`
   - Value: Your project ID (e.g., `my-project-12345`)

### 3. **SLACK_WEBHOOK_URL** (Optional)

Slack webhook for deployment notifications.

**Steps to configure:**

1. Create a Slack App (or use existing)
2. Enable Incoming Webhooks
3. Create new webhook URL
4. Add to GitHub repository secrets:
   - Name: `SLACK_WEBHOOK_URL`
   - Value: Paste the webhook URL

## 🚀 Deployment Workflow

The workflow is triggered automatically on:

- **Push to `develop` branch** → Deploy to **Staging**
- **Push to `main` branch** → Deploy to **Production**
- **Manual trigger** via GitHub Actions UI (workflow_dispatch)

### Workflow Stages

```
Stage 1: Verify Required Secrets (Early failure if missing)
         ↓
Stage 2: Build & Test Application (dotnet restore, build, test)
         ↓
Stage 3: Build & Push Docker Image (requires build success + secrets)
         ↓
Stage 4a: Deploy to Cloud Run (Staging) - if develop branch
         ↓
Stage 4b: Deploy to Cloud Run (Production) - if main branch
         ↓
Stage 5: Send Notifications (always runs)
```

## 🧪 Local Dry-Run Testing

### Prerequisites

```powershell
# Install act (GitHub Actions runner simulator)
winget install nektos.act

# Install Docker Desktop
# Download from: https://www.docker.com/products/docker-desktop
```

### Run Dry-Run

```powershell
cd d:\Devs\CharacterManager

# Test develop branch deployment
act -n -e .github/workflows/test-events/push-develop.json

# Test main branch deployment
act -n -e .github/workflows/test-events/push-main.json
```

**Options:**
- `-n` = Dry-run (no actual execution)
- `-e` = Event payload file
- `-v` = Verbose output
- `-l` = List jobs without running

## 📊 Workflow Jobs

### 1. Verify Required Secrets

Checks for missing `GCP_CREDENTIALS` and `GCP_PROJECT_ID` early.

**Fails if:**
- `GCP_CREDENTIALS` is empty
- `GCP_PROJECT_ID` is empty

### 2. Build Application

- Checkout code
- Setup .NET 9.0
- Restore dependencies
- Build (Release configuration)
- Run tests
- Publish to `publish/` directory
- Upload artifacts

### 3. Build & Push Docker Image

- Setup Google Cloud SDK
- Authenticate to GCP using credentials
- Build Docker image with multiple tags (latest, commit SHA, branch name)
- Push to Artifact Registry

**Only runs if:**
- Build job succeeds
- Secrets job succeeds
- Event is a push (not manual)

### 4. Deploy to Cloud Run

#### Staging (develop branch)

- Authenticates to GCP
- Deploys to `character-manager-staging` service
- Configuration:
  - Region: `europe-west1`
  - Memory: 512Mi
  - CPU: 1
  - Environment: Staging
  - Timeout: 3600s (1 hour)

#### Production (main branch)

- Authenticates to GCP
- Deploys to `character-manager` service
- Configuration:
  - Region: `europe-west1`
  - Memory: 512Mi
  - CPU: 1
  - Min instances: 1 (always running)
  - Environment: Production
  - Timeout: 3600s
- Creates GitHub deployment record

### 5. Send Notifications

Sends deployment status to Slack (if webhook configured).

## 🔧 Troubleshooting

### Workflow Fails at Secrets Job

**Error:** "Missing required secrets: GCP_CREDENTIALS GCP_PROJECT_ID"

**Solution:**
1. Go to Repository → Settings → Secrets and variables → Actions
2. Verify `GCP_CREDENTIALS` and `GCP_PROJECT_ID` are added
3. Ensure values are not empty
4. Re-run the workflow

### Docker Image Push Fails

**Error:** Authentication failed or Project ID invalid

**Solution:**
1. Verify `GCP_CREDENTIALS` JSON is valid and complete
2. Check service account has `artifactregistry.admin` role
3. Verify `GCP_PROJECT_ID` matches your Google Cloud project
4. Check Artifact Registry is enabled in Google Cloud

### Cloud Run Deployment Fails

**Error:** Access denied or service not found

**Solution:**
1. Ensure service account has `roles/run.admin`
2. Verify Cloud Run is enabled in your GCP project
3. Check network permissions (if using VPC)
4. Review Cloud Run deployment logs in GCP Console

### Slack Notifications Not Sent

**Error:** No notification received

**Solution:**
1. `SLACK_WEBHOOK_URL` might be missing or expired
2. Refresh webhook in Slack App settings
3. Update the secret in GitHub
4. Re-run workflow

## 📝 Environment Variables

Set in workflow (or override via Cloud Run service):

```env
ASPNETCORE_ENVIRONMENT=Staging    # Staging deployment
ASPNETCORE_ENVIRONMENT=Production # Production deployment
LOG_LEVEL=Debug                   # Staging
LOG_LEVEL=Information             # Production
```

## 🔄 Rerunning Failed Workflows

1. Go to GitHub Actions tab
2. Select the failed workflow run
3. Click "Re-run jobs" or "Re-run all jobs"
4. Monitor the new run

## 📚 References

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Google Cloud Deploy GitHub Action](https://github.com/google-github-actions/auth)
- [act - Local GitHub Actions Runner](https://nektosact.com/)
- [Cloud Run Deployment Guide](https://cloud.google.com/run/docs/deploying)

## ✅ Verification Checklist

Before going live:

- [ ] `GCP_CREDENTIALS` secret configured and valid
- [ ] `GCP_PROJECT_ID` secret configured with correct project ID
- [ ] Google Cloud service account has required roles
- [ ] Artifact Registry enabled in Google Cloud
- [ ] Cloud Run API enabled in Google Cloud
- [ ] Slack webhook configured (optional)
- [ ] Local dry-run tests pass
- [ ] Test deployment to staging (develop branch)
- [ ] Verify staging deployment is accessible
- [ ] Test deployment to production (main branch)
- [ ] Monitor logs in Cloud Run console

## 📞 Support

For issues or questions:
1. Check GitHub Actions run logs
2. Review Cloud Run deployment logs
3. Verify Google Cloud IAM permissions
4. Check secret values and formats
