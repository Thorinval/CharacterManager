# Script de migration des images de personnages vers la structure par dossiers
# Ce script copie les images depuis wwwroot/images/personnages vers CharacterManager.Resources.Personnages/Images
# organisées par dossier de personnage

param(
    [string]$SourceDir = ".\CharacterManager\wwwroot\images\personnages",
    [string]$TargetDir = ".\CharacterManager.Resources.Personnages\Images",
    [switch]$WhatIf
)

Write-Host "=== Migration des images de personnages ===" -ForegroundColor Cyan
Write-Host "Source: $SourceDir" -ForegroundColor Gray
Write-Host "Destination: $TargetDir" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $SourceDir)) {
    Write-Error "Le dossier source n'existe pas: $SourceDir"
    exit 1
}

if (-not (Test-Path $TargetDir)) {
    Write-Host "Création du dossier cible: $TargetDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
}

# Patterns pour les types d'images (dans l'ordre de priorité)
$suffixPatterns = @(
    "_small_portrait",
    "_small_select",
    "_header",
    ""  # fichier de base sans suffixe
)

# Récupérer tous les fichiers images (hors dossier adult)
$imageFiles = Get-ChildItem -Path $SourceDir -File | Where-Object { 
    ($_.Extension -eq ".png" -or $_.Extension -eq ".jpg") -and 
    $_.DirectoryName -notlike "*\adult*" 
}

Write-Host "Fichiers trouvés: $($imageFiles.Count)" -ForegroundColor Green

# Grouper les fichiers par personnage
$personnages = @{}

foreach ($file in $imageFiles) {
    $fileName = $file.Name.ToLower()
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($fileName)
    
    # Trouver le nom du personnage en retirant les suffixes connus
    $personnageNom = $baseName
    foreach ($suffix in $suffixPatterns) {
        if ($baseName.EndsWith($suffix)) {
            $personnageNom = $baseName.Substring(0, $baseName.Length - $suffix.Length)
            break
        }
    }
    
    if ($personnageNom) {
        # Convertir le nom en PascalCase pour le dossier (première lettre en majuscule)
        # Gérer les underscores et tirets
        $parts = $personnageNom -split '[_-]'
        $folderName = ($parts | ForEach-Object { 
            if ($_.Length -gt 0) {
                $_.Substring(0,1).ToUpper() + $_.Substring(1)
            }
        }) -join ''
        
        if (-not $personnages.ContainsKey($folderName)) {
            $personnages[$folderName] = @()
        }
        
        $personnages[$folderName] += $file
    } else {
        Write-Host "  [WARN] Impossible d'identifier le personnage pour: $fileName" -ForegroundColor Yellow
    }
}

Write-Host "Personnages identifiés: $($personnages.Count)" -ForegroundColor Green
Write-Host ""

# Créer les dossiers et copier les fichiers
$copiedCount = 0
$skippedCount = 0

foreach ($personnage in $personnages.Keys | Sort-Object) {
    $targetFolder = Join-Path $TargetDir $personnage
    
    if (-not (Test-Path $targetFolder)) {
        if ($WhatIf) {
            Write-Host "[WHATIF] Création du dossier: $personnage" -ForegroundColor Yellow
        } else {
            New-Item -ItemType Directory -Path $targetFolder -Force | Out-Null
            Write-Host "Dossier créé: $personnage" -ForegroundColor Green
        }
    }
    
    foreach ($file in $personnages[$personnage]) {
        $targetFile = Join-Path $targetFolder $file.Name
        
        if (Test-Path $targetFile) {
            Write-Host "  [SKIP] $($file.Name) (existe déjà)" -ForegroundColor Gray
            $skippedCount++
        } else {
            if ($WhatIf) {
                Write-Host "  [WHATIF] Copie: $($file.Name)" -ForegroundColor Yellow
            } else {
                Copy-Item -Path $file.FullName -Destination $targetFile -Force
                Write-Host "  [OK] $($file.Name)" -ForegroundColor Green
                $copiedCount++
            }
        }
    }
}

Write-Host ""
Write-Host "=== Résumé ===" -ForegroundColor Cyan
Write-Host "Fichiers copiés: $copiedCount" -ForegroundColor Green
Write-Host "Fichiers ignorés: $skippedCount" -ForegroundColor Yellow
Write-Host ""

if ($WhatIf) {
    Write-Host "Mode simulation (-WhatIf). Aucun fichier n'a été copié." -ForegroundColor Yellow
    Write-Host "Exécutez sans -WhatIf pour effectuer la migration réelle." -ForegroundColor Yellow
} else {
    Write-Host "Migration terminée avec succès!" -ForegroundColor Green
}
