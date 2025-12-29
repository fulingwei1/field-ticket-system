using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单模板服务实现
/// </summary>
public class TicketTemplateService : ITicketTemplateService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TicketTemplateService> _logger;

    public TicketTemplateService(
        ApplicationDbContext dbContext,
        ILogger<TicketTemplateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TicketTemplateDto> CreateTemplateAsync(CreateTicketTemplateRequest request, Guid userId)
    {
        var template = new TicketTemplate
        {
            TemplateId = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            CreatedByUserId = userId,
            IsPublic = request.IsPublic,
            TemplateData = request.TemplateData,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UsageCount = 0,
            IsDeleted = false
        };

        _dbContext.TicketTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("创建工单模板 {TemplateId}: {Name}", template.TemplateId, template.Name);

        return await MapToDtoAsync(template);
    }

    public async Task<TicketTemplateDto> CreateTemplateFromTicketAsync(Guid ticketId, string name, string? description, Guid userId)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 从工单提取模板数据
        var templateData = new Dictionary<string, object?>
        {
            ["domain"] = ticket.Domain.ToString(),
            ["stepCode"] = ticket.StepCode,
            ["stepName"] = ticket.StepName,
            ["symptomTitle"] = ticket.SymptomTitle,
            ["symptomDetail"] = ticket.SymptomDetail,
            ["reproRate"] = ticket.ReproRate,
            ["rebootRecovers"] = ticket.RebootRecovers,
            ["envRelated"] = ticket.EnvRelated,
            ["swVersion"] = ticket.SwVersion,
            ["plcVersion"] = ticket.PlcVersion,
            ["paramVersion"] = ticket.ParamVersion,
            ["factsJson"] = ticket.FactsJson,
            ["actionsTaken"] = ticket.ActionsTaken,
            ["actionsTakenNote"] = ticket.ActionsTakenNote,
            ["alarmCode"] = ticket.AlarmCode
        };

        var templateDataJson = JsonSerializer.Serialize(templateData);
        var templateDataDoc = JsonDocument.Parse(templateDataJson);

        var createRequest = new CreateTicketTemplateRequest
        {
            Name = name,
            Description = description,
            Category = $"问题域{ticket.Domain}",
            IsPublic = false,
            TemplateData = templateDataDoc
        };

        return await CreateTemplateAsync(createRequest, userId);
    }

    public async Task<TicketTemplateDto> UpdateTemplateAsync(Guid templateId, UpdateTicketTemplateRequest request, Guid userId)
    {
        var template = await _dbContext.TicketTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId && !t.IsDeleted);

        if (template == null)
        {
            throw new KeyNotFoundException($"模板 {templateId} 不存在");
        }

        // 权限检查：只能修改自己创建的模板，除非是公开模板
        if (template.CreatedByUserId != userId && !template.IsPublic)
        {
            throw new UnauthorizedAccessException("无权修改此模板");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            template.Name = request.Name;
        }
        if (request.Description != null)
        {
            template.Description = request.Description;
        }
        if (request.Category != null)
        {
            template.Category = request.Category;
        }
        if (request.IsPublic.HasValue)
        {
            template.IsPublic = request.IsPublic.Value;
        }
        if (request.TemplateData != null)
        {
            template.TemplateData = request.TemplateData;
        }

        template.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("更新工单模板 {TemplateId}: {Name}", template.TemplateId, template.Name);

        return await MapToDtoAsync(template);
    }

    public async Task DeleteTemplateAsync(Guid templateId, Guid userId)
    {
        var template = await _dbContext.TicketTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId && !t.IsDeleted);

        if (template == null)
        {
            throw new KeyNotFoundException($"模板 {templateId} 不存在");
        }

        // 权限检查：只能删除自己创建的模板
        if (template.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("无权删除此模板");
        }

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("删除工单模板 {TemplateId}: {Name}", template.TemplateId, template.Name);
    }

    public async Task<TicketTemplateDto> GetTemplateAsync(Guid templateId, Guid userId)
    {
        var template = await _dbContext.TicketTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId && !t.IsDeleted);

        if (template == null)
        {
            throw new KeyNotFoundException($"模板 {templateId} 不存在");
        }

        // 权限检查：只能查看自己创建的模板或公开模板
        if (template.CreatedByUserId != userId && !template.IsPublic)
        {
            throw new UnauthorizedAccessException("无权查看此模板");
        }

        return await MapToDtoAsync(template);
    }

    public async Task<List<TicketTemplateDto>> GetTemplatesAsync(Guid userId, string? category = null, bool includePublic = true)
    {
        var query = _dbContext.TicketTemplates
            .Where(t => !t.IsDeleted)
            .Where(t => t.CreatedByUserId == userId || (includePublic && t.IsPublic));

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }

        var templates = await query
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();

        var result = new List<TicketTemplateDto>();
        foreach (var template in templates)
        {
            result.Add(await MapToDtoAsync(template));
        }

        return result;
    }

    public async Task<CreateTicketRequest> ApplyTemplateAsync(Guid templateId, Guid? deviceId, Guid userId)
    {
        var template = await GetTemplateAsync(templateId, userId);

        // 更新使用统计
        var templateEntity = await _dbContext.TicketTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId);
        if (templateEntity != null)
        {
            templateEntity.UsageCount++;
            templateEntity.LastUsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        // 从模板数据创建工单请求
        var templateData = template.TemplateData;
        var root = templateData.RootElement;

        var createRequest = new CreateTicketRequest
        {
            DeviceId = deviceId ?? Guid.Empty, // 如果未提供设备ID，需要用户后续填写
            Domain = root.TryGetProperty("domain", out var domainProp) 
                ? domainProp.GetString()?[0] ?? 'A' 
                : 'A',
            StepCode = root.TryGetProperty("stepCode", out var stepCodeProp) 
                ? stepCodeProp.GetString() ?? string.Empty 
                : string.Empty,
            StepName = root.TryGetProperty("stepName", out var stepNameProp) 
                ? stepNameProp.GetString() 
                : null,
            SymptomTitle = root.TryGetProperty("symptomTitle", out var symptomTitleProp) 
                ? symptomTitleProp.GetString() ?? string.Empty 
                : string.Empty,
            SymptomDetail = root.TryGetProperty("symptomDetail", out var symptomDetailProp) 
                ? symptomDetailProp.GetString() 
                : null,
            ReproRate = root.TryGetProperty("reproRate", out var reproRateProp) 
                ? reproRateProp.GetInt32() 
                : null,
            RebootRecovers = root.TryGetProperty("rebootRecovers", out var rebootRecoversProp) 
                ? rebootRecoversProp.GetBoolean() 
                : null,
            EnvRelated = root.TryGetProperty("envRelated", out var envRelatedProp) 
                ? envRelatedProp.GetBoolean() 
                : null,
            SwVersion = root.TryGetProperty("swVersion", out var swVersionProp) 
                ? swVersionProp.GetString() ?? string.Empty 
                : string.Empty,
            PlcVersion = root.TryGetProperty("plcVersion", out var plcVersionProp) 
                ? plcVersionProp.GetString() ?? string.Empty 
                : string.Empty,
            ParamVersion = root.TryGetProperty("paramVersion", out var paramVersionProp) 
                ? paramVersionProp.GetString() ?? string.Empty 
                : string.Empty,
            FactsJson = root.TryGetProperty("factsJson", out var factsJsonProp)
                ? JsonDocument.Parse(factsJsonProp.GetRawText())
                : JsonDocument.Parse("{}"),
            ActionsTaken = root.TryGetProperty("actionsTaken", out var actionsTakenProp) 
                ? actionsTakenProp.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList() 
                : new List<string>(),
            ActionsTakenNote = root.TryGetProperty("actionsTakenNote", out var actionsTakenNoteProp) 
                ? actionsTakenNoteProp.GetString() 
                : null,
            AlarmCode = root.TryGetProperty("alarmCode", out var alarmCodeProp) 
                ? alarmCodeProp.GetString() 
                : null,
            ConfirmedAsFact = false // 使用模板时，需要用户确认事实
        };

        _logger.LogInformation("应用工单模板 {TemplateId} 创建工单草稿", templateId);

        return createRequest;
    }

    private async Task<TicketTemplateDto> MapToDtoAsync(TicketTemplate template)
    {
        var dto = new TicketTemplateDto
        {
            TemplateId = template.TemplateId,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            CreatedByUserId = template.CreatedByUserId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            UsageCount = template.UsageCount,
            LastUsedAt = template.LastUsedAt,
            IsPublic = template.IsPublic,
            TemplateData = template.TemplateData
        };

        // 获取创建者用户名（如果有）
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == template.CreatedByUserId);
        if (user != null)
        {
            dto.CreatedByUserName = user.Name;
        }

        return dto;
    }
}


















