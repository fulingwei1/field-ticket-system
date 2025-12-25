using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单编号生成服务
/// </summary>
public class TicketNumberService
{
    private readonly IDistributedCache _cache;
    private const string CounterKeyPrefix = "ticket_counter:";

    public TicketNumberService(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 生成工单编号：TK-YYYYMMDD-NNN
    /// </summary>
    public async Task<string> GenerateTicketNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var dateStr = today.ToString("yyyyMMdd");
        var counterKey = $"{CounterKeyPrefix}{dateStr}";

        // 获取或初始化计数器
        var counterStr = await _cache.GetStringAsync(counterKey);
        int counter = 1;

        if (!string.IsNullOrEmpty(counterStr) && int.TryParse(counterStr, out var existingCounter))
        {
            counter = existingCounter + 1;
        }

        // 更新计数器（24小时过期）
        await _cache.SetStringAsync(
            counterKey,
            counter.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
            });

        // 生成编号：TK-YYYYMMDD-NNN（NNN 为3位数字，不足补0）
        var ticketNo = $"TK-{dateStr}-{counter:D3}";
        return ticketNo;
    }
}


