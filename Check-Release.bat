@echo off
REM Pre-Release Verification Checklist for Character Manager v0.12.1

setlocal enabledelayedexpansion

set PASS=0
set FAIL=0

echo.
echo ============================================================
echo   Character Manager v0.12.1 - Pre-Release Verification
echo ============================================================
echo.

REM 1. Project Files
echo [1] Checking project files...
if exist CharacterManager.sln (echo [OK] Solution file exists) else (echo [FAIL] Solution file missing && set /a FAIL+=1)
if exist CharacterManager\CharacterManager.csproj (echo [OK] Project file exists) else (echo [FAIL] Project file missing && set /a FAIL+=1)
if exist CharacterManager.iss (echo [OK] Inno Setup script exists) else (echo [FAIL] Inno Setup script missing && set /a FAIL+=1)

REM 2. Build
echo.
echo [2] Checking build output...
if exist CharacterManager\bin\Release\net9.0 (echo [OK] Release build folder exists) else (echo [FAIL] Release build folder missing && set /a FAIL+=1)
if exist CharacterManager\bin\Release\net9.0\CharacterManager.dll (echo [OK] Main DLL exists) else (echo [FAIL] Main DLL missing && set /a FAIL+=1)
if exist CharacterManager\bin\Release\net9.0\CharacterManager.Resources.Interface.dll (echo [OK] Resource DLL exists) else (echo [FAIL] Resource DLL missing && set /a FAIL+=1)

REM 3. Tests
echo.
echo [3] Checking test compilation...
if exist CharacterManager.Tests\bin\Release\net9.0\CharacterManager.Tests.dll (echo [OK] Test DLL compiled) else (echo [FAIL] Test DLL missing && set /a FAIL+=1)

REM 4. Publication
echo.
echo [4] Checking publication...
if exist publish (echo [OK] Publish folder exists) else (echo [FAIL] Publish folder missing && set /a FAIL+=1)
if exist publish\CharacterManager.exe (echo [OK] Published EXE exists) else (echo [FAIL] Published EXE missing && set /a FAIL+=1)
if exist publish\wwwroot (echo [OK] Published wwwroot exists) else (echo [FAIL] Published wwwroot missing && set /a FAIL+=1)

REM 5. Resources
echo.
echo [5] Checking resources...
if exist CharacterManager.Resources.Interface\bin\Release\net9.0 (echo [OK] Resource project compiled) else (echo [FAIL] Resource project not compiled && set /a FAIL+=1)
if exist CharacterManager.Resources.Interface\Images (echo [OK] Images folder exists) else (echo [FAIL] Images folder missing && set /a FAIL+=1)

REM 6. Database
echo.
echo [6] Checking database...
if exist CharacterManager\Migrations (echo [OK] Migrations folder exists) else (echo [FAIL] Migrations folder missing && set /a FAIL+=1)

REM 7. Documentation
echo.
echo [7] Checking documentation...
if exist DEPLOYMENT.md (echo [OK] DEPLOYMENT.md exists) else (echo [FAIL] DEPLOYMENT.md missing && set /a FAIL+=1)
if exist INSTALLATION_GUIDE.md (echo [OK] INSTALLATION_GUIDE.md exists) else (echo [FAIL] INSTALLATION_GUIDE.md missing && set /a FAIL+=1)
if exist RELEASE_0.12.1.md (echo [OK] RELEASE_0.12.1.md exists) else (echo [FAIL] RELEASE_0.12.1.md missing && set /a FAIL+=1)

REM 8. Scripts
echo.
echo [8] Checking deployment scripts...
if exist Deploy-Manager.ps1 (echo [OK] Deploy-Manager.ps1 exists) else (echo [FAIL] Deploy-Manager.ps1 missing && set /a FAIL+=1)
if exist Deploy-Local.bat (echo [OK] Deploy-Local.bat exists) else (echo [FAIL] Deploy-Local.bat missing && set /a FAIL+=1)

REM 9. API
echo.
echo [9] Checking API...
if exist CharacterManager\Server\Controllers\ResourcesController.cs (echo [OK] ResourcesController exists) else (echo [FAIL] ResourcesController missing && set /a FAIL+=1)

REM 10. Configuration
echo.
echo [10] Checking configuration...
if exist CharacterManager\appsettings.json (echo [OK] appsettings.json exists) else (echo [FAIL] appsettings.json missing && set /a FAIL+=1)
if exist CharacterManager\Program.cs (echo [OK] Program.cs exists) else (echo [FAIL] Program.cs missing && set /a FAIL+=1)

REM Summary
echo.
echo ============================================================
echo   SUMMARY
echo ============================================================
echo Checks failed: %FAIL%
echo.

if %FAIL% equ 0 (
    echo SUCCESS - Application is READY FOR RELEASE!
    echo.
    echo Next steps:
    echo   1. Run tests: .\Deploy-Manager.ps1 -Action test
    echo   2. Create installer: .\Deploy-Manager.ps1 -Action installer
    echo   3. Test installer: publish\installer\CharacterManager-Setup.exe
    exit /b 0
) else (
    echo FAILED - Please fix the issues above before release
    exit /b 1
)
