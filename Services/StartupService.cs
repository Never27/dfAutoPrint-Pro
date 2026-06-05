using Microsoft.Win32;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// 开机自启服务：通过注册表 HKCU Run 键控制
/// 不需要管理员权限，仅操作用户级注册表
/// </summary>
public static class StartupService
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "PdfAutoPrint.Pro";

    /// <summary>
    /// 设置开机自启
    /// </summary>
    public static void SetAutoStart(bool enable, string? exePath = null)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null)
            {
                Registry.CurrentUser.CreateSubKey(RunKey);
                using var newKey = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (newKey == null) return;
                DoSet(newKey, enable, exePath);
            }
            else
            {
                DoSet(key, enable, exePath);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 没有权限，静默忽略
        }
        catch (Exception)
        {
            // 其他错误也静默忽略
        }
    }

    private static void DoSet(RegistryKey key, bool enable, string? exePath)
    {
        if (enable)
        {
            var path = exePath ?? Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            key.SetValue(AppName, $"\"{path}\"");
        }
        else
        {
            try { key.DeleteValue(AppName, throwOnMissingValue: false); }
            catch { /* 值不存在 */ }
        }
    }

    /// <summary>
    /// 检查是否已设置开机自启
    /// </summary>
    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            if (key == null) return false;
            return key.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }
}
