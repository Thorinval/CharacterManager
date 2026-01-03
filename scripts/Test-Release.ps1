# Pre-Release Verification Checklist
# Ce script vérifie que tout est prêt pour la release v0.12.0

param(
    [switch]$Verbose
)

$ErrorActionPreference = 'Continue'
$checks = @()
$passCount = 0
$failCount = 0

function Add-Check {
    param(
        [string]$Name,
        [bool]$Result,
        [string]$Details = ""
    )
    
    $checks += @{
        Name = $Name
        Result = $Result
        Details = $Details
    }
    
    if ($Result) {
        Write-Host "✅ $Name" -ForegroundColor Green
        $script:passCount++
    } else {
        Write-Host "❌ $Name" -ForegroundColor Red
        $script:failCount++
    }
    
    if ($Details -and $Verbose.IsPresent) {
        Write-Host "   📝 $Details" -ForegroundColor Gray
    }
}
}

Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Character Manager v0.12.0 - Pre-Release Verification  ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# 1. Project Files
Write-Host "📋 Vérification des fichiers projet..." -ForegroundColor Yellow

$solutionExists = Test-Path "CharacterManager.sln"
Add-Check "Solution file exists" $solutionExists

$csprojExists = Test-Path "CharacterManager\CharacterManager.csproj"
Add-Check "Project file exists" $csprojExists

$issExists = Test-Path "CharacterManager.iss"
Add-Check "Inno Setup script exists" $issExists

# 2. Build
Write-Host "`n🔨 Vérification de la compilation..." -ForegroundColor Yellow

$buildOutputExists = Test-Path "CharacterManager\bin\Release\net9.0"
Add-Check "Release build exists" $buildOutputExists

$dllExists = Test-Path "CharacterManager\bin\Release\net9.0\CharacterManager.dll"
Add-Check "Main DLL exists" $dllExists

$resourceDllExists = Test-Path "CharacterManager\bin\Release\net9.0\CharacterManager.Resources.Interface.dll"
Add-Check "Resource DLL exists" $resourceDllExists

# 3. Tests
Write-Host "`n🧪 Vérification des tests..." -ForegroundColor Yellow

$testDllExists = Test-Path "CharacterManager.Tests\bin\Release\net9.0\CharacterManager.Tests.dll"
Add-Check "Test DLL compiled" $testDllExists

# 4. Publication
Write-Host "`n📦 Vérification de la publication..." -ForegroundColor Yellow

$publishExists = Test-Path "publish"
Add-Check "Publish folder exists" $publishExists

$publishExeExists = Test-Path "publish\CharacterManager.exe"
Add-Check "Published .exe exists" $publishExeExists

$publishWwwrootExists = Test-Path "publish\wwwroot"
Add-Check "Published wwwroot exists" $publishWwwrootExists

# 5. Resources
Write-Host "`n🎨 Vérification des ressources..." -ForegroundColor Yellow

$resourcesFolder = Test-Path "CharacterManager.Resources.Interface\bin\Release\net9.0"
Add-Check "Resource project compiled" $resourcesFolder

$imagesFolder = Test-Path "CharacterManager.Resources.Interface\Images"
Add-Check "Images folder exists" $imagesFolder

# Count images
if (Test-Path "CharacterManager.Resources.Interface\Images") {
    $imageCount = @(Get-ChildItem "CharacterManager.Resources.Interface\Images" -Filter "*.png").Count
    $imageCheck = $imageCount -ge 20
    Add-Check "Minimum 20 images embedded" $imageCheck "Found: $imageCount images"
}

# 6. Database
Write-Host "`n💾 Vérification de la base de données..." -ForegroundColor Yellow

$migrationsFolder = Test-Path "CharacterManager\Migrations"
Add-Check "Migrations folder exists" $migrationsFolder

$recentMigration = Test-Path "CharacterManager\Migrations\*_Capacities*.cs"
Add-Check "Capacities migration exists" $recentMigration

# 7. Documentation
Write-Host "`n📖 Vérification de la documentation..." -ForegroundColor Yellow

$deploymentDoc = Test-Path "DEPLOYMENT.md"
Add-Check "DEPLOYMENT.md exists" $deploymentDoc

$installationDoc = Test-Path "INSTALLATION_GUIDE.md"
Add-Check "INSTALLATION_GUIDE.md exists" $installationDoc

$releaseDoc = Test-Path "RELEASE_0.12.0.md"
Add-Check "RELEASE_0.12.0.md exists" $releaseDoc

# 8. Scripts
Write-Host "`n🔧 Vérification des scripts de déploiement..." -ForegroundColor Yellow

$deployManager = Test-Path "Deploy-Manager.ps1"
Add-Check "Deploy-Manager.ps1 exists" $deployManager

$publishSetup = Test-Path "Publish-Setup.ps1"
Add-Check "Publish-Setup.ps1 exists" $publishSetup

$deployLocal = Test-Path "Deploy-Local.bat"
Add-Check "Deploy-Local.bat exists" $deployLocal

# 9. API Functionality
Write-Host "`n🌐 Vérification de l'API..." -ForegroundColor Yellow

$resourcesControllerExists = Test-Path "CharacterManager\Server\Controllers\ResourcesController.cs"
Add-Check "ResourcesController exists" $resourcesControllerExists

$resourcesManagerExists = Test-Path "CharacterManager.Resources.Interface\InterfaceResourceManager.cs"
Add-Check "InterfaceResourceManager exists" $resourcesManagerExists

# 10. Configuration
Write-Host "`n⚙️ Vérification de la configuration..." -ForegroundColor Yellow

$appsettingsExists = Test-Path "CharacterManager\appsettings.json"
Add-Check "appsettings.json exists" $appsettingsExists

$programCsExists = Test-Path "CharacterManager\Program.cs"
Add-Check "Program.cs exists" $programCsExists

# 11. Version
Write-Host "`n🏷️ Vérification de la version..." -ForegroundColor Yellow

if (Test-Path "CharacterManager\CharacterManager.csproj") {
    $csproj = Get-Content "CharacterManager\CharacterManager.csproj"
    $hasVersion = $csproj -match '<Version>0\.12\.0</Version>'
    Add-Check "Version is 0.12.0 in .csproj" $hasVersion
}

if (Test-Path "CharacterManager.iss") {
    $iss = Get-Content "CharacterManager.iss"
    $hasVersion = $iss -match 'AppVersion=0\.12\.1'
    Add-Check "Version is 0.12.1 in .iss" $hasVersion
}

# Summary
Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                     RÉSUMÉ                              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`n✅ Checks passed: $passCount" -ForegroundColor Green
Write-Host "❌ Checks failed: $failCount" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

$readyForRelease = ($failCount -eq 0)

if ($readyForRelease) {
    Write-Host "`n🎉 Application is READY FOR RELEASE!" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Run final tests: .\Deploy-Manager.ps1 -Action test"
    Write-Host "2. Create installer: .\Deploy-Manager.ps1 -Action installer"
    Write-Host "3. Test installer: publish\installer\CharacterManager-Setup.exe"
    Write-Host "4. Tag release: git tag v0.12.0"
    exit 0
} else {
    Write-Host "`n⚠️ Some checks failed. Please fix the issues above." -ForegroundColor Yellow
    exit 1
}
