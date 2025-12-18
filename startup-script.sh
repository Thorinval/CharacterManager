#!/bin/bash
# Script de démarrage pour Compute Engine
# Ce script est exécuté au premier démarrage de la VM

set -e

echo "📦 Character Manager - Google Cloud VM Startup Script"
echo "======================================================"

# Mise à jour des paquets
echo "🔄 Mise à jour des paquets..."
apt-get update
apt-get upgrade -y

# Installation de Docker
echo "🐳 Installation de Docker..."
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
usermod -aG docker $USER

# Installation de git
echo "📝 Installation de git..."
apt-get install -y git curl wget

# Installation de Docker Compose
echo "📝 Installation de Docker Compose..."
curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# Installation de Certbot (SSL/TLS)
echo "🔐 Installation de Certbot..."
apt-get install -y certbot

# Clonage du repository
echo "📂 Clonage du repository..."
mkdir -p /opt
cd /opt
git clone https://github.com/Thorinval/CharacterManager.git
cd CharacterManager

# Création des répertoires pour les données persistantes
echo "📁 Création des répertoires de données..."
mkdir -p /mnt/data
mkdir -p /mnt/images
chmod -R 755 /mnt/data
chmod -R 755 /mnt/images

# Configuration de firewall
echo "🔥 Configuration du firewall..."
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 5269/tcp
ufw --force enable

# Lancement de l'application avec docker-compose
echo "🚀 Démarrage de l'application..."
cd /opt/CharacterManager

# Modifier le docker-compose pour pointer vers les données persistantes
sed -i 's|./data|/mnt/data|g' docker-compose.yml
sed -i 's|./images|/mnt/images|g' docker-compose.yml

# Démarrer les services
docker-compose up -d

# Configuration des logs
echo "📊 Configuration de la journalisation Cloud Logging..."
echo '{"type": "service_account"}' | gsutil config set-json-credentials -

# Récupérer l'adresse IP
IP_ADDRESS=$(hostname -I | awk '{print $1}')
echo ""
echo "✅ Installation terminée!"
echo "======================================================"
echo "🌐 Application accessible à: http://$IP_ADDRESS:5269"
echo "📊 Pour configurer un domaine personnalisé, accédez à"
echo "   Google Cloud Console > Cloud Load Balancing"
echo "======================================================"
