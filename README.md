# PdfAutoPrint Pro

> Windows 虚拟 PDF 打印机管理器 — 零依赖、轻量级、功能全面的 PDF 输出解决方案

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF-5C2D91?style=flat&logo=xaml)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Version](https://img.shields.io/badge/Version-1.0.0-blue.svg)
![Windows](https://img.shields.io/badge/Platform-Windows%207%2B-0078D6?style=flat&logo=windows)

---

## 功能亮点

- **自动/手动保存** — 一键切换自动保存或弹出路径选择对话框
- **智能文件命名** — 支持 `<PrintJobName>`、`<DateTime:format>` 等占位符模板
- **冲突智能处理** — 覆盖 / 递增编号 / 时间戳备份，三种策略应对重名
- **系统托盘通知** — 任务成功/失败气泡提示，随时掌握打印状态
- **日志保留策略** — 可配置保留 N 天或永久存档
- **失败跳过** — 单次失败不中断批量打印，提升流程可靠性
- **友好桌面界面** — 仪表盘、打印机管理、日志查看、任务历史四大模块
- **输出质量控制** — 彩色/灰度模式、矢量/图片模式、可配置 DPI
- **一键安装工具** — PowerShell 脚本自动安装 GhostScript + 驱动 + 打印机 + 应用
- **打印机管理** — 重命名、查看系统打印机、重启 Spooler、设置默认、查看队列
- **水印支持** — 文字水印、图片盖章、页面跳过、绝对定位
- **多打印机配置** — 支持多个虚拟打印机，每个可独立配置

---

## 截图预览

> *截图将在正式版本发布时补充*

| 仪表盘 | 打印机管理 | 日志查看 |
|:-----:|:--------:|:------:|
| ![仪表盘](docs/screenshots/dashboard.png) | ![打印机管理](docs/screenshots/printer-manager.png) | ![日志查看](docs/screenshots/log-viewer.png) |

---

## 快速开始

### 前提条件

- **操作系统**：Windows 7 / 10 / 11（64 位推荐）
- **运行时**：[.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- **GhostScript**：安装脚本将自动下载安装

### 安装

```powershell
# 以管理员身份运行一键安装脚本
.\install.ps1
```

脚本将自动完成：

1. 检查 .NET 8 Desktop Runtime
2. 下载并安装 GhostScript
3. 安装虚拟 PDF 打印机驱动
4. 创建虚拟打印机
5. 注册 PdfAutoPrint Pro 应用

### 使用

1. 启动 PdfAutoPrint Pro
2. 在任意应用中执行"打印"操作
3. 选择 **PdfAutoPrint Pro** 虚拟打印机
4. PDF 文件将按照您的配置自动生成并保存

---

## 系统架构

```
┌──────────────────────────────────────────────────────┐
│                    应用层 (WPF UI)                      │
│  仪表盘 │ 打印机管理 │ 日志查看器 │ 任务历史 │ 系统配置    │
├──────────────────────────────────────────────────────┤
│                   服务层 (Services)                     │
│  SpoolMonitor │ PrintJobHandler │ ConfigManager       │
│  LogService │ WatermarkEngine │ FileNameBuilder       │
├──────────────────────────────────────────────────────┤
│                    中间层                              │
│           GhostScript Processor                        │
│       (PS/PDF 转换引擎，支持颜色/DPI/矢量)                │
├──────────────────────────────────────────────────────┤
│                    输出层                              │
│         PDF 文件 → 指定目录（含水印/重命名）               │
└──────────────────────────────────────────────────────┘
```

**数据流**：应用程序打印 → Windows 打印队列 → Spool 监控服务 → GhostScript 转换 → PDF 输出

---

## 项目结构

```
PdfAutoPrint.Pro/
├── Models/                  # 数据模型 (PrinterProfile, WatermarkConfig, JobRecord)
├── Services/                # 业务服务层 (9个服务类)
│   ├── ConfigService.cs     # JSON 配置管理
│   ├── GhostScriptService.cs # PS→PDF 转换引擎
│   ├── PrinterManager.cs    # PowerShell/WMI 打印机管理
│   ├── PsWatermarkEngine.cs # PostScript 水印注入
│   ├── FileNameResolver.cs  # 文件名模板解析
│   ├── ConflictResolver.cs  # 文件冲突处理
│   ├── LogService.cs        # 文件+控制台日志
│   ├── JobHistoryService.cs # 任务历史 (JSON)
│   └── SpoolMonitor.cs      # 打印队列监控
├── ViewModels/              # MVVM ViewModel (MainViewModel)
├── Views/                   # WPF 视图 (3个窗口, 4个选项卡)
├── Converters/              # WPF 值转换器
├── docs/                    # 项目文档
├── PdfAutoPrint.Pro.csproj  # 项目文件 (net8.0-windows, 零依赖)
├── install.ps1              # 一键安装脚本
└── README.md
```

---

## 联系作者

- **作者**：和学斌
- **QQ**：1210696000
- **邮箱**：1210696000@qq.com

---

## 开源许可

本项目基于 [MIT License](LICENSE) 开源协议发布。

---

> **PdfAutoPrint Pro** — 让 Windows 打印驱动真正成为生产力工具。
