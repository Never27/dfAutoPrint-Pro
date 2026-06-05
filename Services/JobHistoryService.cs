using System.IO;
using System.Text.Json;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// 任务历史记录服务（JSON文件存储）
/// </summary>
public class JobHistoryService
{
    private readonly string _dbDir;
    private readonly string _dbFile;
    private List<JobRecord> _records = new();
    private bool _initialized;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JobHistoryService()
    {
        _dbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PdfAutoPrint.Pro");
        _dbFile = Path.Combine(_dbDir, "jobs.json");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_dbDir);

        if (File.Exists(_dbFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_dbFile);
                _records = JsonSerializer.Deserialize<List<JobRecord>>(json, JsonOptions) ?? new List<JobRecord>();
            }
            catch
            {
                _records = new List<JobRecord>();
            }
        }

        _initialized = true;
    }

    public async Task<long> AddRecordAsync(JobRecord record)
    {
        EnsureInitialized();

        record.Id = _records.Count > 0 ? _records.Max(r => r.Id) + 1 : 1;
        _records.Add(record);

        // 保持最多1000条记录
        if (_records.Count > 1000)
            _records = _records.OrderByDescending(r => r.Id).Take(1000).ToList();

        await SaveAsync();
        return record.Id;
    }

    public Task<List<JobRecord>> GetRecentAsync(int limit = 50)
    {
        EnsureInitialized();
        var result = _records
            .OrderByDescending(r => r.StartTime)
            .Take(limit)
            .ToList();
        return Task.FromResult(result);
    }

    public async Task ClearOldRecordsAsync(int retentionDays)
    {
        if (retentionDays <= 0) return;
        EnsureInitialized();

        var cutoff = DateTime.Now.AddDays(-retentionDays);
        _records.RemoveAll(r => r.StartTime < cutoff);
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_records, JsonOptions);
        await File.WriteAllTextAsync(_dbFile, json);
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("JobHistoryService not initialized");
    }
}
