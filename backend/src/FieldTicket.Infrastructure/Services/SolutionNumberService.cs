using Microsoft.Extensions.Caching.Distributed;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 解决方案编号生成服务
/// </summary>
public class SolutionNumberService
{
    private readonly IDistributedCache _cache;
    private const string CounterKeyPrefix = "solution_counter:";

    public SolutionNumberService(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 生成解决方案编号：SOL-YYYY-NNN
    /// </summary>
    public async Task<string> GenerateSolutionNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var counterKey = $"{CounterKeyPrefix}{year}";

        // 获取或初始化计数器
        var counterStr = await _cache.GetStringAsync(counterKey);
        int counter = 1;

        if (!string.IsNullOrEmpty(counterStr) && int.TryParse(counterStr, out var existingCounter))
        {
            counter = existingCounter + 1;
        }

        // 更新计数器（1年过期）
        await _cache.SetStringAsync(
            counterKey,
            counter.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
            });

        // 生成编号：SOL-YYYY-NNN（NNN 为3位数字，不足补0）
        var solutionCode = $"SOL-{year}-{counter:D3}";
        return solutionCode;
    }
}


