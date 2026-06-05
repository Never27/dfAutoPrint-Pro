using System.IO;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// 文件冲突处理器：覆盖/递增/备份
/// </summary>
public class ConflictResolver
{
    /// <summary>
    /// 根据冲突策略获取实际输出路径
    /// </summary>
    public string Resolve(string targetPath, ConflictConfig config)
    {
        if (!File.Exists(targetPath))
            return targetPath;

        return config.Mode switch
        {
            ConflictMode.Overwrite => HandleOverwrite(targetPath),
            ConflictMode.Increment => HandleIncrement(targetPath, config),
            ConflictMode.Backup => HandleBackup(targetPath, config),
            _ => HandleIncrement(targetPath, config)
        };
    }

    private static string HandleOverwrite(string targetPath)
    {
        // 尝试删除已存在的文件
        try
        {
            File.Delete(targetPath);
        }
        catch
        {
            // 如果删除失败，回退到递增模式
            return HandleIncrement(targetPath, new ConflictConfig());
        }
        return targetPath;
    }

    private static string HandleIncrement(string targetPath, ConflictConfig config)
    {
        var dir = Path.GetDirectoryName(targetPath) ?? ".";
        var nameWithoutExt = Path.GetFileNameWithoutExtension(targetPath);
        var ext = Path.GetExtension(targetPath);
        var sep = string.IsNullOrEmpty(config.IncrementSeparator) ? "_" : config.IncrementSeparator;

        int counter = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(dir, $"{nameWithoutExt}{sep}{counter}{ext}");
            counter++;
        } while (File.Exists(newPath) && counter < 10000);

        return newPath;
    }

    private static string HandleBackup(string targetPath, ConflictConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(targetPath) ?? ".";
            var nameWithoutExt = Path.GetFileNameWithoutExtension(targetPath);
            var ext = Path.GetExtension(targetPath);

            var suffix = DateTime.Now.ToString(config.BackupSuffixFormat);
            var backupPath = Path.Combine(dir, $"{nameWithoutExt}{suffix}{ext}");

            File.Move(targetPath, backupPath);
        }
        catch
        {
            // 备份失败则使用递增模式
            return HandleIncrement(targetPath, new ConflictConfig());
        }

        return targetPath;
    }
}
