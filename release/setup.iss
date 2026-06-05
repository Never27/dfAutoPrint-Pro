; PdfAutoPrint Pro - 一键安装包
; 支持 Win7 / Win10 / Win11
; 自动安装: .NET 8 Runtime + GhostScript + 打印机驱动 + 虚拟打印机 + 管理软件

#define MyAppName "PdfAutoPrint Pro"
#define MyAppVersion "1.1.2"
#define MyAppPublisher "和学斌"
#define MyAppURL "https://github.com/Never27/PdfAutoPrint-Pro"
#define MyAppExeName "PdfAutoPrint.Pro.exe"
#define DotNetRuntime "windowsdesktop-runtime-8.0.27-win-x64.exe"
#define GhostScript "gs10040w64.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=.
OutputBaseFilename=PdfAutoPrint_Pro_Setup_v1.1.2
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=6.1.7601
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Messages]
chinese.WelcomeLabel2=即将安装 {#MyAppName} v{#MyAppVersion} 到您的计算机。%n%n安装程序将自动完成以下操作：%n  1. 检测并安装 .NET 8 桌面运行时%n  2. 安装 GhostScript PDF 处理引擎%n  3. 安装虚拟 PDF 打印机驱动%n  4. 创建 Auto PDF Printer 虚拟打印机%n  5. 安装 PdfAutoPrint Pro 管理软件%n%n全程无需手动干预，点击"下一步"开始。

[Files]
; 管理软件主程序
Source: "app\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "app\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "app\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "app\*.pdb"; DestDir: "{app}"; Flags: ignoreversion
; 捆绑的运行时安装包
Source: "{#DotNetRuntime}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsDotNet8Installed
Source: "{#GhostScript}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsGhostScriptInstalled
; 打印机配置脚本
Source: "printer_setup.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "C:\PDFOutput\spool"; Permissions: everyone-full

[Icons]
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
; Step 1: 安装 .NET 8 桌面运行时
Filename: "{tmp}\{#DotNetRuntime}"; Parameters: "/install /quiet /norestart"; \
    StatusMsg: "正在安装 .NET 8 桌面运行时..."; \
    Check: not IsDotNet8Installed; \
    Flags: runhidden waituntilterminated

; Step 2: 安装 GhostScript
Filename: "{tmp}\{#GhostScript}"; Parameters: "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART"; \
    StatusMsg: "正在安装 GhostScript PDF 引擎..."; \
    Check: not IsGhostScriptInstalled; \
    Flags: runhidden waituntilterminated

; Step 3: 安装打印机驱动 + 创建虚拟打印机
Filename: "powershell.exe"; \
    Parameters: "-ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\printer_setup.ps1"""; \
    StatusMsg: "正在配置虚拟 PDF 打印机..."; \
    Flags: runhidden waituntilterminated

; Step 4: 可选 - 启动软件
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; \
    Flags: nowait postinstall skipifsilent unchecked

[UninstallRun]
; 卸载时删除虚拟打印机
Filename: "powershell.exe"; \
    Parameters: "-ExecutionPolicy Bypass -Command ""Get-Printer -Name 'Auto PDF Printer' -ErrorAction SilentlyContinue | Remove-Printer -ErrorAction SilentlyContinue; Get-PrinterPort -Name 'C:\PDFOutput\spool\printjob.prn' -ErrorAction SilentlyContinue | Remove-PrinterPort -ErrorAction SilentlyContinue"""; \
    Flags: runhidden waituntilterminated; \
    RunOnceId: "RemovePrinter"

[Code]

// 检查 .NET 8 桌面运行时是否已安装
function IsDotNet8Installed: Boolean;
var
  ResultCode: Integer;
  RegPath: String;
begin
  // 检查注册表
  if RegKeyExists(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost') then
  begin
    Result := True;
  end
  else if RegKeyExists(HKLM32, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost') then
  begin
    Result := True;
  end
  else
  begin
    // 也尝试检查 Runtime 注册表位置
    if RegKeyExists(HKLM64, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') then
      Result := True
    else if RegKeyExists(HKLM32, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') then
      Result := True
    else
      Result := False;
  end;
end;

// 检查 GhostScript 是否已安装
function IsGhostScriptInstalled: Boolean;
begin
  Result := False;
  if FileExists(ExpandConstant('{pf}\gs\gs10.04.0\bin\gswin64c.exe')) then
    Result := True
  else if FileExists(ExpandConstant('{pf}\gs\gs10.03.1\bin\gswin64c.exe')) then
    Result := True
  else if FileExists(ExpandConstant('{pf}\gs\gs10.02.1\bin\gswin64c.exe')) then
    Result := True;
end;

// 检查操作系统版本
function IsWindows7OrEarlier: Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  Result := (Version.Major < 6) or ((Version.Major = 6) and (Version.Minor <= 1));
end;

// 安装初始化 - Win7 警告
function InitializeSetup: Boolean;
begin
  Result := True;
  if IsWindows7OrEarlier then
  begin
    if MsgBox(
      '⚠️ 检测到您使用的是 Windows 7 系统。' + #13#10 + #13#10 +
      '.NET 8 桌面运行时官方仅支持 Windows 10 及以上版本。' + #13#10 +
      '在 Win7 上安装可能会失败或运行不稳定。' + #13#10 + #13#10 +
      '建议升级到 Windows 10/11 以获得完整体验。' + #13#10 + #13#10 +
      '是否仍然继续安装？',
      mbWarning, MB_YESNO) = IDYES then
    begin
      Result := True;
    end
    else
    begin
      Result := False;
    end;
  end;
end;

// 安装完成页面 - 显示概要
procedure CurStepChanged(CurStep: TSetupStep);
var
  InfoStr: String;
begin
  if CurStep = ssPostInstall then
  begin
    InfoStr := '安装完成！' + #13#10 + #13#10 +
      '✔ 软件已安装到: ' + ExpandConstant('{app}') + #13#10 +
      '✔ 输入目录: C:\PDFOutput\{date}\' + #13#10 +
      '✔ 假脱机目录: C:\PDFOutput\spool\' + #13#10 + #13#10 +
      '使用方式:' + #13#10 +
      '  1. 从桌面或开始菜单启动 PdfAutoPrint Pro' + #13#10 +
      '  2. 点击 "Start All" 开始监听' + #13#10 +
      '  3. 在任何应用中打印 → 选择 "Auto PDF Printer"' + #13#10 +
      '  4. PDF 自动输出到 C:\PDFOutput\ 下' + #13#10 + #13#10 +
      '作者: 和学斌  QQ: 1210696000' + #13#10 +
      'GitHub: https://github.com/Never27/PdfAutoPrint-Pro';

    // 写入安装信息到注册表
    RegWriteStringValue(HKLM, 'Software\{#MyAppName}', 'InstallPath', ExpandConstant('{app}'));
    RegWriteStringValue(HKLM, 'Software\{#MyAppName}', 'Version', '{#MyAppVersion}');
    RegWriteStringValue(HKLM, 'Software\{#MyAppName}', 'OutputDir', 'C:\PDFOutput');
  end;
end;

// 卸载时清理
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // 清理注册表
    RegDeleteKeyIncludingSubkeys(HKLM, 'Software\{#MyAppName}');
    // 询问是否保留输出文件
    if MsgBox('是否保留 PDF 输出目录 (C:\PDFOutput\) 中的文件？' + #13#10 +
      '选"是"保留文件，选"否"删除所有输出。',
      mbConfirmation, MB_YESNO) = IDNO then
    begin
      DelTree(ExpandConstant('C:\PDFOutput'), True, True, True);
    end;
  end;
end;
