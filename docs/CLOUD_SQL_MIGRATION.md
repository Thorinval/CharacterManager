# Guide Cloud SQL Migration

> Migrer de SQLite (local) à PostgreSQL/MySQL (Google Cloud SQL)

---

## 📋 Vue d'ensemble

### Avant (SQLite Local)
- ✅ Simple à développer
- ❌ Pas persistant sur Cloud Run
- ❌ Pas de sauvegarde
- ❌ Scalabilité limitée

### Après (Cloud SQL)
- ✅ Persistant
- ✅ Backups automatiques
- ✅ Réplication haute dispo
- ✅ Scalabilité illimitée

---

## 🚀 Migration

### Option 1 : PostgreSQL (Recommandé)

#### 1. Créer l'Instance Cloud SQL

```bash
gcloud sql instances create character-manager-db \
  --database-version POSTGRES_15 \
  --tier db-f1-micro \
  --region europe-west1 \
  --backup-start-time 03:00 \
  --enable-bin-log
```

#### 2. Créer la Base de Données

```bash
gcloud sql databases create character_manager \
  --instance character-manager-db
```

#### 3. Créer l'Utilisateur

```bash
gcloud sql users create app_user \
  --instance character-manager-db \
  --password YOUR_STRONG_PASSWORD
```

#### 4. Configurer appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=cloudsql-proxy;Port=5432;Database=character_manager;Username=app_user;Password=YOUR_PASSWORD;"
  }
}
```

#### 5. Installer le Cloud SQL Proxy

```bash
# Dans Dockerfile
RUN curl -L -o cloud_sql_proxy https://dl.google.com/cloudsql/cloud_sql_proxy.linux.amd64
RUN chmod +x cloud_sql_proxy
```

#### 6. Configurer le script d'entrée

```bash
#!/bin/bash
# entrypoint.sh
./cloud_sql_proxy -instances=PROJECT_ID:REGION:INSTANCE_NAME=tcp:5432 &
sleep 2
exec dotnet CharacterManager.dll
```

---

### Option 2 : MySQL

```bash
# Créer l'instance
gcloud sql instances create character-manager-db \
  --database-version MYSQL_8_0 \
  --tier db-f1-micro \
  --region europe-west1

# Connection string
"Host=cloudsql-proxy;Port=3306;Database=character_manager;Uid=app_user;Pwd=PASSWORD;"
```

---

## 💾 Migration des Données

### Exporter depuis SQLite

```bash
# Export
sqlite3 CharacterManager.db ".dump" > data_dump.sql

# Ou utiliser Entity Framework Migrations
dotnet ef migrations add CloudSQLMigration
dotnet ef database update
```

### Importer dans PostgreSQL

```bash
# Connexion via Cloud SQL Proxy
psql -h localhost -U app_user -d character_manager < data_dump.sql

# Ou via Entity Framework
dotnet ef database update --project CharacterManager.csproj
```

---

## 🔐 Sécurité

### IAM Permissions

```bash
# Service Account pour app
gcloud iam service-accounts create character-manager-app

# SQL Client role
gcloud projects add-iam-policy-binding PROJECT_ID \
  --member "serviceAccount:character-manager-app@PROJECT_ID.iam.gserviceaccount.com" \
  --role "roles/cloudsql.client"
```

### Authorized Networks

```bash
# Permettre uniquement Cloud Run
gcloud sql instances patch character-manager-db \
  --require-ssl
```

---

## 🔄 Sauvegarde & Récupération

### Sauvegarde Automatique

```bash
gcloud sql backups create \
  --instance character-manager-db \
  --description "Manual backup"

# Lister les backups
gcloud sql backups list --instance character-manager-db
```

### Restauration

```bash
gcloud sql backups restore BACKUP_ID \
  --instance character-manager-db
```

---

## 📊 Monitoring

### Performance

```bash
# Connexions
gcloud sql operations list --instance character-manager-db

# Metrics
gcloud monitoring time-series list \
  --filter 'resource.type="cloudsql_database"'
```

### Logs

```bash
# Erreurs
gcloud sql operations list \
  --instance character-manager-db \
  --filter "status!=RUNNING"
```

---

## 💰 Coûts

| Tier | Spec | Coût/mois |
|------|------|-----------|
| db-f1-micro | 0.6 GB RAM | ~$4-5 |
| db-g1-small | 1.7 GB RAM | ~$8-10 |
| db-n1-standard-1 | 3.75 GB RAM | ~$20-25 |

**Pour démarrage** : db-f1-micro suffisant

---

## ⚠️ Troubleshooting

### Erreur de connexion

```bash
# Vérifier le proxy
docker ps | grep cloud_sql_proxy

# Vérifier l'instance
gcloud sql instances describe character-manager-db

# Logs
gcloud sql operations list --instance character-manager-db
```

### Lent

```bash
# Augmenter tier
gcloud sql instances patch character-manager-db \
  --tier db-g1-small

# Ajouter index
CREATE INDEX idx_personnages_nom ON Personnages(Nom);
```

### Timeout

```bash
# Augmenter le timeout de connexion
gcloud sql instances patch character-manager-db \
  --database-flags=cloudsql_iam_authentication=on

# Ou dans connection string
"Timeout=30;"
```

---

## 🔄 Rollback

### Revenir à SQLite

```bash
# Exporter depuis PostgreSQL
pg_dump character_manager > data.sql

# Convertir pour SQLite
# (Nécessite conversion manuelle)

# Reconfigurer appsettings.json
"DefaultConnection": "Data Source=CharacterManager.db"
```

---

## 📚 Resources

- 📖 [Cloud SQL Documentation](https://cloud.google.com/sql/docs)
- 📖 [Entity Framework Core SQL](https://docs.microsoft.com/en-us/ef/core/)
- 📖 [Cloud SQL Proxy](https://cloud.google.com/sql/docs/mysql/sql-proxy)

---

**Choix recommandé** : PostgreSQL + Cloud SQL Proxy sur Cloud Run
