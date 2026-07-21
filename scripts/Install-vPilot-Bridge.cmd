@echo off
setlocal
title EasyCPDLC vPilot Bridge Installer

echo EasyCPDLC vPilot Bridge Installer
echo Close vPilot before continuing.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-VPilotBridge.ps1"
set "INSTALL_EXIT=%ERRORLEVEL%"

echo.
if not "%INSTALL_EXIT%"=="0" (
    echo Bridge installation failed. Review the error above.
) else (
    echo Bridge installation completed successfully.
)
echo.
pause
exit /b %INSTALL_EXIT%
