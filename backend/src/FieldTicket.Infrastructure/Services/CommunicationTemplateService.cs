using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 客户沟通模板服务实现
/// </summary>
public class CommunicationTemplateService : ICommunicationTemplateService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CommunicationTemplateService> _logger;

    public CommunicationTemplateService(
        ApplicationDbContext dbContext,
        ILogger<CommunicationTemplateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<CommunicationTemplateDto>> GetTemplatesAsync(string? scenario = null)
    {
        var query = _dbContext.CommunicationTemplates
            .Where(t => t.IsActive);

        if (!string.IsNullOrEmpty(scenario))
        {
            query = query.Where(t => t.Scenario == scenario);
        }

        var templates = await query
            .OrderBy(t => t.Scenario)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return templates.Select(MapToDto).ToList();
    }

    public async Task<CommunicationTemplateDto?> GetTemplateAsync(Guid templateId)
    {
        var template = await _dbContext.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId);

        return template != null ? MapToDto(template) : null;
    }

    public async Task<CommunicationTemplateDto?> GetTemplateByCodeAsync(string code)
    {
        var template = await _dbContext.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.Code == code);

        return template != null ? MapToDto(template) : null;
    }

    public async Task<Guid> CreateTemplateAsync(CreateCommunicationTemplateRequest request, Guid userId)
    {
        var template = new CommunicationTemplate
        {
            TemplateId = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            Scenario = request.Scenario,
            ContentTemplate = request.ContentTemplate,
            IsActive = request.IsActive,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.CommunicationTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("创建沟通模板：{Code} - {Name}", request.Code, request.Name);

        return template.TemplateId;
    }

    public async Task UpdateTemplateAsync(Guid templateId, UpdateCommunicationTemplateRequest request)
    {
        var template = await _dbContext.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId);

        if (template == null)
        {
            throw new KeyNotFoundException($"模板 {templateId} 不存在");
        }

        if (request.Name != null)
        {
            template.Name = request.Name;
        }
        if (request.Scenario != null)
        {
            template.Scenario = request.Scenario;
        }
        if (request.ContentTemplate != null)
        {
            template.ContentTemplate = request.ContentTemplate;
        }
        if (request.IsActive.HasValue)
        {
            template.IsActive = request.IsActive.Value;
        }

        template.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("更新沟通模板：{TemplateId}", templateId);
    }

    public async Task DeleteTemplateAsync(Guid templateId)
    {
        var template = await _dbContext.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId);

        if (template == null)
        {
            throw new KeyNotFoundException($"模板 {templateId} 不存在");
        }

        _dbContext.CommunicationTemplates.Remove(template);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("删除沟通模板：{TemplateId}", templateId);
    }

    public async Task<string> RenderTemplateAsync(Guid templateId, Dictionary<string, string> variables)
    {
        var template = await _dbContext.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId);

        if (template == null)
        {
            throw new KeyNotFoundException($"模板 {templateId} 不存在");
        }

        var content = template.ContentTemplate;

        // 替换变量 {variable_name}
        foreach (var variable in variables)
        {
            content = content.Replace($"{{{variable.Key}}}", variable.Value);
        }

        return content;
    }

    public async Task<Guid> SaveCommunicationAsync(CreateCommunicationRequest request, Guid userId)
    {
        // 映射沟通类型（小程序使用 call/message/email/other，后端使用 wechat/phone/email/other）
        var communicationType = request.CommunicationType switch
        {
            "call" => "phone",
            "message" => "wechat",
            "email" => "email",
            "other" => "other",
            _ => request.CommunicationType // 如果已经是后端格式，直接使用
        };

        var communication = new CustomerCommunication
        {
            CommunicationId = Guid.NewGuid(),
            TicketId = request.TicketId,
            TemplateId = request.TemplateId,
            Content = request.Content,
            CommunicationType = communicationType,
            CommunicatedBy = userId,
            CommunicatedAt = DateTime.UtcNow,
            CustomerFeedback = request.CustomerFeedback,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CustomerCommunications.Add(communication);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("保存沟通记录：工单 {TicketId}，类型 {Type}", request.TicketId, request.CommunicationType);

        return communication.CommunicationId;
    }

    public async Task<List<CustomerCommunicationDto>> GetTicketCommunicationsAsync(Guid ticketId)
    {
        var communications = await _dbContext.CustomerCommunications
            .Where(c => c.TicketId == ticketId)
            .OrderByDescending(c => c.CommunicatedAt)
            .ToListAsync();

        var result = new List<CustomerCommunicationDto>();

        foreach (var comm in communications)
        {
            // 映射沟通类型（后端使用 wechat/phone/email/other，小程序使用 call/message/email/other）
            var communicationType = comm.CommunicationType switch
            {
                "phone" => "call",
                "wechat" => "message",
                "email" => "email",
                "other" => "other",
                _ => comm.CommunicationType
            };

            var dto = new CustomerCommunicationDto
            {
                CommunicationId = comm.CommunicationId,
                TicketId = comm.TicketId,
                TemplateId = comm.TemplateId,
                Content = comm.Content,
                CommunicationType = communicationType,
                CommunicatedBy = comm.CommunicatedBy,
                CommunicatedAt = comm.CommunicatedAt,
                CustomerFeedback = comm.CustomerFeedback
            };

            // 获取模板名称
            if (comm.TemplateId.HasValue)
            {
                var template = await _dbContext.CommunicationTemplates
                    .FirstOrDefaultAsync(t => t.TemplateId == comm.TemplateId.Value);
                if (template != null)
                {
                    dto.TemplateName = template.Name;
                }
            }

            // 获取沟通人名称
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == comm.CommunicatedBy);
            if (user != null)
            {
                dto.CommunicatedByName = user.Name;
            }

            result.Add(dto);
        }

        return result;
    }

    private CommunicationTemplateDto MapToDto(CommunicationTemplate template)
    {
        return new CommunicationTemplateDto
        {
            TemplateId = template.TemplateId,
            Code = template.Code,
            Name = template.Name,
            Scenario = template.Scenario,
            ContentTemplate = template.ContentTemplate,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}


