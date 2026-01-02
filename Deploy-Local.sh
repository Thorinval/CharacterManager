#!/bin/bash
# Script de déploiement simple pour Linux/Mac
# Usage: ./Deploy-Local.sh [port]

PORT=${1:-5000}
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
APP_DIR="$SCRIPT_DIR/CharacterManager/bin/Release/net9.0"

echo "========================================="
echo "Character Manager - Starting Application"
echo "========================================="
echo ""
echo "Application URL: http://localhost:$PORT"
echo "Press Ctrl+C to stop"
echo ""

cd "$SCRIPT_DIR/CharacterManager"

# Publier l'application en Release
dotnet publish -c Release --self-contained

# Démarrer l'application
export ASPNETCORE_URLS="http://localhost:$PORT"
exec "$APP_DIR/CharacterManager" "$@"
