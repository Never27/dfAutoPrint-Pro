using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using PdfAutoPrint.Pro.Models;
using PdfAutoPrint.Pro.Services;
using WinForms = System.Windows.Forms;

namespace PdfAutoPrint.Pro.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _configService;
    private readonly PrinterManager _printerManager;
    private readonly GhostScriptService _gsService;
    private readonly LogService _logService;
    private readonly JobHistoryService _historyService;
    private readonly PsWatermarkEngine _watermarkEngine;
    private readonly FileNameResolver _fileNameResolver;

    private readonly Dictionary<string, (SpoolMonitor Monitor, CancellationTokenSource Cts)> _monitors = new();

    public MainViewModel()
    {
        _configService = new ConfigService();
        _printerManager = new PrinterManager();
        _gsService = new GhostScriptService();
        _watermarkEngine = new PsWatermarkEngine();
        _fileNameResolver = new FileNameResolver();
        _historyService = new JobHistoryService();

        _configService.Load();
        _logService = new LogService(_configService.Config.Log);

        _gsService.Initialize(_configService.Config.Printers.FirstOrDefault()?.Service.GhostScriptPath);

        // 订阅日志
        _logService.OnLogEntry += entry =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogEntries.Add(entry);
                if (LogEntries.Count > 500)
                    LogEntries.RemoveAt(0);
            });
        };

        // 初始化命令
        StartAllCommand = new RelayCommand(async _ => await StartAllAsync());
        StopAllCommand = new RelayCommand(_ => StopAll());
        StartPrinterCommand = new RelayCommand(async p => await StartPrinterAsync(p as PrinterViewModel));
        StopPrinterCommand = new RelayCommand(p => StopPrinter(p as PrinterViewModel));
        CreatePrinterCommand = new RelayCommand(async _ => await CreatePrinterAsync());
        DeletePrinterCommand = new RelayCommand(async p => await DeletePrinterAsync(p as PrinterViewModel));
        EditPrinterCommand = new RelayCommand(p => EditPrinter(p as PrinterViewModel));
        OpenOutputFolderCommand = new RelayCommand(p => OpenFolder(p as PrinterViewModel));
        RestartSpoolerCommand = new RelayCommand(async _ => await RestartSpoolerAsync());
        SetDefaultPrinterCommand = new RelayCommand(async p => await SetDefaultAsync(p as string));
        RefreshSystemPrintersCommand = new RelayCommand(async _ => await RefreshSystemPrintersAsync());
        ClearHistoryCommand = new RelayCommand(async _ => await ClearHistoryAsync());
        OpenLogFolderCommand = new RelayCommand(_ => OpenLogFolder());

        // 初始化打印机列表
        RefreshPrinterList();

        // 异步初始化历史
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _historyService.InitializeAsync();
        await RefreshHistoryAsync();
        _logService.Info("PdfAutoPrint Pro 已启动");
    }

    // ======== 属性 ========

    public ObservableCollection<PrinterViewModel> Printers { get; } = new();
    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    public ObservableCollection<JobRecord> RecentJobs { get; } = new();
    public ObservableCollection<PrinterInfo> SystemPrinters { get; } = new();

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; OnPropertyChanged(); }
    }

    private string _statusText = "就绪";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    // ======== 命令 ========

    public ICommand StartAllCommand { get; }
    public ICommand StopAllCommand { get; }
    public ICommand StartPrinterCommand { get; }
    public ICommand StopPrinterCommand { get; }
    public ICommand CreatePrinterCommand { get; }
    public ICommand DeletePrinterCommand { get; }
    public ICommand EditPrinterCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }
    public ICommand RestartSpoolerCommand { get; }
    public ICommand SetDefaultPrinterCommand { get; }
    public ICommand RefreshSystemPrintersCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand OpenLogFolderCommand { get; }

    // ======== 方法 ========

    private void RefreshPrinterList()
    {
        Printers.Clear();
        foreach (var profile in _configService.Config.Printers)
        {
            Printers.Add(new PrinterViewModel(profile));
        }
    }

    private async Task StartAllAsync()
    {
        IsRunning = true;
        StatusText = "启动中...";

        foreach (var pvm in Printers)
        {
            if (!pvm.Profile.Enabled) continue;
            await StartMonitor(pvm);
        }

        StatusText = $"运行中 - {Printers.Count(p => p.IsMonitoring)} 个打印机";
        _logService.Info("所有打印机监控已启动");
    }

    private async Task StartMonitor(PrinterViewModel pvm)
    {
        var conflictResolver = new ConflictResolver();

        var monitor = new SpoolMonitor(
            pvm.Profile, _gsService, _watermarkEngine,
            _fileNameResolver, conflictResolver, _logService, _historyService, _printerManager);

        monitor.OnJobCompleted += record =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RecentJobs.Insert(0, record);
                if (RecentJobs.Count > 100) RecentJobs.RemoveAt(RecentJobs.Count - 1);
                pvm.LastJob = $"{Path.GetFileName(record.OutputFile)}";
                pvm.JobCount++;
                pvm.Status = "正常";

                // 气泡通知
                if (pvm.Profile.Notification.NotifyOnSuccess && App.TrayIcon != null)
                {
                    App.TrayIcon.ShowBalloonTip(3000,
                        $"PDF 转换完成 - {pvm.Name}",
                        $"{Path.GetFileName(record.OutputFile)}\n大小: {record.FileSize / 1024f:F1} KB",
                        WinForms.ToolTipIcon.Info);
                }
            });
        };

        monitor.OnJobFailed += record =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RecentJobs.Insert(0, record);
                pvm.Status = "失败";

                // 气泡通知
                if (pvm.Profile.Notification.NotifyOnFailure && App.TrayIcon != null)
                {
                    App.TrayIcon.ShowBalloonTip(5000,
                        $"转换失败 - {pvm.Name}",
                        record.ErrorMessage ?? "未知错误",
                        WinForms.ToolTipIcon.Error);
                }
            });
        };

        var cts = new CancellationTokenSource();
        _monitors[pvm.Profile.Id] = (monitor, cts);

        pvm.IsMonitoring = true;
        pvm.Status = "监控中";

        // 后台运行监控
        _ = Task.Run(() => monitor.StartAsync(cts.Token), cts.Token);
    }

    private void StopAll()
    {
        foreach (var (id, (monitor, cts)) in _monitors)
        {
            cts.Cancel();
            monitor.Stop();
        }
        _monitors.Clear();

        foreach (var pvm in Printers)
        {
            pvm.IsMonitoring = false;
            pvm.Status = "已停止";
        }

        IsRunning = false;
        StatusText = "已停止";
        _logService.Info("所有监控已停止");
    }

    private async Task StartPrinterAsync(PrinterViewModel? pvm)
    {
        if (pvm == null || pvm.IsMonitoring) return;
        await StartMonitor(pvm);
        _logService.Info($"打印机监控已启动: {pvm.Name}");
    }

    private void StopPrinter(PrinterViewModel? pvm)
    {
        if (pvm == null || !pvm.IsMonitoring) return;

        if (_monitors.TryGetValue(pvm.Profile.Id, out var pair))
        {
            pair.Cts.Cancel();
            pair.Monitor.Stop();
            _monitors.Remove(pvm.Profile.Id);
        }

        pvm.IsMonitoring = false;
        pvm.Status = "已停止";
        _logService.Info($"打印机监控已停止: {pvm.Name}");
    }

    private async Task CreatePrinterAsync()
    {
        var name = $"Auto PDF Printer {Printers.Count + 1}";
        var profile = _configService.AddPrinter(name);
        Printers.Add(new PrinterViewModel(profile));

        _logService.Info($"已创建打印机: {name}");

        // 询问是否立即安装
        var result = MessageBox.Show(
            $"打印机 \"{name}\" 已创建。\n是否立即以管理员身份安装到系统中？",
            "安装打印机", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _printerManager.CreateVirtualPrinterAsync(profile);
                _logService.Success($"打印机安装成功: {name}");
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("需要管理员权限才能安装打印机。请以管理员身份运行此程序。",
                    "权限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安装失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        StatusText = $"{Printers.Count} 个打印机";
    }

    private async Task DeletePrinterAsync(PrinterViewModel? pvm)
    {
        if (pvm == null) return;

        var result = MessageBox.Show(
            $"确定要删除打印机 \"{pvm.Name}\" 吗？\n此操作不可撤销。",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        // 停止监控
        if (_monitors.TryGetValue(pvm.Profile.Id, out var pair))
        {
            pair.Cts.Cancel();
            pair.Monitor.Stop();
            _monitors.Remove(pvm.Profile.Id);
        }

        // 卸载 Windows 系统打印机
        try
        {
            await _printerManager.DeletePrinterAsync(pvm.Name);
            _logService.Info($"已从系统卸载打印机: {pvm.Name}");
        }
        catch (Exception ex)
        {
            _logService.Warn($"卸载系统打印机失败（配置已删除）: {ex.Message}");
        }

        // 从配置删除
        _configService.RemovePrinter(pvm.Profile.Id);
        Printers.Remove(pvm);

        _logService.Info($"已删除打印机: {pvm.Name}");
    }

    private void EditPrinter(PrinterViewModel? pvm)
    {
        if (pvm == null) return;

        var editor = new Views.PrinterConfigWindow(pvm.Profile, _logService, _configService.Config)
        {
            Owner = Application.Current.MainWindow
        };

        if (editor.ShowDialog() == true)
        {
            _configService.Save();
            pvm.Refresh();
            _logService.Info($"配置已更新: {pvm.Name}");
        }
    }

    private void OpenFolder(PrinterViewModel? pvm)
    {
        if (pvm == null) return;
        var path = FileNameResolver.ResolveOutputRoot(pvm.Profile.Output.OutputRoot);
        Directory.CreateDirectory(path);
        Process.Start("explorer.exe", path);
    }

    private async Task RestartSpoolerAsync()
    {
        try
        {
            await _printerManager.RestartSpoolerAsync();
            _logService.Success("打印服务已重启");
            MessageBox.Show("打印服务已成功重启。", "操作完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("需要管理员权限。", "权限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task SetDefaultAsync(string? printerName)
    {
        if (string.IsNullOrEmpty(printerName)) return;
        try
        {
            await _printerManager.SetDefaultPrinterAsync(printerName);
            _logService.Success($"已设为默认打印机: {printerName}");
        }
        catch (Exception ex)
        {
            _logService.Error($"设置默认打印机失败: {ex.Message}");
        }
    }

    private async Task RefreshSystemPrintersAsync()
    {
        var printers = await _printerManager.GetSystemPrintersAsync();
        SystemPrinters.Clear();
        foreach (var p in printers)
            SystemPrinters.Add(p);

        StatusText = $"系统共 {printers.Count} 个打印机";
    }

    private async Task RefreshHistoryAsync()
    {
        var records = await _historyService.GetRecentAsync(50);
        RecentJobs.Clear();
        foreach (var r in records)
            RecentJobs.Add(r);
    }

    private async Task ClearHistoryAsync()
    {
        await _historyService.ClearOldRecordsAsync(1);
        RecentJobs.Clear();
        _logService.Info("历史记录已清空");
    }

    private void OpenLogFolder()
    {
        var path = _logService.GetLogDirectory();
        Process.Start("explorer.exe", path);
    }

    // ======== INotifyPropertyChanged ========

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class PrinterViewModel : INotifyPropertyChanged
{
    public PrinterProfile Profile { get; }

    public PrinterViewModel(PrinterProfile profile)
    {
        Profile = profile;
    }

    public string Id => Profile.Id;
    public string Name => Profile.PrinterName;
    public bool Enabled
    {
        get => Profile.Enabled;
        set { Profile.Enabled = value; OnPropertyChanged(); }
    }

    private bool _isMonitoring;
    public bool IsMonitoring
    {
        get => _isMonitoring;
        set { _isMonitoring = value; OnPropertyChanged(); }
    }

    private string _status = "就绪";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private string _lastJob = "-";
    public string LastJob
    {
        get => _lastJob;
        set { _lastJob = value; OnPropertyChanged(); }
    }

    private int _jobCount;
    public int JobCount
    {
        get => _jobCount;
        set { _jobCount = value; OnPropertyChanged(); }
    }

    public string OutputDir => FileNameResolver.ResolveOutputRoot(Profile.Output.OutputRoot);

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(OutputDir));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
