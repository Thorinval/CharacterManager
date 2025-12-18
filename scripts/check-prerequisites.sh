#!/bin/bash
# Script de Vérification des Prérequis pour Déploiement Google Cloud (Linux/macOS)
# Usage: ./scripts/check-prerequisites.sh

set -e

# Couleurs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

ERROR_COUNT=0
WARNING_COUNT=0

# Fonctions
print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
    ((ERROR_COUNT++))
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
    ((WARNING_COUNT++))
}

print_info() {
    echo -e "${CYAN}ℹ️  $1${NC}"
}

check_command() {
    local cmd=$1
    local display_name=$2
    local min_version=$3
    local install_url=$4
    
    echo ""
    print_info "Vérification: $display_name"
    
    if command -v "$cmd" &> /dev/null; then
        local path=$(command -v "$cmd")
        print_success "  Installé: $path"
        
        if [ -n "$min_version" ]; then
            local version=$($cmd --version 2>&1 | head -n1 | grep -oP '\d+\.\d+' | head -1)
            if [ -n "$version" ]; then
                print_success "  Version: $version"
            fi
        fi
        
        return 0
    else
        print_error "  Non trouvé"
        if [ -n "$install_url" ]; then
            print_info "  Installer depuis: $install_url"
        fi
        return 1
    fi
}

# En-tête
echo ""
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}🔍 Vérification des Prérequis - Déploiement Google Cloud${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"

# Outils essentiels
echo ""
echo -e "${YELLOW}📋 Outils Essentiels${NC}"
echo -e "${CYAN}──────────────────────────────────────────────────────────────${NC}"

GCLOUD_OK=0
DOCKER_OK=0
DOTNET_OK=0

check_command "gcloud" "Google Cloud SDK" "450.0" "https://cloud.google.com/sdk/docs/install" && GCLOUD_OK=1 || true
check_command "docker" "Docker" "20.0" "https://docs.docker.com/get-docker/" && DOCKER_OK=1 || true
check_command "dotnet" ".NET CLI" "9.0" "https://dotnet.microsoft.com/en-us/download/dotnet/9.0" && DOTNET_OK=1 || true

# Outils optionnels
echo ""
echo -e "${YELLOW}📦 Outils Optionnels${NC}"
echo -e "${CYAN}──────────────────────────────────────────────────────────────${NC}"

check_command "git" "Git" "2.0" "https://git-scm.com/download" && true || true
check_command "terraform" "Terraform" "1.0" "https://www.terraform.io/downloads.html" && true || true
check_command "node" "Node.js (optionnel)" "16.0" "https://nodejs.org/en/download/" && true || true

# Configuration GCP
if [ $GCLOUD_OK -eq 1 ]; then
    echo ""
    echo -e "${YELLOW}☁️  Google Cloud Configuration${NC}"
    echo -e "${CYAN}──────────────────────────────────────────────────────────────${NC}"
    
    PROJECT=$(gcloud config get-value project 2>/dev/null || echo "")
    if [ -n "$PROJECT" ] && [ "$PROJECT" != "null" ]; then
        print_success "  Projet actif: $PROJECT"
    else
        print_warning "  Aucun projet GCP configuré"
        print_info "  Exécuter: gcloud init"
    fi
    
    AUTH=$(gcloud auth list 2>/dev/null | grep -i active || echo "")
    if [ -n "$AUTH" ]; then
        print_success "  Authentifié"
    else
        print_error "  Non authentifié"
        print_info "  Exécuter: gcloud auth login"
    fi
fi

# Ports
echo ""
echo -e "${YELLOW}🔌 Ports Réseau${NC}"
echo -e "${CYAN}──────────────────────────────────────────────────────────────${NC}"

for port in 5269 80 443 8080; do
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1 ; then
        print_warning "  Port $port déjà utilisé"
    else
        print_success "  Port $port disponible"
    fi
done

# Espace disque
echo ""
echo -e "${YELLOW}💾 Espace Disque${NC}"
echo -e "${CYAN}──────────────────────────────────────────────────────────────${NC}"

AVAILABLE=$(df / | tail -1 | awk '{print $4}')
AVAILABLE_GB=$((AVAILABLE / 1024 / 1024))

if [ $AVAILABLE_GB -gt 10 ]; then
    print_success "  Espace libre: ${AVAILABLE_GB} GB (recommandé: 10+ GB)"
else
    print_warning "  Espace libre: ${AVAILABLE_GB} GB (recommandé: 10+ GB)"
fi

# Résumé
echo ""
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}📊 Résumé${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"

echo ""
print_info "Erreurs: $ERROR_COUNT"
print_info "Avertissements: $WARNING_COUNT"

if [ $ERROR_COUNT -eq 0 ]; then
    echo ""
    print_success "✅ Tous les prérequis sont satisfaits!"
    echo ""
    print_info "Prochaines étapes:"
    print_info "  1. Configurer le projet GCP: gcloud init"
    print_info "  2. Lancer le déploiement:"
    print_info "     ./scripts/Deploy-GoogleCloud.ps1"
    echo ""
    exit 0
else
    echo ""
    print_error "❌ $ERROR_COUNT erreur(s) détectée(s)"
    echo ""
    print_info "À faire:"
    print_info "  1. Installer les outils manquants (voir liens ci-dessus)"
    print_info "  2. Vérifier la configuration GCP"
    print_info "  3. Re-exécuter cette vérification"
    echo ""
    print_info "Pour plus d'aide: https://cloud.google.com/docs"
    echo ""
    exit 1
fi
