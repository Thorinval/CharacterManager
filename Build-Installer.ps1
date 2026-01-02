# Compile l'installateur Inno Setup
# Usage: .\Build-Installer.ps1

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "   Inno Setup Installer Compiler - Character Manager" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Chemin vers iscc.exe
$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# Vérifier que iscc existe
if (-not (Test-Path $isccPath)) {
    Write-Host "ERROR: Inno Setup not found at:" -ForegroundColor Red
    Write-Host "  $isccPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Download Inno Setup from: https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
    exit 1
}

Write-Host "Found: $isccPath" -ForegroundColor Green
Write-Host ""

# Vérifier que CharacterManager.iss existe
if (-not (Test-Path "CharacterManager.iss")) {
    Write-Host "ERROR: CharacterManager.iss not found" -ForegroundColor Red
    exit 1
}

Write-Host "Compiling: CharacterManager.iss" -ForegroundColor Yellow
Write-Host ""

# Compiler
& $isccPath "CharacterManager.iss"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "SUCCESS - Installer compiled!" -ForegroundColor Green
    Write-Host ""
    
    $installerPath = "publish\installer\CharacterManager-0.12.0-Setup.exe"
    if (Test-Path $installerPath) {
        $size = [Math]::Round((Get-Item $installerPath).Length / 1MB, 2)
        Write-Host "Location: $installerPath" -ForegroundColor Cyan
        Write-Host "Size: $size MB" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Next: Run the installer" -ForegroundColor Yellow
        Write-Host "  .\$installerPath" -ForegroundColor Cyan
    }
} else {
    Write-Host ""
    Write-Host "ERROR - Compilation failed (code: $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}

Write-Host ""
