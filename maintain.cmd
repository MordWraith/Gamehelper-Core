@echo off
REM Gamehelper Core - Wartungs-Assistent (GUI standardmaessig).
cd /d "%~dp0"
if "%~1"=="" (
    REM GUI: eigenes Fenster, kein schwarzes CMD offen lassen
    start "" powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0maintain.ps1" -Gui
    exit /b 0
)
if /i "%~1"=="-Console" (
    shift
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0maintain.ps1" -Console -Action Menu %*
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0maintain.ps1" %*
)
if errorlevel 1 (
    echo.
    echo maintain.cmd fehlgeschlagen. Taste druecken zum Schliessen.
    pause >nul
    exit /b 1
)
