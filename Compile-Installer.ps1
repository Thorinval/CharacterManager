# Script pour compiler l'installateur Inno Setup
# Usage: .\Compile-Installer.ps1

param(
    [string]$ScriptFile = "CharacterManager.iss"
)

Write-Host "`n╔════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Inno Setup Installer Compiler             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Chemins possibles pour iscc.exe
$possiblePaths = @(
    "C:\Program Files (x86)\Inno Setup 6\iscc.exe",
    "C:\Program Files (x86)\Inno Setup 5\iscc.exe",
    "C:\Program Files\Inno Setup 6\iscc.exe",
    "C:\Program Files\Inno Setup 5\iscc.exe"
)

# Chercher iscc.exe
$isccPath = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $isccPath = $path
        Write-Host "✅ Found Inno Setup at: $path" -ForegroundColor Green
        break
    }
}

if (-not $isccPath) {
    Write-Host "❌ Inno Setup not found in expected locations:" -ForegroundColor Red
    $possiblePaths | ForEach-Object { Write-Host "   - $_" -ForegroundColor Yellow }
    Write-Host ""
    Write-Host "⚙️ Possible Solutions:" -ForegroundColor Yellow
    Write-Host "1. Install Inno Setup from: https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
    Write-Host "2. Or manually verify installation path and update this script" -ForegroundColor Cyan
    Write-Host "3. Or add Inno Setup to your PATH environment variable" -ForegroundColor Cyan
    exit 1
}

# Vérifier que le fichier .iss existe
if (-not (Test-Path $ScriptFile)) {
    Write-Host "❌ File not found: $ScriptFile" -ForegroundColor Red
    exit 1
}

Write-Host "📋 Script: $ScriptFile" -ForegroundColor Cyan
Write-Host "🔨 Compiler: $isccPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Compiling installer..." -ForegroundColor Yellow
Write-Host ""

# Compiler l'installateur
& $isccPath $ScriptFile

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Compilation successful!" -ForegroundColor Green
    Write-Host ""
    
    $installerPath = "publish\installer\CharacterManager-Setup.exe"
    if (Test-Path $installerPath) {
        $size = (Get-Item $installerPath).Length / 1MB
        Write-Host "📦 Installer created:" -ForegroundColor Cyan
        Write-Host "   Path: $installerPath" -ForegroundColor Green
        Write-Host "   Size: $([Math]::Round($size, 2)) MB" -ForegroundColor Green
        Write-Host ""
        Write-Host "🚀 Next step: Run the installer to test" -ForegroundColor Yellow
        Write-Host "   Command: .\$installerPath" -ForegroundColor Cyan
    }
} else {
    Write-Host ""
    Write-Host "❌ Compilation failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    exit 1
}
