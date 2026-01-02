@echo off
REM Script de déploiement simple pour Windows
REM Usage: Deploy-Local.bat [port]

setlocal enabledelayedexpansion

set PORT=5000
if not "%1"=="" set PORT=%1

set SCRIPT_DIR=%~dp0
set APP_DIR=%SCRIPT_DIR%CharacterManager\bin\Release\net9.0

cls
echo =========================================
echo Character Manager - Starting Application
echo =========================================
echo.
echo Application URL: http://localhost:%PORT%
echo Press Ctrl+C to stop
echo.
echo.

cd /d "%SCRIPT_DIR%CharacterManager"

REM Publier l'application en Release
echo Building and publishing application...
dotnet publish -c Release --self-contained -o "%SCRIPT_DIR%publish"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Publication failed!
    pause
    exit /b 1
)

REM Démarrer l'application
echo.
echo Starting application on port %PORT%...
echo.

set ASPNETCORE_URLS=http://localhost:%PORT%
"%SCRIPT_DIR%publish\CharacterManager.exe"

REM Garder la fenêtre ouverte en cas d'erreur
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Application exited with code %errorlevel%
    pause
)

endlocal
