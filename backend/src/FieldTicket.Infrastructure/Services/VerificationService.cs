using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
        if (request.SolutionId.HasValue)
        {
            var solution = await _dbContext.Solutions
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

        // 验证数据一致性
        if (request.RunCount != request.PassCount + request.FailCount)
        {
            throw new ArgumentException("验证次数必须等于通过次数加失败次数");
        }

        // 计算验证结果
        var result = CalculateVerificationResult(request);

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
    /// 计算验证结果
    /// </summary>
    private string CalculateVerificationResult(SubmitVerificationRequest request)
    {
        // 检查验证清单结果
        var checklistResult = request.ChecklistResultJson;
        var checklistRoot = checklistResult.RootElement;

        // 获取解决方案的验证清单（如果有）
        // TODO: 从解决方案获取验证清单，检查必填项

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
            // 检查所有必填项是否完成
            // TODO: 从解决方案验证清单检查必填项
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
