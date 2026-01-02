@echo off
REM Script pour compiler l'installateur Inno Setup
REM Usage: Compile-Installer.bat

setlocal

echo.
echo ============================================================
echo   Inno Setup Installer Compiler
echo ============================================================
echo.

REM Chercher iscc.exe aux emplacements courants
set ISCC_PATH=

if exist "C:\Program Files (x86)\Inno Setup 6\iscc.exe" (
    set ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\iscc.exe
    echo [OK] Found: C:\Program Files (x86)\Inno Setup 6
    goto FOUND
)

if exist "C:\Program Files (x86)\Inno Setup 5\iscc.exe" (
    set ISCC_PATH=C:\Program Files (x86)\Inno Setup 5\iscc.exe
    echo [OK] Found: C:\Program Files (x86)\Inno Setup 5
    goto FOUND
)

if exist "C:\Program Files\Inno Setup 6\iscc.exe" (
    set ISCC_PATH=C:\Program Files\Inno Setup 6\iscc.exe
    echo [OK] Found: C:\Program Files\Inno Setup 6
    goto FOUND
)

if exist "C:\Program Files\Inno Setup 5\iscc.exe" (
    set ISCC_PATH=C:\Program Files\Inno Setup 5\iscc.exe
    echo [OK] Found: C:\Program Files\Inno Setup 5
    goto FOUND
)

REM Si on arrive ici, Inno Setup n'est pas trouvé
echo [FAIL] Inno Setup not found
echo.
echo Possible locations checked:
echo   - C:\Program Files (x86)\Inno Setup 6
echo   - C:\Program Files (x86)\Inno Setup 5
echo   - C:\Program Files\Inno Setup 6
echo   - C:\Program Files\Inno Setup 5
echo.
echo Solutions:
echo   1. Install Inno Setup: https://jrsoftware.org/isdl.php
echo   2. Or add Inno Setup to your PATH
echo.
pause
exit /b 1

:FOUND
if not exist "CharacterManager.iss" (
    echo [FAIL] File not found: CharacterManager.iss
    pause
    exit /b 1
)

echo.
echo Script: CharacterManager.iss
echo Compiler: %ISCC_PATH%
echo.
echo Compiling installer...
echo.

"%ISCC_PATH%" "CharacterManager.iss"

if %errorlevel% equ 0 (
    echo.
    echo [OK] Compilation successful!
    echo.
    echo Installer created:
    echo   Path: publish\installer\CharacterManager-0.12.0-Setup.exe
    echo.
    echo Next step: Run the installer
    echo   Command: publish\installer\CharacterManager-0.12.0-Setup.exe
) else (
    echo.
    echo [FAIL] Compilation failed with code: %errorlevel%
    pause
    exit /b 1
)

endlocal
