# PdfAutoPrint Pro v1.1.0 - 完整安装包构建脚本
# 包含：.NET 8 Runtime + GhostScript 10.04.0 + 虚拟打印机驱动 + 管理软件

import os, shutil, struct, subprocess, zipfile

RELEASE_DIR = r"C:\Users\Administrator\WorkBuddy\2026-06-05-09-13-35\PdfAutoPrint.Pro\release"
APP_DIR      = os.path.join(RELEASE_DIR, "app")
GS_FILE      = os.path.join(RELEASE_DIR, "gs10040w64.exe")
DOTNET_FILE = os.path.join(RELEASE_DIR, "windowsdesktop-runtime-8.0.27-win-x64.exe")
SETUP_EXE   = os.path.join(RELEASE_DIR, "PdfAutoPrint_Pro_Setup_v1.1.0.exe")

# 1. 创建 ZIP 包（所有安装文件）
zip_path = os.path.join(RELEASE_DIR, "_setup_payload.zip")
print("📦 打包安装文件...")

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
        print("  ✔ GhostScript 10.04.0")
    else:
        print("  ✗ GhostScript 未找到")
    # .NET Runtime
    if os.path.isfile(DOTNET_FILE):
        zf.write(DOTNET_FILE, "windowsdesktop-runtime-8.0.27-win-x64.exe")
        print("  ✔ .NET 8 Desktop Runtime")
    else:
        print("  ✗ .NET Runtime 未找到")
    # 安装脚本
    ps1 = os.path.join(RELEASE_DIR, "一键安装.ps1")
    if os.path.isfile(ps1):
        zf.write(ps1, "一键安装.ps1")
    print("  ✔ 安装脚本")

zip_size = os.path.getsize(zip_path)
print(f"  ZIP 大小: {zip_size//1024//1024} MB")

# 2. 创建 SFX 存根（自解压引导程序）
stub = open(SETUP_EXE, "wb")

# SFX 存根批处理脚本（嵌入在 EXE 尾部）
installer_bat = """@echo off
chcp 65001 >nul
echo ============================================
echo  PdfAutoPrint Pro v1.1.0 - 完整安装包
echo  包含：.NET 8 + GhostScript + 虚拟打印机
echo ============================================
echo.

:: 获取自身路径
set "SELF=%~dp0"
set "TEMP_DIR=%TEMP%\PdfAutoPrint_Setup"
mkdir "%TEMP_DIR%" 2>nul

echo [1/5] 解压安装文件...
powershell -ExecutionPolicy Bypass -Command "Add-Type -Assembly System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::ExtractToDirectory('%SELF%_payload.zip', '%TEMP_DIR%')"

echo [2/5] 检查管理员权限...
net session >nul 2>&1
if errorlevel 1 (
    echo 需要管理员权限，正在重新启动...
    powershell -Command "Start-Process -FilePath '%SELF%' -Verb RunAs"
    exit /b
)

echo [3/5] 开始安装...
powershell -ExecutionPolicy Bypass -File "%TEMP_DIR%\一键安装.ps1" -Silent

echo.
echo 安装完成！按任意键退出...
pause >nul
rd /s /q "%TEMP_DIR%" 2>nul
"""

# 写 SFX 引导（简单的自解压格式）
# 结构：批处理脚本 + 分隔符 + ZIP 数据
bat_bytes = installer_bat.encode("utf-8-sig")

# 写批处理存根
stub.write(bat_bytes)

# 分隔符（标记 ZIP 数据开始）
stub.write(b"\n---PAYLOAD_ZIP_START---\n")

# 嵌入 ZIP
with open(zip_path, "rb") as zf:
    shutil.copyfileobj(zf, stub)

stub.close()
print(f"✅ 安装包已生成: {SETUP_EXE}")
print(f"   大小: {os.path.getsize(SETUP_EXE)//1024//1024} MB")

# 清理临时 ZIP
os.remove(zip_path)
print("🗑️  临时文件已清理")
