# PdfAutoPrint Pro v1.1.1 - Self-extracting installer builder
# Output: PdfAutoPrint_Pro_Setup_v1.1.1.bat (double-click to install)
# Format: ASCII batch header + __ZIP_DATA__ marker + binary ZIP payload

import os, shutil, zipfile

RELEASE_DIR = r"C:\Users\Administrator\WorkBuddy\2026-06-05-09-13-35\PdfAutoPrint.Pro\release"
APP_DIR      = os.path.join(RELEASE_DIR, "app")
GS_FILE      = os.path.join(RELEASE_DIR, "gs10040w64.exe")
DOTNET_FILE  = os.path.join(RELEASE_DIR, "windowsdesktop-runtime-8.0.27-win-x64.exe")
SETUP_BAT    = os.path.join(RELEASE_DIR, "PdfAutoPrint_Pro_Setup_v1.1.1.bat")

# ── Step 1: Build ZIP payload ──────────────────────────────────────
zip_path = os.path.join(RELEASE_DIR, "_setup_payload.zip")
print("Building ZIP payload...")

with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
    # App binaries
    if os.path.isdir(APP_DIR):
        for root, _, files in os.walk(APP_DIR):
            for f in files:
                fp = os.path.join(root, f)
                arcname = os.path.relpath(fp, RELEASE_DIR).replace("\\", "/")
                zf.write(fp, arcname)
                print(f"  + app/{os.path.basename(fp)}")
    # GhostScript
    if os.path.isfile(GS_FILE):
        zf.write(GS_FILE, "gs10040w64.exe")
        print("  + gs10040w64.exe (GhostScript 10.04.0)")
    # .NET Runtime
    if os.path.isfile(DOTNET_FILE):
        zf.write(DOTNET_FILE, "windowsdesktop-runtime-8.0.27-win-x64.exe")
        print("  + windowsdesktop-runtime-8.0.27-win-x64.exe (.NET 8)")
    # Install scripts
    for script in ["一键安装.ps1", "printer_setup.ps1"]:
        sp = os.path.join(RELEASE_DIR, script)
        if os.path.isfile(sp):
            zf.write(sp, script)
            print(f"  + {script}")

zip_size = os.path.getsize(zip_path)
print(f"  ZIP total: {zip_size // 1024 // 1024} MB")

# Read ZIP into memory
with open(zip_path, "rb") as f:
    zip_data = f.read()

# ── Step 2: Build ASCII-only batch header ─────────────────────────
# CRITICAL: Must be pure ASCII (no Chinese characters) for cmd.exe compatibility.
# The embedded PowerShell extraction script handles everything.

bat = r'''@echo off
setlocal enabledelayedexpansion
title PdfAutoPrint Pro v1.1.1 Setup

echo.
echo   ============================================
echo     PdfAutoPrint Pro v1.1.1 - Setup
echo     .NET 8 + GhostScript + Virtual Printer
echo   ============================================
echo.

:: Request admin
net session >nul 2>&1
if errorlevel 1 (
    echo   Requesting administrator privileges...
    powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

:: Create temp dir
set "TMPDIR=%TEMP%\PdfAutoPrint_Setup_%RANDOM%"
mkdir "%TMPDIR%" 2>nul

:: Extract ZIP payload from self
echo   [1/5] Extracting files...
powershell -ExecutionPolicy Bypass -Command "$b=[IO.File]::ReadAllBytes('%~f0');$m=[Text.Encoding]::ASCII.GetBytes('__ZIP_DATA__');$i=0;while($i -le $b.Length-$m.Length){$f=1;for($j=0;$j -lt $m.Length;$j++){if($b[$i+$j] -ne $m[$j]){$f=0;break}};if($f){break};$i++};$z=$b[($i+$m.Length)..($b.Length-1)];$p='%TMPDIR%\payload.zip';[IO.File]::WriteAllBytes($p,$z);Add-Type -A System.IO.Compression.FileSystem;[IO.Compression.ZipFile]::ExtractToDirectory($p,'%TMPDIR%');Remove-Item $p -Force;Write-Host '   Extraction OK'"

if not exist "%TMPDIR%\一键安装.ps1" (
    echo   [ERROR] Extraction failed. Please re-download.
    pause >nul
    exit /b 1
)

echo.
echo   [2/5] Installing .NET 8 Desktop Runtime...
if exist "%TMPDIR%\windowsdesktop-runtime-8.0.27-win-x64.exe" (
    "%TMPDIR%\windowsdesktop-runtime-8.0.27-win-x64.exe" /install /quiet /norestart
    echo    .NET 8 Runtime installed
) else (
    echo    Runtime not found, skipping
)

echo.
echo   [3/5] Installing GhostScript 10.04.0...
if exist "%TMPDIR%\gs10040w64.exe" (
    "%TMPDIR%\gs10040w64.exe" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
    echo    GhostScript installed
) else (
    echo    GhostScript not found, skipping
)

echo.
echo   [4/5] Configuring virtual printer and app...
powershell -ExecutionPolicy Bypass -File "%TMPDIR%\一键安装.ps1" -Silent

echo.
echo   [5/5] Cleaning up...
rd /s /q "%TMPDIR%" 2>nul

echo.
echo   ============================================
echo     Installation complete!
echo     Shortcut created on Desktop
echo   ============================================
echo.
echo   Press any key to exit...
pause >nul
exit /b 0

__ZIP_DATA__
'''

# ── Step 3: Combine header + ZIP binary ──────────────────────────
# Write batch header as ASCII (Python will encode correctly, no BOM)
# Append raw ZIP bytes directly
with open(SETUP_BAT, "wb") as f:
    f.write(bat.encode("ascii", errors="replace"))
    f.write(zip_data)

os.remove(zip_path)
size_mb = os.path.getsize(SETUP_BAT) // 1024 // 1024
print(f"\nDone! {os.path.basename(SETUP_BAT)}")
print(f"Size: {size_mb} MB")
print(f"Usage: Right-click the .bat file -> Run as Administrator")
