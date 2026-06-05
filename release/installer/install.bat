@echo off
setlocal enabledelayedexpansion
title PdfAutoPrint Pro Installer

:: Get the directory where this script is located
set "SCRIPT_DIR=%~dp0"
set "INSTALL_DIR=C:\Program Files\PdfAutoPrint Pro"

echo.
echo ============================================
echo   PdfAutoPrint Pro v1.1.1 Installer
echo ============================================
echo.
echo Installing to: %INSTALL_DIR%
echo.

:: Check admin privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [WARN] Administrator privileges required.
    echo Attempting to restart with admin rights...
    powershell -Command "Start-Process '%~f0' -Verb RunAs -WorkingDirectory '%~dp0'"
    exit /b
)

:: Stop existing instance
taskkill /F /IM PdfAutoPrint.Pro.exe >nul 2>&1

:: Create installation directory
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Copy all files
echo [1/4] Copying program files...
xcopy "%SCRIPT_DIR%*" "%INSTALL_DIR%\" /E /Y /Q /I >nul 2>&1
del "%INSTALL_DIR%\install.bat" /Q >nul 2>&1
del "%INSTALL_DIR%\sfx_config.txt" /Q >nul 2>&1

:: Create desktop shortcut
echo [2/4] Creating shortcuts...
powershell -NoProfile -Command ^
  "$WshShell = New-Object -ComObject WScript.Shell; ^
   $Shortcut = $WshShell.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\PdfAutoPrint Pro.lnk'); ^
   $Shortcut.TargetPath = '%INSTALL_DIR%\PdfAutoPrint.Pro.exe'; ^
   $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; ^
   $Shortcut.IconLocation = '%INSTALL_DIR%\Assets\app.ico'; ^
   $Shortcut.Save()"

:: Create Start Menu shortcut
powershell -NoProfile -Command ^
  "$WshShell = New-Object -ComObject WScript.Shell; ^
   $StartMenu = [Environment]::GetFolderPath('Programs') + '\PdfAutoPrint Pro'; ^
   if (-not (Test-Path $StartMenu)) { New-Item -ItemType Directory -Path $StartMenu -Force }; ^
   $Shortcut = $WshShell.CreateShortcut($StartMenu + '\PdfAutoPrint Pro.lnk'); ^
   $Shortcut.TargetPath = '%INSTALL_DIR%\PdfAutoPrint.Pro.exe'; ^
   $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; ^
   $Shortcut.IconLocation = '%INSTALL_DIR%\Assets\app.ico'; ^
   $Shortcut.Save()"

:: Register uninstall info
echo [3/4] Registering uninstall info...
powershell -NoProfile -Command ^
  "$regPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PdfAutoPrintPro'; ^
   New-Item -Path $regPath -Force | Out-Null; ^
   Set-ItemProperty -Path $regPath -Name 'DisplayName' -Value 'PdfAutoPrint Pro'; ^
   Set-ItemProperty -Path $regPath -Name 'DisplayVersion' -Value '1.1.1'; ^
   Set-ItemProperty -Path $regPath -Name 'Publisher' -Value 'Never27'; ^
   Set-ItemProperty -Path $regPath -Name 'DisplayIcon' -Value '%INSTALL_DIR%\Assets\app.ico'; ^
   Set-ItemProperty -Path $regPath -Name 'UninstallString' -Value 'cmd /c rmdir /S /Q \"%INSTALL_DIR%\"'; ^
   Set-ItemProperty -Path $regPath -Name 'InstallLocation' -Value '%INSTALL_DIR%'; ^
   Set-ItemProperty -Path $regPath -Name 'NoModify' -Value 1; ^
   Set-ItemProperty -Path $regPath -Name 'NoRepair' -Value 1"

echo [4/4] Installation complete!
echo.
echo ============================================
echo   PdfAutoPrint Pro has been installed!
echo   Desktop shortcut created.
echo.
echo   To create a virtual printer, run the app
echo   and click "Create Printer" in settings.
echo ============================================
echo.
echo Press any key to finish...
pause >nul
