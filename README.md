# PdfAutoPrint Pro

> Windows 虚拟 PDF 打印机管理器 — 零依赖、轻量级、功能全面的 PDF 输出解决方案

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF-5C2D91?style=flat&logo=xaml)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Version](https://img.shields.io/badge/Version-1.1.0-blue.svg)
![Windows](https://img.shields.io/badge/Platform-Windows%207%2B-0078D6?style=flat&logo=windows)

---

## 功能亮点（v1.1.0）

- **自动/手动保存** — 一键切换自动保存或弹出路径选择对话框
- **智能文件命名** — 支持 `<PrintJobName>`、`<DateTime:format>` 等占位符模板'
- **冲突智能处理** — 覆盖 / 递增编号 / 时间戳备份，三种策略应对重名'
- **系统托盘通知** — 任务成功/失败气泡提示，随时掌握打印状态（v1.1.0 ✅）'
- **系统托盘集成** — 最小化到托盘，不占用任务栏空间（v1.1.0 ✅）'
- **开机自启** — 支持开机自动启动，无需手动打开（v1.1.0 ✅）'
- **日志保留策略** — 可配置保留 N 天或永久存档'
- **失败跳过** — 单次失败不中断批量打印，提升流程可靠性'
- **友好桌面界面** — 仪表盘、打印机管理、日志查看、任务历史四大模块'
- **输出质量控制** — 彩色/灰度模式、矢量/图片模式、可配置 DPI'
- **一键安装工具** — PowerShell 脚本自动安装 GhostScript + 驱动 + 打印机 + 应用'
- **打印机管理** — 重命名、查看系统打印机、重启 Spooler、设置默认、查看队列'
- **水印支持** — 文字水印、图片盖章、页面跳过、绝对定位'
- **多打印机配置** — 支持多个虚拟打印机，每个可独立配置'

---

## 新增功能（v1.1.0）

### 系统托盘最小化运行
- 点击窗口「×」按钮时，应用最小化到系统托盘而非退出'
- 双击托盘图标恢复窗口，右键菜单：显示主窗口 / 退出'
- 使用 WinForms NotifyIcon + WPF 混合技术'

### 开机自启
- 通过注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 实现'
- 无需管理员权限，用户在安装时勾选即可'
- 可在「设置」→「服务」中随时开启/关闭'

### 打印完成/失败气泡通知
- 任务成功：显示文件名和保存路径'
- 任务失败：显示失败原因摘要'
- 批量打印时自动合并通知，避免消息轰炸'

---

## 快速开始

### 系统要求

| 项目 | 最低要求 | 推荐配置 |
|------|----------|----------|
| 操作系统 | Windows 7 SP1 x64 | Windows 10/11 x64 |
| .NET 运行时 | .NET 8 Desktop Runtime | .NET 8 Desktop Runtime |
| GhostScript | 10.04.0（安装包自动安装） | 10.04.0 |
| 内存 | 512 MB 可用 | 2 GB 及以上 |
| 磁盘空间 | 300 MB（含运行时） | 500 MB |

> ⚠️ **Windows 7 说明**：.NET 8 官方仅支持 Windows 10 及以上版本。在 Win7 上安装可能会失败或运行不稳定，建议升级到 Windows 10/11 以获得完整体验。

### 安装

1. 下载 `PdfAutoPrint_Pro_Setup_v1.1.0.exe`（约 117 MB，含 .NET 8 + GhostScript）
2. 双击运行，如弹出用户账户控制（UAC），点击「是」'
3. 安装向导将自动依次执行：'
   - 检测 .NET 8 Desktop Runtime，缺失则自动安装'
   - 安装 GhostScript 10.04.0'
   - 安装虚拟 PDF 打印机驱动'
   - 创建 `Auto PDF Printer` 虚拟打印机'
   - 安装 PdfAutoPrint Pro 管理软件'
   - 创建桌面和开始菜单快捷方式'

### 首次启动验证

1. 打开任意文档（如记事本）'
2. 按下 `Ctrl + P` 打印'
3. 选择打印机 **Auto PDF Printer**'
4. 点击「打印」'
5. 查看输出目录是否生成了 PDF 文件'
6. 查看系统托盘是否有通知弹出'

---

## 适用场景

### 企业办公
- 批量合同、报价单、发票的 PDF 归档'
- 统一文件命名规范，如 `合同_客户名_2026-06-05.pdf`'
- 自动添加公司水印，保护文档版权'

### 个人使用
- 网页文章保存为 PDF 存档'
- 各类文档格式统一转换为 PDF'
- 重要邮件的 PDF 备份'

### 开发测试
- 自动化测试中的报表生成'
- 批量文档转换和格式验证'
- 打印输出的质量对比测试'

### 设计制图
- CAD 图纸输出为高清 PDF'
- 图片设计稿的 PDF 交付'
- 支持高 DPI 矢量输出'

---

## 与其他方案对比

| 对比维度 | PdfAutoPrint Pro | Windows 自带 | 第三方 PDF 打印机 | Adobe Acrobat |
|---------|:---------------:|:---------------:|:---------------:|:------------:|
| **自动命名** | ✅ 模板化 | ❌ | ⚠️ | ❌ |
| **水印支持** | ✅ 内置 | ❌ | ❌ | ✅ 付费 |
| **多打印机** | ✅ 支持 | ❌ | ❌ | ❌ |
| **托盘通知** | ✅ 支持 | ❌ | ❌ | ❌ |
| **开机自启** | ✅ 支持 | ❌ | ❌ | ❌ |
| **零依赖** | ✅ 是 | — | ❌ | ❌ |
| **开源免费** | ✅ MIT | 预装 | ⚠️ | ❌ |

---

## 系统架构

```
┌───────────────────────────────────────────────┐
│                    UI 层 (WPF)                     │
│   MainWindow / PrinterConfigWindow             │
├───────────────────────────────────────────────┤
│                  ViewModels 层                  │
│   MainViewModel (INotifyPropertyChanged)       │
├───────────────────────────────────────────────┤
│                   服务层                        │
│  SpoolMonitor / GhostScriptService           │
│  PsWatermarkEngine / PrinterManager         │
│  ConfigService / JobHistoryService          │
├───────────────────────────────────────────────┤
│                   数据层                        │
│  PrinterProfile / WatermarkConfig           │
│  JobRecord                               │
└───────────────────────────────────────────────┘
```

**数据流**：应用程序打印 → Windows 打印队列 → Spool 监控服务 → GhostScript 转换 → PDF 输出'

---

## 项目结构

```
PdfAutoPrint.Pro/
├── Models/                    # 数据模型'
│   ├── PrinterProfile.cs      # 打印机配置模型'
│   ├── WatermarkConfig.cs     # 水印配置模型'
│   └── JobRecord.cs           # 作业历史模型'
├── Services/                  # 业务服务层'
│   ├── ConfigService.cs       # JSON 配置管理'
│   ├── GhostScriptService.cs  # PS→PDF 转换引擎'
│   ├── PrinterManager.cs      # PowerShell/WMI 打印机管理'
│   ├── PsWatermarkEngine.cs   # PostScript 水印注入'
│   ├── FileNameResolver.cs    # 文件名模板解析'
│   ├── ConflictResolver.cs    # 文件冲突处理'
│   ├── LogService.cs          # 文件和控制台日志'
│   ├── JobHistoryService.cs   # 作业历史服务'
│   └── SpoolMonitor.cs        # 打印队列监控'
├── ViewModels/                # MVVM ViewModel'
│   └── MainViewModel.cs       # 主 VM（12 Commands）'
├── Views/                     # WPF 视图'
│   ├── MainWindow.xaml/.cs    # 主窗口（4 标签页）'
│   ├── PrinterConfigWindow    # 打印机配置（5 标签页）'
│   └── WatermarkEditorWindow  # 水印编辑器'
├── Converters/                # WPF 值转换器'
├── docs/                      # 项目文档'
├── release/                    # 发布目录'
│   ├── PdfAutoPrint_Pro_Setup_v1.1.0.exe  # 完整安装包（117 MB）'
│   ├── gs10040w64.exe                         # GhostScript 安装包'
│   └── windowsdesktop-runtime-8.0.27-win-x64.exe  # .NET 8 运行时'
├── install.ps1                # 一键安装脚本'
├── PdfAutoPrint.Pro.csproj    # 项目文件'
└── README.md
```

---

## 技术特点

- **MVVM 架构**：清晰的代码结构，便于二次开发和维护'
- **.NET 8**：基于最新的 .NET 框架，性能卓越'
- **WPF 界面**：原生 Windows 桌面体验，无 Electron 笨重感'
- **零 NuGet 依赖**：不依赖任何第三方包，安装包体积极小'
- **系统托盘集成**：WinForms NotifyIcon + WPF 混合技术'

---

## 文档

| 文档 | 本地路径 | 腾讯文档 |
|------|----------|----------|
| 产品介绍 | `docs/产品介绍.md` | [查看](https://docs.qq.com/aio/DRm1ob1FyT0h0YXNB) |
| 安装手册 | `docs/安装手册.md` | [查看](https://docs.qq.com/aio/DRm5IQXBLd2FOR0Zm) |
| 功能介绍 | `docs/功能介绍.md` | — |
| 软件简介 | `docs/软件简介.md` | — |
| 架构图 | `docs/架构图.md` | [查看](https://docs.qq.com/flowchart/DRnFwbnZZSHNrZHVB) |
| 开发文档 | `docs/开发文档.md` | — |
| 开发计划 | `docs/开发计划.md` | — |
| 技术方案 | `docs/技术方案.md` | — |
| 技术白皮书 | `docs/技术白皮书.md` | — |

---

## 下载

- **完整安装包 v1.1.0**（含 .NET 8 + GhostScript）：`release/PdfAutoPrint_Pro_Setup_v1.1.0.exe`'
- **GitHub 仓库**：https://github.com/Never27/dfAutoPrint-Pro'

---

## 联系作者

- **作者**：和学斌'
- **QQ**：1210696000'
- **邮箱**：1210696000@qq.com'

---

## 开源许可

MIT License — 自由使用、修改和分发。

---

> **PdfAutoPrint Pro** — 让 Windows 打印驱动真正成为生产力工具。
