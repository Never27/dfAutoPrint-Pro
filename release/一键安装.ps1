# PdfAutoPrint Pro - 一键安装引导脚本
# 被自解压安装包调用
# 支持 Win7 / Win10 / Win11

param([switch]$Silent)

$ErrorActionPreference = "Continue"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  PdfAutoPrint Pro v1.1.2 - 一键安装" -ForegroundColor Cyan
Write-Host "  Win7 / Win10 / Win11 通用" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 0. 检查管理员权限
# ============================================
Write-Host "[1/6] 检查管理员权限..." -ForegroundColor Yellow
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "需要管理员权限，正在重新启动..." -ForegroundColor Red
    Start-Process powershell -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit 0
}
Write-Host "  OK: 以管理员身份运行" -ForegroundColor Green

# ============================================
# 1. 检查/安装 .NET 8 Desktop Runtime
# ============================================
Write-Host "[2/6] 检查 .NET 8 桌面运行时..." -ForegroundColor Yellow

$dotnetOk = $false
try {
    $dotnetRuntime = Get-ChildItem "HKLM:\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost" -ErrorAction SilentlyContinue
    if ($dotnetRuntime) { $dotnetOk = $true }
} catch {}

if (-not $dotnetOk) {
    $dotnetInstaller = Join-Path $ScriptDir "windowsdesktop-runtime-8.0.27-win-x64.exe"
    if (Test-Path $dotnetInstaller) {
        Write-Host "  正在安装 .NET 8 桌面运行时..." -ForegroundColor Yellow
        $proc = Start-Process -FilePath $dotnetInstaller -ArgumentList "/install /quiet /norestart" -Wait -PassThru
        if ($proc.ExitCode -eq 0) { Write-Host "  OK: .NET 8 安装完成" -ForegroundColor Green }
        else { Write-Host "  WARN: .NET 8 安装失败 (可能需要 Win10+)" -ForegroundColor Red }
    } else {
        Write-Host "  WARN: 未找到 .NET 8 安装包" -ForegroundColor Red
    }
} else {
    Write-Host "  OK: .NET 8 已安装" -ForegroundColor Green
}

# ============================================
# 2. 安装 GhostScript
# ============================================
Write-Host "[3/6] 安装 GhostScript PDF 引擎..." -ForegroundColor Yellow

$gsFound = Test-Path "C:\Program Files\gs\gs10.04.0\bin\gswin64c.exe"
if (-not $gsFound) {
    $gsInstaller = Join-Path $ScriptDir "gs10040w64.exe"
    if (Test-Path $gsInstaller) {
        Write-Host "  正在安装 GhostScript 10.04.0..." -ForegroundColor Yellow
        Start-Process -FilePath $gsInstaller -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" -Wait
        if (Test-Path "C:\Program Files\gs\gs10.04.0\bin\gswin64c.exe") {
            Write-Host "  OK: GhostScript 安装完成" -ForegroundColor Green
        } else {
            Write-Host "  WARN: GhostScript 安装可能失败，手动安装: https://ghostscript.com" -ForegroundColor Red
        }
    } else {
        Write-Host "  WARN: 未找到 GhostScript 安装包，跳过" -ForegroundColor Red
    }
} else {
    Write-Host "  OK: GhostScript 已安装" -ForegroundColor Green
}

# ============================================
# 3. 安装打印驱动 + 虚拟打印机
# ============================================
Write-Host "[4/6] 配置虚拟 PDF 打印机..." -ForegroundColor Yellow

$outputDir = "C:\PDFOutput"
$spoolDir = "C:\PDFOutput\spool"
$portName = "C:\PDFOutput\spool\printjob.prn"
$printerName = "Auto PDF Printer"

# 创建目录
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
New-Item -ItemType Directory -Path $spoolDir -Force | Out-Null

