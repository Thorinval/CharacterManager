#!/usr/bin/env pwsh
# Script de nettoyage du cache des tests
# Utilisation: ./clean-test-cache.ps1

Write-Host "Nettoyage du cache des tests..." -ForegroundColor Green

# Nettoyer tous les dossiers bin, obj, et TestResults
$folders = @("bin", "obj", "TestResults")
$count = 0

Get-ChildItem -Path (Get-Location).Path -Recurse -Directory | Where-Object { $_.Name -in $folders } | ForEach-Object {
    try {
        Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop
        $count++
        Write-Host "  [OK] Supprime: $($_.FullName)" -ForegroundColor Cyan
    } catch {
        Write-Host "  [ERREUR] Suppression de $($_.FullName): $_" -ForegroundColor Yellow
    }
}

# Nettoyer le cache VS Code
$vsCodeCache = "$env:APPDATA\Code\User\workspaceStorage"
if (Test-Path $vsCodeCache) {
    Get-ChildItem -Path $vsCodeCache | Where-Object { $_.Name -match "charactermanager|Character" } | ForEach-Object {
        try {
            Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop
            Write-Host "  [OK] Cache VS Code supprime: $($_.Name)" -ForegroundColor Cyan
            $count++
        } catch {
            Write-Host "  [ERREUR] Cache VS Code: $_" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "Nettoyage termine! ($count elements supprimes)" -ForegroundColor Green
Write-Host ""
Write-Host "Prochaines etapes:" -ForegroundColor Cyan
Write-Host "  1. dotnet clean" -ForegroundColor White
Write-Host "  2. dotnet build CharacterManager.Tests" -ForegroundColor White
Write-Host "  3. dotnet test CharacterManager.Tests" -ForegroundColor White
