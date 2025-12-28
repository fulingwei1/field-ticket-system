using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 验证服务实现
/// </summary>
public class VerificationService : IVerificationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        ApplicationDbContext dbContext,
        ILogger<VerificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<VerificationDto> SubmitVerificationAsync(Guid ticketId, SubmitVerificationRequest request, Guid userId)
    {
        // 验证工单存在
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 验证工单状态（必须是 SolutionIssued）
        if (ticket.Status != "SolutionIssued")
        {
            throw new InvalidOperationException($"工单状态必须是 SolutionIssued，当前状态：{ticket.Status}");
        }

        // 如果指定了解决方案ID，验证解决方案存在
        Solution? solution = null;
        if (request.SolutionId.HasValue)
        {
            solution = await _dbContext.Solutions
                .FirstOrDefaultAsync(s => s.SolutionId == request.SolutionId.Value);

            if (solution == null)
            {
                throw new KeyNotFoundException($"解决方案 {request.SolutionId} 不存在");
            }

            if (solution.TicketId != ticketId)
            {
                throw new InvalidOperationException("解决方案不属于该工单");
            }
        }
        else
        {
            // 如果没有指定解决方案ID，尝试从工单获取最新的解决方案
            solution = await _dbContext.Solutions
                .Where(s => s.TicketId == ticketId && s.Status == "Published")
                .OrderByDescending(s => s.PublishedAt ?? s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        // 验证数据一致性
        if (request.RunCount != request.PassCount + request.FailCount)
        {
            throw new ArgumentException("验证次数必须等于通过次数加失败次数");
        }

        // 检查验证清单必填项
        if (solution != null)
        {
            ValidateChecklistRequiredItems(solution.VerificationChecklistJson, request.ChecklistResultJson);
        }

        // 计算验证结果
        var result = CalculateVerificationResult(request, solution);

        // 创建验证记录
        var verification = new Verification
        {
            VerificationId = Guid.NewGuid(),
            TicketId = ticketId,
            SolutionId = request.SolutionId,
            ExecutedBy = userId,
            RunCount = request.RunCount,
            PassCount = request.PassCount,
            FailCount = request.FailCount,
            Result = result,
            ChecklistResultJson = request.ChecklistResultJson,
            EvidenceAttachmentIds = request.EvidenceAttachmentIds,
            Note = request.Note,
            VerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Verifications.Add(verification);

        // 更新工单状态
        ticket.Status = "Verifying";
        ticket.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("验证结果已提交，工单 {TicketId} 状态更新为 Verifying，验证结果：{Result}", 
            ticketId, result);

        return await MapToDtoAsync(verification);
    }

    public async Task<List<VerificationDto>> GetVerificationHistoryAsync(Guid ticketId)
    {
        var verifications = await _dbContext.Verifications
            .Include(v => v.Executor)
            .Where(v => v.TicketId == ticketId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        var result = new List<VerificationDto>();
        foreach (var verification in verifications)
        {
            result.Add(await MapToDtoAsync(verification));
        }

        return result;
    }

    public async Task<VerificationDto?> GetVerificationAsync(Guid verificationId)
    {
        var verification = await _dbContext.Verifications
            .Include(v => v.Executor)
            .FirstOrDefaultAsync(v => v.VerificationId == verificationId);

        if (verification == null)
        {
            return null;
        }

        return await MapToDtoAsync(verification);
    }

    /// <summary>
    /// 验证验证清单必填项
    /// </summary>
    private void ValidateChecklistRequiredItems(JsonDocument checklistTemplate, JsonDocument checklistResult)
    {
        var templateRoot = checklistTemplate.RootElement;
        var resultRoot = checklistResult.RootElement;

        // 如果验证清单模板为空，跳过检查
        if (templateRoot.ValueKind != JsonValueKind.Object && templateRoot.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var missingRequiredItems = new List<string>();

        // 验证清单模板可能是数组格式或对象格式
        if (templateRoot.ValueKind == JsonValueKind.Array)
        {
            // 数组格式：每个项是一个检查项
            foreach (var item in templateRoot.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var field = item.TryGetProperty("field", out var fieldProp) 
                        ? fieldProp.GetString() 
                        : null;
                    var required = item.TryGetProperty("required", out var requiredProp) 
                        && requiredProp.GetBoolean();
                    var question = item.TryGetProperty("question", out var questionProp) 
                        ? questionProp.GetString() 
                        : null;

                    if (required && !string.IsNullOrEmpty(field))
                    {
                        // 检查结果中是否包含该字段
                        if (!resultRoot.TryGetProperty(field, out var resultValue))
                        {
                            missingRequiredItems.Add(question ?? field);
                        }
                        else
                        {
                            // 检查值是否为空
                            if (resultValue.ValueKind == JsonValueKind.Null ||
                                (resultValue.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(resultValue.GetString())) ||
                                (resultValue.ValueKind == JsonValueKind.Array && resultValue.GetArrayLength() == 0))
                            {
                                missingRequiredItems.Add(question ?? field);
                            }
                        }
                    }
                }
            }
        }
        else if (templateRoot.ValueKind == JsonValueKind.Object)
        {
            // 对象格式：遍历所有属性
            foreach (var prop in templateRoot.EnumerateObject())
            {
                var fieldName = prop.Name;
                var fieldValue = prop.Value;

                // 检查是否是必填项
                if (fieldValue.ValueKind == JsonValueKind.Object)
                {
                    var required = fieldValue.TryGetProperty("required", out var requiredProp) 
                        && requiredProp.GetBoolean();
                    var question = fieldValue.TryGetProperty("question", out var questionProp) 
                        ? questionProp.GetString() 
                        : null;

                    if (required)
                    {
                        // 检查结果中是否包含该字段
                        if (!resultRoot.TryGetProperty(fieldName, out var resultValue))
                        {
                            missingRequiredItems.Add(question ?? fieldName);
                        }
                        else
                        {
                            // 检查值是否为空
                            if (resultValue.ValueKind == JsonValueKind.Null ||
                                (resultValue.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(resultValue.GetString())) ||
                                (resultValue.ValueKind == JsonValueKind.Array && resultValue.GetArrayLength() == 0))
                            {
                                missingRequiredItems.Add(question ?? fieldName);
                            }
                        }
                    }
                }
            }
        }

        // 如果有缺失的必填项，抛出异常
        if (missingRequiredItems.Any())
        {
            var missingItemsStr = string.Join("、", missingRequiredItems);
            throw new ArgumentException($"验证清单必填项未完成：{missingItemsStr}。请完成所有必填项后再提交验证结果。");
        }
    }

    /// <summary>
    /// 计算验证结果
    /// </summary>
    private string CalculateVerificationResult(SubmitVerificationRequest request, Solution? solution)
    {
        // 计算通过率
        var passRate = request.RunCount > 0 
            ? (double)request.PassCount / request.RunCount 
            : 0.0;

        // 判断结果
        if (request.RunCount == 0)
        {
            return "FAIL"; // 没有验证次数，判定为失败
        }

        if (passRate == 1.0)
        {
            // 如果通过率是 100%，且验证清单必填项已检查（在 ValidateChecklistRequiredItems 中已检查），返回 PASS
            return "PASS";
        }
        else if (passRate > 0.0)
        {
            return "PARTIAL";
        }
        else
        {
            return "FAIL";
        }
    }

    private async Task<VerificationDto> MapToDtoAsync(Verification verification)
    {
        var executor = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == verification.ExecutedBy);

        return new VerificationDto
        {
            VerificationId = verification.VerificationId,
            TicketId = verification.TicketId,
            SolutionId = verification.SolutionId,
            ExecutedBy = verification.ExecutedBy,
            ExecutedByName = executor?.Name,
            RunCount = verification.RunCount,
            PassCount = verification.PassCount,
            FailCount = verification.FailCount,
            Result = verification.Result,
            ChecklistResultJson = verification.ChecklistResultJson,
            EvidenceAttachmentIds = verification.EvidenceAttachmentIds,
            Note = verification.Note,
            VerifiedAt = verification.VerifiedAt,
            CreatedAt = verification.CreatedAt,
            UpdatedAt = verification.UpdatedAt
        };
    }
}
