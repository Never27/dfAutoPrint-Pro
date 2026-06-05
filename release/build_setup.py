# PdfAutoPrint Pro v1.1.0 - 自解压安装包构建脚本
# 产出：PdfAutoPrint_Pro_Setup_v1.1.0.bat（双击运行）
# 原理：批处理脚本 + 嵌入 ZIP 数据，PowerShell 自解压

import os, shutil, zipfile

RELEASE_DIR = r"C:\Users\Administrator\WorkBuddy\2026-06-05-09-13-35\PdfAutoPrint.Pro\release"
APP_DIR      = os.path.join(RELEASE_DIR, "app")
GS_FILE      = os.path.join(RELEASE_DIR, "gs10040w64.exe")
DOTNET_FILE  = os.path.join(RELEASE_DIR, "windowsdesktop-runtime-8.0.27-win-x64.exe")
SETUP_BAT    = os.path.join(RELEASE_DIR, "PdfAutoPrint_Pro_Setup_v1.1.1.bat")

# 1. 创建 ZIP 包
zip_path = os.path.join(RELEASE_DIR, "_setup_payload.zip")
print("ZIP 打包安装文件...")

with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
    # app 目录
    if os.path.isdir(APP_DIR):
        for root, _, files in os.walk(APP_DIR):
            for f in files:
                fp = os.path.join(root, f)
                arcname = os.path.relpath(fp, RELEASE_DIR).replace("\\", "/")
                zf.write(fp, arcname)
    # GhostScript
    if os.path.isfile(GS_FILE):
        zf.write(GS_FILE, "gs10040w64.exe")
        print("  + GhostScript 10.04.0")
    else:
        print("  X GhostScript not found")
    # .NET Runtime
    if os.path.isfile(DOTNET_FILE):
        zf.write(DOTNET_FILE, "windowsdesktop-runtime-8.0.27-win-x64.exe")
        print("  + .NET 8 Desktop Runtime")
    else:
        print("  X .NET Runtime not found")
    # 安装脚本
    ps1 = os.path.join(RELEASE_DIR, "一键安装.ps1")
    if os.path.isfile(ps1):
        zf.write(ps1, "一键安装.ps1")
    print("  + 安装脚本")

zip_size = os.path.getsize(zip_path)
print(f"  ZIP size: {zip_size//1024//1024} MB")

# 2. 读取 ZIP 为字节，计算偏移
with open(zip_path, "rb") as f:
    zip_data = f.read()

# 3. 生成自解压批处理
# 结构：批处理脚本 + __ZIP_DATA__ 标记 + ZIP 二进制
bat = """@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title PdfAutoPrint Pro v1.1.0 安装向导

echo.
echo   ============================================
echo     PdfAutoPrint Pro v1.1.1 - 完整安装包
echo     包含 .NET 8 Runtime + GhostScript + 虚拟打印机
echo   ============================================
echo.

:: 请求管理员权限
net session >nul 2>&1
if errorlevel 1 (
    echo   [信息] 请求管理员权限...
    powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

:: 创建临时目录
set "TEMP_DIR=%TEMP%\\PdfAutoPrint_Setup_%RANDOM%"
mkdir "%TEMP_DIR%" 2>nul

:: 从自身提取 ZIP 数据
echo   [1/5] 正在解压安装文件...
powershell -ExecutionPolicy Bypass -Command ^
  "$self = Get-Content -Path '%~f0' -Encoding Byte -Raw; ^
   $marker = [System.Text.Encoding]::UTF8.GetBytes('__ZIP_DATA__'); ^
   $idx = -1; for($i=0; $i -lt $self.Length - $marker.Length; $i++) { ^
     $match = $true; for($j=0; $j -lt $marker.Length; $j++) { ^
       if($self[$i+$j] -ne $marker[$j]) { $match=$false; break } ^
     }; ^
     if($match) { $idx = $i + $marker.Length; break } ^
   }; ^
   if($idx -gt 0) { ^
     $zipBytes = $self[$idx..($self.Length-1)]; ^
     $zipPath = '%TEMP_DIR%\\payload.zip'; ^
     [System.IO.File]::WriteAllBytes($zipPath, $zipBytes); ^
     Add-Type -Assembly System.IO.Compression.FileSystem; ^
     [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, '%TEMP_DIR%'); ^
     Remove-Item $zipPath -Force; ^
     Write-Host '   解压完成' ^
   }"

:: 检查文件
if not exist "%TEMP_DIR%\\一键安装.ps1" (
    echo   [错误] 解压失败，请重新下载安装包！
    pause >nul
    exit /b 1
)

echo.
echo   [2/5] 安装 .NET 8 Desktop Runtime...
if exist "%TEMP_DIR%\\windowsdesktop-runtime-8.0.27-win-x64.exe" (
    "%TEMP_DIR%\\windowsdesktop-runtime-8.0.27-win-x64.exe" /install /quiet /norestart
    echo    .NET 8 Runtime 安装完成
) else (
    echo    未找到 Runtime 文件，跳过
)

echo.
echo   [3/5] 安装 GhostScript 10.04.0...
if exist "%TEMP_DIR%\\gs10040w64.exe" (
    "%TEMP_DIR%\\gs10040w64.exe" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
    echo    GhostScript 安装完成
) else (
    echo    未找到 GhostScript 文件，跳过
)

echo.
echo   [4/5] 配置虚拟打印机和应用...
powershell -ExecutionPolicy Bypass -File "%TEMP_DIR%\\一键安装.ps1" -Silent

echo.
echo   [5/5] 清理临时文件...
rd /s /q "%TEMP_DIR%" 2>nul

echo.
echo   ============================================
echo     安装完成！
echo     桌面已创建 PdfAutoPrint Pro 快捷方式
echo   ============================================
echo.
echo   按任意键退出...
pause >nul
exit /b 0

__ZIP_DATA__
"""

with open(SETUP_BAT, "wb") as f:
    f.write(bat.encode("utf-8"))
    f.write(zip_data)

os.remove(zip_path)
size_mb = os.path.getsize(SETUP_BAT) // 1024 // 1024
print(f"\nDone! {SETUP_BAT}")
print(f"Size: {size_mb} MB")
print(f"\nUsage: Right-click {os.path.basename(SETUP_BAT)} -> Run as Administrator")
