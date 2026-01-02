# Pre-Release Verification Checklist
# Ce script vérifie que tout est prêt pour la release v0.12.0

Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Character Manager v0.12.0 - Pre-Release Verification  ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

$passCount = 0
$failCount = 0

# Helper function
function Check-Item {
    param([string]$Name, [bool]$Result)
    if ($Result) {
        Write-Host "✅ $Name" -ForegroundColor Green
        $script:passCount++
    } else {
        Write-Host "❌ $Name" -ForegroundColor Red
        $script:failCount++
    }
}

# 1. Project Files
Write-Host "📋 Vérification des fichiers projet..." -ForegroundColor Yellow
Check-Item "Solution file" (Test-Path "CharacterManager.sln")
Check-Item "Project file" (Test-Path "CharacterManager\CharacterManager.csproj")
Check-Item "Inno Setup script" (Test-Path "CharacterManager.iss")

# 2. Build
Write-Host "`n🔨 Vérification de la compilation..." -ForegroundColor Yellow
Check-Item "Release build folder" (Test-Path "CharacterManager\bin\Release\net9.0")
Check-Item "Main DLL" (Test-Path "CharacterManager\bin\Release\net9.0\CharacterManager.dll")
Check-Item "Resource DLL" (Test-Path "CharacterManager\bin\Release\net9.0\CharacterManager.Resources.Interface.dll")

# 3. Tests
Write-Host "`n🧪 Vérification des tests..." -ForegroundColor Yellow
Check-Item "Test DLL compiled" (Test-Path "CharacterManager.Tests\bin\Release\net9.0\CharacterManager.Tests.dll")

# 4. Publication
Write-Host "`n📦 Vérification de la publication..." -ForegroundColor Yellow
Check-Item "Publish folder" (Test-Path "publish")
Check-Item "Published .exe" (Test-Path "publish\CharacterManager.exe")
Check-Item "Published wwwroot" (Test-Path "publish\wwwroot")

# 5. Resources
Write-Host "`n🎨 Vérification des ressources..." -ForegroundColor Yellow
Check-Item "Resource project compiled" (Test-Path "CharacterManager.Resources.Interface\bin\Release\net9.0")
Check-Item "Images folder" (Test-Path "CharacterManager.Resources.Interface\Images")

if (Test-Path "CharacterManager.Resources.Interface\Images") {
    $imageCount = @(Get-ChildItem "CharacterManager.Resources.Interface\Images" -Filter "*.png").Count
    Write-Host "   Found: $imageCount images" -ForegroundColor Gray
    Check-Item "Minimum 20 images" ($imageCount -ge 20)
}

# 6. Database
Write-Host "`n💾 Vérification de la base de données..." -ForegroundColor Yellow
Check-Item "Migrations folder" (Test-Path "CharacterManager\Migrations")
Check-Item "Recent migrations exist" ((Get-ChildItem "CharacterManager\Migrations\*.cs" | Measure-Object).Count -gt 10)

# 7. Documentation
Write-Host "`n📖 Vérification de la documentation..." -ForegroundColor Yellow
Check-Item "DEPLOYMENT.md" (Test-Path "DEPLOYMENT.md")
Check-Item "INSTALLATION_GUIDE.md" (Test-Path "INSTALLATION_GUIDE.md")
Check-Item "RELEASE_0.12.0.md" (Test-Path "RELEASE_0.12.0.md")

# 8. Scripts
Write-Host "`n🔧 Vérification des scripts..." -ForegroundColor Yellow
Check-Item "Deploy-Manager.ps1" (Test-Path "Deploy-Manager.ps1")
Check-Item "Publish-Setup.ps1" (Test-Path "Publish-Setup.ps1")
Check-Item "Deploy-Local.bat" (Test-Path "Deploy-Local.bat")

# 9. API
Write-Host "`n🌐 Vérification de l'API..." -ForegroundColor Yellow
Check-Item "ResourcesController" (Test-Path "CharacterManager\Server\Controllers\ResourcesController.cs")
Check-Item "InterfaceResourceManager" (Test-Path "CharacterManager.Resources.Interface\InterfaceResourceManager.cs")

# 10. Configuration
Write-Host "`n⚙️ Vérification de la configuration..." -ForegroundColor Yellow
Check-Item "appsettings.json" (Test-Path "CharacterManager\appsettings.json")
Check-Item "Program.cs" (Test-Path "CharacterManager\Program.cs")

# 11. Version check
Write-Host "`n🏷️ Vérification de la version..." -ForegroundColor Yellow

if (Test-Path "CharacterManager\CharacterManager.csproj") {
    $csproj = Get-Content "CharacterManager\CharacterManager.csproj" -Raw
    $hasVersion = $csproj -match '<Version>0\.12\.0</Version>'
    Check-Item "Version in .csproj" $hasVersion
}

if (Test-Path "CharacterManager.iss") {
    $iss = Get-Content "CharacterManager.iss" -Raw
    $hasVersion = $iss -match 'AppVersion=0\.12\.0'
    Check-Item "Version in .iss" $hasVersion
}

# Summary
Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    RÉSUMÉ FINAL                         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`n✅ Checks passed: $passCount" -ForegroundColor Green
Write-Host "❌ Checks failed: $failCount" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

$readyForRelease = ($failCount -eq 0)

if ($readyForRelease) {
    Write-Host "`n🎉 Application is READY FOR RELEASE!" -ForegroundColor Green
    Write-Host "`nProchaines étapes:" -ForegroundColor Yellow
    Write-Host "1. Exécuter les tests: .\Deploy-Manager.ps1 -Action test" -ForegroundColor Cyan
    Write-Host "2. Créer l'installateur: .\Deploy-Manager.ps1 -Action installer" -ForegroundColor Cyan
    Write-Host "3. Tester l'installateur: publish\installer\CharacterManager-0.12.0-Setup.exe" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "`n⚠️ Please fix the issues above" -ForegroundColor Yellow
    exit 1
}
