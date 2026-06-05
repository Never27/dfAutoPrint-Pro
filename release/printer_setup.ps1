# PdfAutoPrint Pro - 打印机配置脚本
# 由 Inno Setup 安装程序自动调用
# 功能：安装打印驱动 + 创建虚拟 PDF 打印机

$ErrorActionPreference = "Continue"
$printerName = "Auto PDF Printer"
$portName = "C:\PDFOutput\spool\printjob.prn"
$outputDir = "C:\PDFOutput"
$spoolDir = "C:\PDFOutput\spool"

# 确保输出目录存在
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}
if (-not (Test-Path $spoolDir)) {
    New-Item -ItemType Directory -Path $spoolDir -Force | Out-Null
}

# ============================================
# 1. 安装打印驱动
# ============================================
$driverName = "Microsoft PS Class Driver"
$driverInstalled = Get-PrinterDriver -Name $driverName -ErrorAction SilentlyContinue

if (-not $driverInstalled) {
    Write-Host "Installing $driverName..."
    try {
        Add-PrinterDriver -Name $driverName -ErrorAction Stop
        Write-Host "Driver installed: $driverName"
    } catch {
        Write-Host "PS Class Driver failed, trying MS Publisher Imagesetter..."
        try {
            Add-PrinterDriver -Name "MS Publisher Imagesetter" -ErrorAction Stop
            $driverName = "MS Publisher Imagesetter"
            Write-Host "Using: $driverName"
        } catch {
            Write-Host "WARNING: Could not install any PS driver."
            exit 0
        }
    }
}

# ============================================
# 2. 移除旧打印机（如果存在）
# ============================================
$existing = Get-Printer -Name $printerName -ErrorAction SilentlyContinue
if ($existing) {
    Remove-Printer -Name $printerName -ErrorAction SilentlyContinue
    Write-Host "Removed old printer: $printerName"
}

# ============================================
# 3. 创建本地打印机端口
# ============================================
$portExists = Get-PrinterPort -Name $portName -ErrorAction SilentlyContinue
if (-not $portExists) {
    Add-PrinterPort -Name $portName -ErrorAction Stop
    Write-Host "Created port: $portName"
}

# ============================================
# 4. 创建虚拟打印机
# ============================================
try {
    Add-Printer -Name $printerName -DriverName $driverName -PortName $portName -ErrorAction Stop
    Write-Host "SUCCESS: Virtual printer created: $printerName"
} catch {
    Write-Host "ERROR: Could not create printer: $_"
}
