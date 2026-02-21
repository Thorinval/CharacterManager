# PowerShell Profile for CharacterManager project
# This file sets up the environment for working with the project

# Configure UTF-8 encoding for proper output display
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'

Write-Host "CharacterManager environment loaded - UTF-8 encoding configured" -ForegroundColor Green
