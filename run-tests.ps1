#!/usr/bin/env pwsh
# Script pour executer les tests avec la bonne configuration
# Utilisation: ./run-tests.ps1 -coverage

param(
    [switch]$coverage
)

# Configurer l'encodage UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'

Write-Host "Execution des tests CharacterManager..." -ForegroundColor Green
Write-Host ""

if ($coverage) {
    Write-Host "Mode: Test avec couverture de code" -ForegroundColor Cyan
    Write-Host ""
    dotnet test CharacterManager.Tests `
        --collect:"XPlat Code Coverage" `
        --settings CharacterManager.Tests/coverlet.runsettings `
        --verbosity minimal
} else {
    Write-Host "Mode: Test normal" -ForegroundColor Cyan
    Write-Host ""
    dotnet test CharacterManager.Tests --verbosity minimal
}

Write-Host ""
Write-Host "Resume des resultats ci-dessus" -ForegroundColor Green
