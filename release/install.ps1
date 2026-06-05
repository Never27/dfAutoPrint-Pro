# Install Script for PdfAutoPrint Pro
# 一键安装脚本：GhostScript + 打印机驱动 + 虚拟打印机 + 管理软件
# Requires: Administrator privileges
# Author: 和学斌
# Contact: QQ 1210696000

param(
    [switch]$Silent,
    [switch]$NoPrinter,
    [string]$InstallPath = "${env:ProgramFiles}\PdfAutoPrint Pro"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppDir = Join-Path $ScriptDir "app"
$GhostScriptUrl = "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10040/gs10040w64.exe"
$GhostScriptInstaller = Join-Path $env:TEMP "gs10040w64.exe"
$Version = "1.1.2"

# Colors
function Write-Info { Write-Host "[INFO] $args" -ForegroundColor Cyan }
function Write-Success { Write-Host "[OK] $args" -ForegroundColor Green }
function Write-Warn { Write-Host "[WARN] $args" -ForegroundColor Yellow }
function Write-Error { Write-Host "[ERROR] $args" -ForegroundColor Red }
function Write-Step { Write-Host "`n==> $args" -ForegroundColor White }

# ============================================================
# Step 0: Check Admin
# ============================================================
Write-Step "Step 0: Checking administrator privileges..."
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "This installer requires Administrator privileges."
    Write-Info "Please run PowerShell as Administrator and try again."
    exit 1
}
Write-Success "Running as Administrator"

Write-Host ""
Write-Host "============================================" -ForegroundColor Magenta
Write-Host "  PdfAutoPrint Pro v$Version Installer" -ForegroundColor Magenta
Write-Host "  Author: 和学斌  QQ: 1210696000" -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta
Write-Host ""

# ============================================================
# Step 1: Check .NET Runtime
# ============================================================
Write-Step "Step 1: Checking .NET 8 Desktop Runtime..."
$dotnetRuntime = Get-ChildItem "HKLM:\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost" -ErrorAction SilentlyContinue | 
    Where-Object { $_.PSChildName -like "*8.*" }
if (-not $dotnetRuntime) {
    Write-Warn ".NET 8 Desktop Runtime not found."
    Write-Info "Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
    Write-Info "Select: .NET Desktop Runtime 8.0 (Windows x64)"
    if (-not $Silent) {
        $installDotnet = Read-Host "Download and install .NET 8 now? (y/n)"
        if ($installDotnet -eq "y") {
            Start-Process "https://dotnet.microsoft.com/download/dotnet/8.0"
            Write-Info "After installing .NET 8, re-run this installer."
            exit 0
        }
    }
} else {
    Write-Success ".NET 8 Desktop Runtime found"
}

# ============================================================
# Step 2: Install GhostScript
# ============================================================
Write-Step "Step 2: Checking GhostScript..."

$gsFound = $false
$gsPaths = @(
    "${env:ProgramFiles}\gs\gs10.04.0\bin\gswin64c.exe",
    "${env:ProgramFiles(x86)}\gs\gs10.04.0\bin\gswin32c.exe",
    "${env:ProgramFiles}\gs\gs10.03.1\bin\gswin64c.exe"
)

foreach ($path in $gsPaths) {
    if (Test-Path $path) {
        Write-Success "GhostScript found: $path"
        $gsFound = $true
        break
    }
}

if (-not $gsFound) {
    Write-Warn "GhostScript not found. Attempting to download..."
    try {
        Write-Info "Downloading GhostScript 10.04.0 (64-bit)..."
        Invoke-WebRequest -Uri $GhostScriptUrl -OutFile $GhostScriptInstaller -UseBasicParsing
        
        Write-Info "Installing GhostScript (silent)..."
        Start-Process -FilePath $GhostScriptInstaller -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" -Wait -NoNewWindow
        
        Remove-Item $GhostScriptInstaller -Force -ErrorAction SilentlyContinue
        
        if (Test-Path "${env:ProgramFiles}\gs\gs10.04.0\bin\gswin64c.exe") {
            Write-Success "GhostScript installed successfully"
        } else {
            Write-Warn "GhostScript may have installed to a different location."
            Write-Info "Please install GhostScript manually from: https://ghostscript.com/releases/gsdnld.html"
        }
    } catch {
        Write-Warn "Could not download GhostScript automatically."
        Write-Info "Please install GhostScript manually from: https://ghostscript.com/releases/gsdnld.html"
    }
}

# ============================================================
# Step 3: Install Print Driver
# ============================================================
Write-Step "Step 3: Installing print driver..."

$driverName = "Microsoft PS Class Driver"
$driverInstalled = Get-PrinterDriver -Name $driverName -ErrorAction SilentlyContinue

