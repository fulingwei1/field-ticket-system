using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单关联分析服务实现
/// </summary>
public class TicketAssociationService : ITicketAssociationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly ILogger<TicketAssociationService> _logger;

    public TicketAssociationService(
        ApplicationDbContext dbContext,
        IDuplicateDetectionService duplicateDetectionService,
        ILogger<TicketAssociationService> logger)
    {
        _dbContext = dbContext;
        _duplicateDetectionService = duplicateDetectionService;
        _logger = logger;
    }

    public async Task<TicketAssociationDto> GetAssociationsAsync(Guid ticketId)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        var associations = new TicketAssociationDto
        {
            TicketId = ticketId
        };

        // 获取各类关联工单
        associations.SimilarTickets = await GetSimilarTicketsAsync(ticketId);
        associations.DeviceRelatedTickets = await GetDeviceRelatedTicketsAsync(ticketId);
        associations.CustomerRelatedTickets = await GetCustomerRelatedTicketsAsync(ticketId);
        associations.DomainRelatedTickets = await GetDomainRelatedTicketsAsync(ticketId);

        // 计算总数（去重）
        var allTicketIds = new HashSet<Guid>();
        allTicketIds.UnionWith(associations.SimilarTickets.Select(t => t.TicketId));
        allTicketIds.UnionWith(associations.DeviceRelatedTickets.Select(t => t.TicketId));
        allTicketIds.UnionWith(associations.CustomerRelatedTickets.Select(t => t.TicketId));
        allTicketIds.UnionWith(associations.DomainRelatedTickets.Select(t => t.TicketId));
        associations.TotalCount = allTicketIds.Count;

        return associations;
    }

    public async Task<List<AssociatedTicketDto>> GetSimilarTicketsAsync(Guid ticketId, int maxResults = 10)
    {
        try
        {
            var candidates = await _duplicateDetectionService.DetectDuplicatesAsync(ticketId, maxResults);
            
            return candidates.Select(c => new AssociatedTicketDto
            {
                TicketId = c.TicketId,
                TicketNo = c.TicketNo,
                SymptomTitle = c.SymptomTitle,
                Domain = c.Domain,
                StepCode = c.StepCode,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                AssociationType = "similar",
                SimilarityScore = c.Similarity.Overall,
                AssociationReason = $"相似度: {(c.Similarity.Overall * 100):F1}%"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取相似工单失败，工单 {TicketId}", ticketId);
            return new List<AssociatedTicketDto>();
        }
    }

    public async Task<List<AssociatedTicketDto>> GetDeviceRelatedTicketsAsync(Guid ticketId, int maxResults = 10)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            return new List<AssociatedTicketDto>();
        }

        // 获取同一设备的其他工单（排除自己、草稿、已合并的工单）
        var relatedTickets = await _dbContext.Tickets
            .Where(t => t.DeviceId == ticket.DeviceId &&
                       t.TicketId != ticketId &&
                       t.Status != "Draft" &&
                       t.MergedInto == null &&
                       t.DuplicateOf == null)
            .OrderByDescending(t => t.CreatedAt)
            .Take(maxResults)
            .ToListAsync();

        return relatedTickets.Select(t => new AssociatedTicketDto
        {
            TicketId = t.TicketId,
            TicketNo = t.TicketNo,
            SymptomTitle = t.SymptomTitle,
            Domain = t.Domain,
            StepCode = t.StepCode,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            ClosedAt = t.ClosedAt,
            AssociationType = "device",
            AssociationReason = "同一设备"
        }).ToList();
    }

    public async Task<List<AssociatedTicketDto>> GetCustomerRelatedTicketsAsync(Guid ticketId, int maxResults = 10)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            return new List<AssociatedTicketDto>();
        }

        // 获取同一客户的其他工单（排除自己、草稿、已合并的工单）
        var relatedTickets = await _dbContext.Tickets
            .Where(t => t.CustomerId == ticket.CustomerId &&
                       t.TicketId != ticketId &&
                       t.Status != "Draft" &&
                       t.MergedInto == null &&
                       t.DuplicateOf == null)
            .OrderByDescending(t => t.CreatedAt)
            .Take(maxResults)
            .ToListAsync();

        return relatedTickets.Select(t => new AssociatedTicketDto
        {
            TicketId = t.TicketId,
            TicketNo = t.TicketNo,
            SymptomTitle = t.SymptomTitle,
            Domain = t.Domain,
            StepCode = t.StepCode,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            ClosedAt = t.ClosedAt,
            AssociationType = "customer",
            AssociationReason = "同一客户"
        }).ToList();
    }

    public async Task<List<AssociatedTicketDto>> GetDomainRelatedTicketsAsync(Guid ticketId, int maxResults = 10)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            return new List<AssociatedTicketDto>();
        }

        // 获取同一问题域和步骤的其他工单（排除自己、草稿、已合并的工单）
        var relatedTickets = await _dbContext.Tickets
            .Where(t => t.Domain == ticket.Domain &&
                       t.StepCode == ticket.StepCode &&
                       t.TicketId != ticketId &&
                       t.Status != "Draft" &&
                       t.MergedInto == null &&
                       t.DuplicateOf == null)
            .OrderByDescending(t => t.CreatedAt)
            .Take(maxResults)
            .ToListAsync();

        return relatedTickets.Select(t => new AssociatedTicketDto
        {
            TicketId = t.TicketId,
            TicketNo = t.TicketNo,
            SymptomTitle = t.SymptomTitle,
            Domain = t.Domain,
            StepCode = t.StepCode,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            ClosedAt = t.ClosedAt,
            AssociationType = "domain",
            AssociationReason = $"相同问题域({ticket.Domain})和步骤({ticket.StepCode})"
        }).ToList();
    }
}





















