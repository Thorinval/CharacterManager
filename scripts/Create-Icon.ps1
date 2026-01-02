Add-Type -AssemblyName System.Drawing
$png = "..\CharacterManager.Resources.Interface\Images\favicon.png"
$ico = "..\CharacterManager\CharacterManager.ico"

if (-not (Test-Path $png)) {
    Write-Error "PNG not found: $png"
    exit 1
}

$bitmap = [System.Drawing.Bitmap]::FromFile($png)
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())

$fs = [System.IO.File]::Create($ico)
$icon.Save($fs)
$fs.Close()

$icon.Dispose()
$bitmap.Dispose()

Write-Host "Icon generated at $ico"