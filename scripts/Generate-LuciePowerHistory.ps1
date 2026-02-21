# Generate Lucie power history (retroactive)
# Usage: .\Generate-LuciePowerHistory.ps1

Write-Host "=== Generating Lucie power history ==="

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$appDir = Join-Path $projectDir "CharacterManager"
$csxFile = Join-Path $scriptDir "GenerateLuciePowerHistory.csx"

# Checks
if (-not (Test-Path $csxFile)) {
    Write-Host "Script missing: $csxFile" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $appDir)) {
    Write-Host "Project folder missing: $appDir" -ForegroundColor Red
    exit 1
}

Write-Host "Checking dotnet-script..."
$hasScript = dotnet tool list -g | Select-String "dotnet-script"
if (-not $hasScript) {
    Write-Host "Installing dotnet-script..."
    dotnet tool install -g dotnet-script
    if ($LASTEXITCODE -ne 0) { exit 1 }
}
Write-Host "dotnet-script OK"

Push-Location $appDir
Write-Host "Working directory: $appDir"
Write-Host "Building project..."
dotnet build CharacterManager.csproj --configuration Debug --nologo
if ($LASTEXITCODE -ne 0) { Pop-Location; exit 1 }

Write-Host "Running script..."

dotnet script $csxFile --no-cache --verbosity diagnostic
$exitCode = $LASTEXITCODE

Pop-Location

if ($exitCode -eq 0) {
    Write-Host "=== Generation done ===" -ForegroundColor Green
} else {
    Write-Host "=== Generation failed ===" -ForegroundColor Red
}

exit $exitCode