# 安装驱动
$driverName = "Microsoft PS Class Driver"
$driverOk = Get-PrinterDriver -Name $driverName -ErrorAction SilentlyContinue
if (-not $driverOk) {
    try { Add-PrinterDriver -Name $driverName -ErrorAction Stop }
    catch {
        try { Add-PrinterDriver -Name "MS Publisher Imagesetter" -ErrorAction Stop; $driverName = "MS Publisher Imagesetter" }
        catch { Write-Host "  WARN: 无法安装打印驱动" -ForegroundColor Red }
    }
}
Write-Host "  OK: 打印驱动就绪 ($driverName)" -ForegroundColor Green

# 移除旧打印机
Get-Printer -Name $printerName -ErrorAction SilentlyContinue | Remove-Printer -ErrorAction SilentlyContinue

# 创建端口
if (-not (Get-PrinterPort -Name $portName -ErrorAction SilentlyContinue)) {
    Add-PrinterPort -Name $portName -ErrorAction SilentlyContinue
}

# 创建打印机
try {
    Add-Printer -Name $printerName -DriverName $driverName -PortName $portName -ErrorAction Stop
    Write-Host "  OK: 虚拟打印机创建成功: $printerName" -ForegroundColor Green
} catch {
    Write-Host "  WARN: 无法创建打印机: $_" -ForegroundColor Red
}

# ============================================
# 4. 复制软件文件
# ============================================
Write-Host "[5/6] 安装管理软件..." -ForegroundColor Yellow

$installPath = "C:\Program Files\PdfAutoPrint Pro"
New-Item -ItemType Directory -Path $installPath -Force | Out-Null

# 复制 app 文件
$appSrc = Join-Path $ScriptDir "app"
if (Test-Path $appSrc) {
    Copy-Item -Path "$appSrc\*" -Destination $installPath -Recurse -Force
    Write-Host "  OK: 软件已安装到 $installPath" -ForegroundColor Green
}

# ============================================
# 5. 创建快捷方式
# ============================================
Write-Host "[6/6] 创建快捷方式..." -ForegroundColor Yellow

$targetExe = Join-Path $installPath "PdfAutoPrint.Pro.exe"
$WshShell = New-Object -ComObject WScript.Shell

# 桌面
$desktopLink = Join-Path ([Environment]::GetFolderPath("Desktop")) "PdfAutoPrint Pro.lnk"
$sc = $WshShell.CreateShortcut($desktopLink)
$sc.TargetPath = $targetExe
$sc.WorkingDirectory = $installPath
$sc.Description = "PdfAutoPrint Pro - 虚拟 PDF 打印机管理器"
$sc.IconLocation = "$targetExe,0"
$sc.Save()

# 开始菜单
$startLink = Join-Path ([Environment]::GetFolderPath("CommonPrograms")) "PdfAutoPrint Pro.lnk"
$sc = $WshShell.CreateShortcut($startLink)
$sc.TargetPath = $targetExe
$sc.WorkingDirectory = $installPath
$sc.Description = "PdfAutoPrint Pro - 虚拟 PDF 打印机管理器"
$sc.IconLocation = "$targetExe,0"
$sc.Save()

Write-Host "  OK: 快捷方式已创建" -ForegroundColor Green

# ============================================
# 完成
# ============================================
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  安装完成!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "  软件位置: $installPath" -ForegroundColor White
Write-Host "  输出目录: C:\PDFOutput\{date}\" -ForegroundColor White
Write-Host ""
Write-Host "  使用方法:" -ForegroundColor White
Write-Host "    1. 双击桌面 'PdfAutoPrint Pro' 图标" -ForegroundColor White
Write-Host "    2. 点击 'Start All' 开始监听" -ForegroundColor White
Write-Host "    3. 打印到 'Auto PDF Printer'" -ForegroundColor White
Write-Host ""
Write-Host "  作者: 和学斌  QQ: 1210696000" -ForegroundColor White
Write-Host ""
if (-not $Silent) {
    Write-Host "按任意键退出..." -ForegroundColor Gray
    Read-Host
}
