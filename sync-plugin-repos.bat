@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sync-plugin-repos.ps1" %*
pause