if (-not $driverInstalled) {
    Write-Info "Installing $driverName..."
    try {
        Add-PrinterDriver -Name $driverName -ErrorAction Stop
        Write-Success "Print driver installed"
    } catch {
        Write-Warn "Could not install PS Class Driver automatically."
        Write-Info "Trying alternative: MS Publisher Imagesetter..."
        try {
            Add-PrinterDriver -Name "MS Publisher Imagesetter" -ErrorAction Stop
            $driverName = "MS Publisher Imagesetter"
            Write-Success "Using MS Publisher Imagesetter driver"
        } catch {
            Write-Warn "Could not install any PS driver."
            Write-Info "Virtual printer creation will be skipped."
            Write-Info "You can manually add a printer driver via Windows Settings."
        }
    }
} else {
    Write-Success "Print driver already installed: $driverName"
}

# ============================================================
# Step 4: Create Output Directories
# ============================================================
Write-Step "Step 4: Creating output directories..."

$outputRoot = "C:\PDFOutput"
$spoolDir = Join-Path $outputRoot "spool"

if (-not (Test-Path $outputRoot)) {
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
    Write-Success "Created: $outputRoot"
}
if (-not (Test-Path $spoolDir)) {
    New-Item -ItemType Directory -Path $spoolDir -Force | Out-Null
    Write-Success "Created: $spoolDir"
}

# ============================================================
# Step 5: Create Virtual Printer
# ============================================================
if (-not $NoPrinter -and $driverInstalled) {
    Write-Step "Step 5: Creating virtual PDF printer..."

    $printerName = "Auto PDF Printer"
    $portName = "C:\PDFOutput\spool\printjob.prn"
    $existingPrinter = Get-Printer -Name $printerName -ErrorAction SilentlyContinue

    if ($existingPrinter) {
        Write-Warn "Printer '$printerName' already exists. Removing..."
        Remove-Printer -Name $printerName -ErrorAction SilentlyContinue
    }

    try {
        # Create local port
        $portExists = Get-PrinterPort -Name $portName -ErrorAction SilentlyContinue
        if (-not $portExists) {
            Add-PrinterPort -Name $portName -ErrorAction Stop
        }
        
        # Create printer
        Add-Printer -Name $printerName -DriverName $driverName -PortName $portName -ErrorAction Stop
        
        Write-Success "Virtual printer created: $printerName"
        Write-Info "  Port: $portName"
        Write-Info "  Driver: $driverName"
        Write-Info ""
        Write-Info "To print to PDF: Select '$printerName' in any application's Print dialog."
    } catch {
        Write-Warn "Could not create virtual printer."
        Write-Info "Error: $_"
        Write-Info "You can create it manually via the PdfAutoPrint Pro app later."
    }
}

# ============================================================
# Step 6: Install Management Software
# ============================================================
Write-Step "Step 6: Installing management software..."

# Create install directory
if (Test-Path $InstallPath) {
    Write-Info "Removing previous installation..."
    Remove-Item $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

# Copy app files
if (Test-Path $AppDir) {
    Copy-Item -Path "$AppDir\*" -Destination $InstallPath -Recurse -Force
    Write-Success "App files copied to: $InstallPath"
} else {
    Write-Error "App directory not found: $AppDir"
    Write-Info "Please ensure 'app' folder is in the same directory as this script."
    exit 1
}

# Create desktop shortcut
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "PdfAutoPrint Pro.lnk"
$targetPath = Join-Path $InstallPath "PdfAutoPrint.Pro.exe"

if (Test-Path $targetPath) {
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($shortcutPath)
    $Shortcut.TargetPath = $targetPath
    $Shortcut.WorkingDirectory = $InstallPath
    $Shortcut.Description = "PdfAutoPrint Pro - Virtual PDF Printer Manager"
    $Shortcut.IconLocation = "$targetPath,0"
    $Shortcut.Save()
    Write-Success "Desktop shortcut created"
}

# Create Start Menu shortcut
$startMenuPath = [Environment]::GetFolderPath("CommonPrograms")
$startShortcutPath = Join-Path $startMenuPath "PdfAutoPrint Pro.lnk"
if (Test-Path $targetPath) {
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($startShortcutPath)
    $Shortcut.TargetPath = $targetPath
    $Shortcut.WorkingDirectory = $InstallPath
    $Shortcut.Description = "PdfAutoPrint Pro - Virtual PDF Printer Manager"
    $Shortcut.IconLocation = "$targetPath,0"
    $Shortcut.Save()
    Write-Success "Start Menu shortcut created"
}

# ============================================================
# Step 7: Final Summary
# ============================================================
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Info "Software location: $InstallPath"
Write-Info "Configuration: %LocalAppData%\PdfAutoPrint.Pro\"
Write-Info ""
Write-Info "Quick Start:"
Write-Info "  1. Launch 'PdfAutoPrint Pro' from Desktop or Start Menu"
Write-Info "  2. Click 'Start All' to begin monitoring"
Write-Info "  3. Print any document to 'Auto PDF Printer'"
Write-Info "  4. PDF appears in: $outputRoot\{date}\"
Write-Host ""
Write-Info "Author: 和学斌  QQ: 1210696000"
Write-Host ""
Write-Info "Press any key to exit..."
if (-not $Silent) {
    Read-Host
}
