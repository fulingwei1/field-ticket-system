using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 缺失信息分析服务实现
/// </summary>
public class MissingInfoAnalysisService : IMissingInfoAnalysisService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MissingInfoAnalysisService> _logger;

    public MissingInfoAnalysisService(
        ApplicationDbContext dbContext,
        ILogger<MissingInfoAnalysisService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<MissingInfoItem>> AnalyzeMissingInfoAsync(Guid ticketId, string? jcCode = null)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        var missingInfo = new List<MissingInfoItem>();

        // 如果指定了判断卡，基于判断卡分析
        if (!string.IsNullOrEmpty(jcCode))
        {
            var jc = await _dbContext.JudgementCards
                .FirstOrDefaultAsync(j => j.JcCode == jcCode);

            if (jc != null)
            {
                missingInfo.AddRange(AnalyzeByJudgementCard(ticket, jc));
            }
        }

        // 基于工单基本信息分析（通用检查）
        missingInfo.AddRange(AnalyzeByTicketInfo(ticket));

        // 去重（按Field）
        missingInfo = missingInfo
            .GroupBy(m => m.Field)
            .Select(g => g.First())
            .ToList();

        return missingInfo;
    }

    public Task<List<QuestionItem>> GenerateQuestionnaireAsync(List<MissingInfoItem> missingInfo)
    {
        var questions = missingInfo.Select((item, index) => new QuestionItem
        {
            QuestionId = $"Q{index + 1}",
            Question = item.Question,
            Type = item.Type,
            Required = item.Required,
            Options = item.Options,
            Hint = item.Hint,
            Field = item.Field,
        }).ToList();

        return Task.FromResult(questions);
    }

    /// <summary>
    /// 基于判断卡分析缺失信息
    /// </summary>
    private List<MissingInfoItem> AnalyzeByJudgementCard(Ticket ticket, JudgementCard jc)
    {
        var missingInfo = new List<MissingInfoItem>();

        // 检查判断卡的症状结构中的关键信息
        if (jc.SymptomStructure.RootElement.ValueKind == JsonValueKind.Object)
        {
            var symptomStructure = jc.SymptomStructure.RootElement;
            
            // 检查是否有 key_checks 字段
            if (symptomStructure.TryGetProperty("key_checks", out var keyChecks) &&
                keyChecks.ValueKind == JsonValueKind.Array)
            {
                foreach (var check in keyChecks.EnumerateArray())
                {
                    if (check.ValueKind == JsonValueKind.Object)
                    {
                        var field = check.TryGetProperty("field", out var fieldProp) 
                            ? fieldProp.GetString() 
                            : null;
                        var required = check.TryGetProperty("required", out var requiredProp) 
                            && requiredProp.GetBoolean();
                        var question = check.TryGetProperty("question", out var questionProp) 
                            ? questionProp.GetString() 
                            : null;
                        var type = check.TryGetProperty("type", out var typeProp) 
                            ? typeProp.GetString() ?? "yes_no" 
                            : "yes_no";

                        if (!string.IsNullOrEmpty(field) && required)
                        {
                            // 检查工单中是否有该字段的值
                            var hasValue = CheckFieldValue(ticket.FactsJson, field);
                            
                            if (!hasValue)
                            {
                                missingInfo.Add(new MissingInfoItem
                                {
                                    Field = field,
                                    Question = question ?? $"请提供 {field} 的信息",
                                    Type = type,
                                    Required = required,
                                    Domain = check.TryGetProperty("domain", out var domainProp) 
                                        ? domainProp.GetString() 
                                        : null,
                                    Options = check.TryGetProperty("options", out var optionsProp) 
                                        ? optionsProp.EnumerateArray().Select(o => o.GetString() ?? string.Empty).ToList() 
                                        : null,
                                    Hint = check.TryGetProperty("hint", out var hintProp) 
                                        ? hintProp.GetString() 
                                        : null,
                                });
                            }
                        }
                    }
                }
            }
        }

        return missingInfo;
    }

    /// <summary>
    /// 基于工单基本信息分析缺失信息
    /// </summary>
    private List<MissingInfoItem> AnalyzeByTicketInfo(Ticket ticket)
    {
        var missingInfo = new List<MissingInfoItem>();

        // 检查症状详情
        if (string.IsNullOrWhiteSpace(ticket.SymptomDetail))
        {
            missingInfo.Add(new MissingInfoItem
            {
                Field = "symptom_detail",
                Question = "请详细描述问题的具体情况",
                Type = "text",
                Required = false,
                Hint = "包括问题发生的时间、频率、影响范围等",
            });
        }

        // 检查复现率
        if (!ticket.ReproRate.HasValue)
        {
            missingInfo.Add(new MissingInfoItem
            {
                Field = "repro_rate",
                Question = "问题复现率是多少？（0-100%）",
                Type = "number",
                Required = false,
                Hint = "例如：100%表示每次都会出现，50%表示偶尔出现",
            });
        }

        // 检查环境相关信息
        if (!ticket.EnvRelated.HasValue)
        {
            missingInfo.Add(new MissingInfoItem
            {
                Field = "env_related",
                Question = "问题是否与环境相关？",
                Type = "yes_no",
                Required = false,
            });
        }

        // 检查重启后是否恢复
        if (!ticket.RebootRecovers.HasValue)
        {
            missingInfo.Add(new MissingInfoItem
            {
                Field = "reboot_recovers",
                Question = "重启后问题是否恢复？",
                Type = "yes_no",
                Required = false,
            });
        }

        return missingInfo;
    }

    /// <summary>
    /// 检查字段是否有值
    /// </summary>
    private bool CheckFieldValue(JsonDocument factsJson, string field)
    {
        if (factsJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // 支持嵌套字段（如 "domain.mechanical.action_completed"）
        var parts = field.Split('.');
        var current = factsJson.RootElement;

        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!current.TryGetProperty(part, out var prop))
            {
                return false;
            }

            current = prop;
        }

        // 检查值是否存在且不为空
        if (current.ValueKind == JsonValueKind.Null)
        {
            return false;
        }

        if (current.ValueKind == JsonValueKind.String)
        {
            var value = current.GetString();
            return !string.IsNullOrWhiteSpace(value) && value != "NA" && value != "N/A";
        }

        if (current.ValueKind == JsonValueKind.Number)
        {
            return true;
        }

        if (current.ValueKind == JsonValueKind.True || current.ValueKind == JsonValueKind.False)
        {
            return true;
        }

        return false;
    }
}


