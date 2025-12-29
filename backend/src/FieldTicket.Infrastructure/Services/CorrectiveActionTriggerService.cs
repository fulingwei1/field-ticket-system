using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 整改任务触发服务实现
/// </summary>
public class CorrectiveActionTriggerService : ICorrectiveActionTriggerService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CorrectiveActionTriggerService> _logger;

    // 默认触发规则
    private const int DefaultDays = 30;
    private const int DefaultCount = 3;

    public CorrectiveActionTriggerService(
        ApplicationDbContext dbContext,
        ILogger<CorrectiveActionTriggerService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<CorrectiveActionTriggerDto>> CheckTriggersAsync(Guid ticketId)
    {
        _logger.LogInformation("Checking triggers for ticket {TicketId}", ticketId);

        var triggers = new List<CorrectiveActionTriggerDto>();

        // 获取工单信息
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            return triggers;
        }

        // 检查阈值触发（30天内相同问题出现3次）
        var thresholdTrigger = await CheckThresholdTriggerAsync(ticket);
        if (thresholdTrigger != null)
        {
            triggers.Add(thresholdTrigger);
        }

        return triggers;
    }

    private async Task<CorrectiveActionTriggerDto?> CheckThresholdTriggerAsync(Ticket ticket)
    {
        var fromDate = DateTime.UtcNow.AddDays(-DefaultDays);
        var toDate = DateTime.UtcNow;

        // 查找相似工单（相同设备、相同症状、相同根因）
        var query = _dbContext.Tickets
            .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
            .Where(t => t.TicketId != ticket.TicketId);

        // 匹配条件：相同设备
        if (!string.IsNullOrEmpty(ticket.DeviceSn))
        {
            query = query.Where(t => t.DeviceSn == ticket.DeviceSn);
        }

        // 匹配条件：相同症状（标题相似）
        if (!string.IsNullOrEmpty(ticket.SymptomTitle))
        {
            query = query.Where(t => t.SymptomTitle == ticket.SymptomTitle);
        }

        // 匹配条件：相同根因（如果已归因）
        if (!string.IsNullOrEmpty(ticket.RootResponsibility))
        {
            query = query.Where(t => t.RootResponsibility == ticket.RootResponsibility);
        }

        var similarTickets = await query
            .Select(t => t.TicketId)
            .ToListAsync();

        // 检查是否达到阈值
        if (similarTickets.Count >= DefaultCount - 1) // -1 因为不包括当前工单
        {
            var relatedTicketIds = similarTickets.Take(DefaultCount - 1).ToList();
            relatedTicketIds.Add(ticket.TicketId);

            return new CorrectiveActionTriggerDto
            {
                ShouldTrigger = true,
                Reason = $"在{DefaultDays}天内，相同问题已出现{relatedTicketIds.Count}次，达到阈值{DefaultCount}次",
                RelatedTicketIds = relatedTicketIds,
                TriggerData = new Dictionary<string, object>
                {
                    { "days", DefaultDays },
                    { "count", relatedTicketIds.Count },
                    { "threshold", DefaultCount },
                    { "match_criteria", new Dictionary<string, bool>
                    {
                        { "same_device", !string.IsNullOrEmpty(ticket.DeviceSn) },
                        { "same_symptom", !string.IsNullOrEmpty(ticket.SymptomTitle) },
                        { "same_root_responsibility", !string.IsNullOrEmpty(ticket.RootResponsibility) }
                    }}
                }
            };
        }

        return null;
    }
}



















