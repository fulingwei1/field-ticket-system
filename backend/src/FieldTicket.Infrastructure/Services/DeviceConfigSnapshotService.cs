using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 设备配置快照服务实现
/// </summary>
public class DeviceConfigSnapshotService : IDeviceConfigSnapshotService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeviceConfigSnapshotService> _logger;

    public DeviceConfigSnapshotService(
        ApplicationDbContext dbContext,
        ILogger<DeviceConfigSnapshotService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> CreateSnapshotAsync(
        CreateConfigSnapshotRequest request,
        Guid userId)
    {
        var snapshot = new DeviceConfigSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            DeviceId = request.DeviceId,
            SnapshotType = request.SnapshotType,
            SnapshotAt = request.SnapshotAt,
            ConfigJson = JsonDocument.Parse(JsonSerializer.Serialize(request.ConfigJson)),
            StandardConfigJson = request.StandardConfigJson != null
                ? JsonDocument.Parse(JsonSerializer.Serialize(request.StandardConfigJson))
                : null,
            Differences = request.Differences != null && request.Differences.Any()
                ? JsonDocument.Parse(JsonSerializer.Serialize(request.Differences))
                : null,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.DeviceConfigSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("创建设备 {DeviceId} 配置快照，类型：{SnapshotType}，时间：{SnapshotAt}",
            request.DeviceId, request.SnapshotType, request.SnapshotAt);

        return snapshot.SnapshotId;
    }

    public async Task<List<ConfigDifference>> CheckConfigDifferencesAsync(
        Guid deviceId,
        Dictionary<string, object>? currentConfig = null)
    {
        // 获取当前配置
        if (currentConfig == null)
        {
            currentConfig = await GetCurrentConfigAsync(deviceId);
        }

        // 获取标准配置
        var standardConfig = await GetStandardConfigAsync(deviceId);
        if (standardConfig == null)
        {
            _logger.LogWarning("设备 {DeviceId} 没有标准配置，无法进行差异检测", deviceId);
            return new List<ConfigDifference>();
        }

        // 对比配置
        var differences = CompareConfigs(currentConfig, standardConfig);

        // 如果有差异，自动创建问题快照
        if (differences.Any())
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CreateSnapshotAsync(new CreateConfigSnapshotRequest
                    {
                        DeviceId = deviceId,
                        SnapshotType = "problem",
                        SnapshotAt = DateTime.UtcNow,
                        ConfigJson = currentConfig,
                        StandardConfigJson = standardConfig,
                        Differences = differences
                    }, Guid.Empty); // 系统自动创建，无用户ID
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "自动创建问题快照失败，设备：{DeviceId}", deviceId);
                }
            });
        }

        return differences;
    }

    public async Task<List<ConfigSnapshotDto>> GetSnapshotHistoryAsync(
        Guid deviceId,
        string? snapshotType = null,
        int limit = 50)
    {
        var query = _dbContext.DeviceConfigSnapshots
            .Where(s => s.DeviceId == deviceId);

        if (!string.IsNullOrEmpty(snapshotType))
        {
            query = query.Where(s => s.SnapshotType == snapshotType);
        }

        var snapshots = await query
            .OrderByDescending(s => s.SnapshotAt)
            .Take(limit)
            .ToListAsync();

        return snapshots.Select(MapToDto).ToList();
    }

    public async Task<Dictionary<string, object>?> GetStandardConfigAsync(Guid deviceId)
    {
        // 查找交付时的标准配置快照
        var standardSnapshot = await _dbContext.DeviceConfigSnapshots
            .Where(s => s.DeviceId == deviceId && s.SnapshotType == "delivery")
            .OrderByDescending(s => s.SnapshotAt)
            .FirstOrDefaultAsync();

        if (standardSnapshot != null && standardSnapshot.ConfigJson != null)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(
                standardSnapshot.ConfigJson.RootElement.GetRawText());
        }

        return null;
    }

    public async Task<Dictionary<string, object>> GetCurrentConfigAsync(Guid deviceId)
    {
        // 从工单中获取当前配置（版本信息）
        // 这里需要根据实际业务逻辑获取设备的当前配置
        // 目前从最新的工单中获取版本信息作为当前配置
        
        var latestTicket = await _dbContext.Tickets
            .Where(t => t.DeviceId == deviceId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        var config = new Dictionary<string, object>();

        if (latestTicket != null)
        {
            config["sw_version"] = latestTicket.SwVersion;
            config["plc_version"] = latestTicket.PlcVersion;
            config["param_version"] = latestTicket.ParamVersion;

            // 如果有事实表，也包含进去
            if (latestTicket.FactsJson != null)
            {
                var facts = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    latestTicket.FactsJson.RootElement.GetRawText());
                if (facts != null)
                {
                    foreach (var fact in facts)
                    {
                        config[$"fact_{fact.Key}"] = fact.Value;
                    }
                }
            }
        }
        else
        {
            // 如果没有工单，尝试从设备表获取
            // TODO: 如果设备表有版本字段，从这里获取
            config["sw_version"] = "";
            config["plc_version"] = "";
            config["param_version"] = "";
        }

        return config;
    }

    public List<ConfigDifference> CompareConfigs(
        Dictionary<string, object> config1,
        Dictionary<string, object> config2)
    {
        var differences = new List<ConfigDifference>();

        // 获取所有键的并集
        var allKeys = config1.Keys.Union(config2.Keys).ToList();

        foreach (var key in allKeys)
        {
            var value1 = config1.ContainsKey(key) ? config1[key] : null;
            var value2 = config2.ContainsKey(key) ? config2[key] : null;

            // 值不同
            if (!Equals(value1, value2))
            {
                var diff = new ConfigDifference
                {
                    Field = key,
                    Standard = value2,
                    Actual = value1,
                    DifferenceType = value1 == null ? "removed" : (value2 == null ? "added" : "changed")
                };

                // 根据字段类型添加影响说明
                diff.Impact = GetImpactDescription(key, value2, value1);

                differences.Add(diff);
            }
        }

        return differences;
    }

    /// <summary>
    /// 获取差异影响说明
    /// </summary>
    private string? GetImpactDescription(string field, object? standard, object? actual)
    {
        // 根据字段名和差异值生成影响说明
        if (field.Contains("version"))
        {
            return "版本变更可能影响功能兼容性";
        }

        if (field.Contains("timeout") || field.Contains("time"))
        {
            if (standard != null && actual != null)
            {
                var standardStr = standard.ToString();
                var actualStr = actual.ToString();
                
                // 尝试解析时间值
                if (TryParseTime(standardStr, out var standardTime) &&
                    TryParseTime(actualStr, out var actualTime))
                {
                    if (actualTime < standardTime)
                    {
                        return "超时时间缩短可能导致超时错误";
                    }
                    else if (actualTime > standardTime)
                    {
                        return "超时时间延长可能影响性能";
                    }
                }
            }
        }

        if (field.Contains("filter") || field.Contains("threshold"))
        {
            return "过滤参数变更可能影响检测精度";
        }

        return null;
    }

    /// <summary>
    /// 尝试解析时间值（支持 ms、s 等单位）
    /// </summary>
    private bool TryParseTime(string? value, out int milliseconds)
    {
        milliseconds = 0;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        value = value.Trim().ToLower();
        
        if (value.EndsWith("ms"))
        {
            if (int.TryParse(value.Substring(0, value.Length - 2), out var ms))
            {
                milliseconds = ms;
                return true;
            }
        }
        else if (value.EndsWith("s"))
        {
            if (int.TryParse(value.Substring(0, value.Length - 1), out var seconds))
            {
                milliseconds = seconds * 1000;
                return true;
            }
        }
        else if (int.TryParse(value, out var num))
        {
            // 假设是毫秒
            milliseconds = num;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 映射到DTO
    /// </summary>
    private ConfigSnapshotDto MapToDto(DeviceConfigSnapshot snapshot)
    {
        return new ConfigSnapshotDto
        {
            SnapshotId = snapshot.SnapshotId,
            DeviceId = snapshot.DeviceId,
            SnapshotType = snapshot.SnapshotType,
            SnapshotAt = snapshot.SnapshotAt,
            ConfigJson = JsonSerializer.Deserialize<Dictionary<string, object>>(
                snapshot.ConfigJson.RootElement.GetRawText()) ?? new(),
            StandardConfigJson = snapshot.StandardConfigJson != null
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                    snapshot.StandardConfigJson.RootElement.GetRawText())
                : null,
            Differences = snapshot.Differences != null
                ? JsonSerializer.Deserialize<List<ConfigDifference>>(
                    snapshot.Differences.RootElement.GetRawText())
                : null,
            CreatedBy = snapshot.CreatedBy,
            CreatedAt = snapshot.CreatedAt
        };
    }
}

