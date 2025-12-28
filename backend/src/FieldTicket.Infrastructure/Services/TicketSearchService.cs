using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单全文搜索服务实现
/// </summary>
public class TicketSearchService : ITicketSearchService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TicketSearchService> _logger;

    public TicketSearchService(
        ApplicationDbContext dbContext,
        ILogger<TicketSearchService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SearchResultDto> SearchTicketsAsync(SearchRequest request)
    {
        _logger.LogInformation("Searching tickets with query: {Query}", request.Query);

        var query = _dbContext.Tickets.AsQueryable();

        // 应用筛选条件
        query = ApplyFilters(query, request);

        // 如果有搜索关键词，添加搜索条件
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerms = request.Query.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            // 使用 LINQ 进行模糊搜索（PostgreSQL 的 ILIKE）
            var searchQuery = query;
            foreach (var term in searchTerms)
            {
                var searchTerm = $"%{term}%";
                searchQuery = searchQuery.Where(t =>
                    (t.SymptomTitle != null && EF.Functions.ILike(t.SymptomTitle, searchTerm)) ||
                    (t.SymptomDetail != null && EF.Functions.ILike(t.SymptomDetail, searchTerm)) ||
                    (t.FactsJson != null && EF.Functions.ILike(t.FactsJson.ToString()!, searchTerm)) ||
                    (t.ActionsTakenNote != null && EF.Functions.ILike(t.ActionsTakenNote, searchTerm))
                );
            }
            query = searchQuery;
        }

        // 获取总数
        var total = await query.CountAsync();

        // 分页查询
        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new
            {
                t.TicketId,
                t.TicketNo,
                t.Status,
                t.Domain,
                t.StepCode,
                t.SymptomTitle,
                t.SymptomDetail,
                t.Priority,
                t.CustomerName,
                t.DeviceSn,
                t.CreatedByUserId,
                t.CreatedAt
            })
            .ToListAsync();

        // 获取创建人名称
        var userIds = tickets.Select(t => t.CreatedByUserId).Distinct().ToList();
        var users = await _dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        // 构建结果
        var items = tickets.Select(t => new TicketSearchResultItem
        {
            TicketId = t.TicketId,
            TicketNo = t.TicketNo,
            Status = t.Status,
            Domain = t.Domain.ToString(),
            StepCode = t.StepCode,
            SymptomTitle = t.SymptomTitle,
            SymptomDetail = t.SymptomDetail,
            Priority = t.Priority,
            CustomerName = t.CustomerName,
            DeviceSn = t.DeviceSn,
            CreatedByName = users.GetValueOrDefault(t.CreatedByUserId),
            CreatedAt = t.CreatedAt,
            RelevanceScore = 0,
            Matches = !string.IsNullOrWhiteSpace(request.Query)
                ? ExtractMatches(request.Query, t.SymptomTitle, t.SymptomDetail)
                : new List<string>()
        }).ToList();

        return new SearchResultDto
        {
            Items = items,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<string>> GetSearchSuggestionsAsync(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new List<string>();
        }

        // 从工单标题中提取建议
        var suggestions = await _dbContext.Tickets
            .Where(t => t.SymptomTitle != null && t.SymptomTitle.Contains(query))
            .Select(t => t.SymptomTitle!)
            .Distinct()
            .Take(limit)
            .ToListAsync();

        return suggestions;
    }

    private IQueryable<Ticket> ApplyFilters(IQueryable<Ticket> query, SearchRequest request)
    {
        if (request.Statuses != null && request.Statuses.Any())
        {
            query = query.Where(t => request.Statuses.Contains(t.Status));
        }

        if (!string.IsNullOrEmpty(request.Domain))
        {
            query = query.Where(t => t.Domain.ToString() == request.Domain);
        }

        if (!string.IsNullOrEmpty(request.Priority))
        {
            query = query.Where(t => t.Priority == request.Priority);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(t => t.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrEmpty(request.DeviceSn))
        {
            query = query.Where(t => t.DeviceSn == request.DeviceSn);
        }

        if (request.CreatedBy.HasValue)
        {
            query = query.Where(t => t.CreatedByUserId == request.CreatedBy.Value);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= request.DateTo.Value);
        }

        return query;
    }


    private List<string> ExtractMatches(string query, string? title, string? detail)
    {
        var matches = new List<string>();
        var terms = query.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (!string.IsNullOrEmpty(title))
        {
            foreach (var term in terms)
            {
                if (title.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    var index = title.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                    var start = Math.Max(0, index - 20);
                    var length = Math.Min(title.Length - start, term.Length + 40);
                    var snippet = title.Substring(start, length);
                    if (!matches.Contains(snippet))
                    {
                        matches.Add(snippet);
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(detail))
        {
            foreach (var term in terms)
            {
                if (detail.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    var index = detail.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                    var start = Math.Max(0, index - 20);
                    var length = Math.Min(detail.Length - start, term.Length + 40);
                    var snippet = detail.Substring(start, length);
                    if (!matches.Contains(snippet))
                    {
                        matches.Add(snippet);
                    }
                }
            }
        }

        return matches.Take(3).ToList();
    }
}

