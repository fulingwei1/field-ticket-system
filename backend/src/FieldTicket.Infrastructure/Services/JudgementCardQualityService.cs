using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 判断卡质量评分服务实现
/// </summary>
public class JudgementCardQualityService : IJudgementCardQualityService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<JudgementCardQualityService> _logger;

    public JudgementCardQualityService(
        ApplicationDbContext dbContext,
        ILogger<JudgementCardQualityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<JudgementCardQualityScore> ScoreAsync(string jcCode)
    {
        var card = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode);

        if (card == null)
        {
            throw new KeyNotFoundException($"判断卡 {jcCode} 不存在");
        }

        var score = new JudgementCardQualityScore
        {
            JcCode = card.JcCode,
            Title = card.Title
        };

        // 1. 完整性检查（30分）
        score.CompletenessScore = CheckCompleteness(card, score.Issues);

        // 2. 逻辑一致性（30分）
        score.LogicConsistencyScore = CheckLogicConsistency(card, score.Issues);

        // 3. 可验证性（20分）
        score.VerifiabilityScore = CheckVerifiability(card, score.Issues);

        // 4. 证据支撑（20分）
        score.EvidenceScore = CheckEvidence(card, score.Issues);

        _logger.LogInformation("判断卡 {JcCode} 质量评分完成，总分：{TotalScore}，等级：{Level}",
            jcCode, score.TotalScore, score.Level);

        return score;
    }

    public async Task<List<JudgementCardQualityScore>> BatchScoreAsync(List<string> jcCodes)
    {
        var scores = new List<JudgementCardQualityScore>();

        foreach (var jcCode in jcCodes)
        {
            try
            {
                var score = await ScoreAsync(jcCode);
                scores.Add(score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量评分判断卡 {JcCode} 失败", jcCode);
            }
        }

        return scores;
    }

    public async Task<List<QualityIssue>> GetQualityIssuesAsync(string jcCode)
    {
        var score = await ScoreAsync(jcCode);
        return score.Issues;
    }

    /// <summary>
    /// 完整性检查（30分）
    /// </summary>
    private int CheckCompleteness(JudgementCard card, List<QualityIssue> issues)
    {
        int score = 30;

        // 检查假设模板
        if (string.IsNullOrWhiteSpace(card.HypothesisTemplate))
        {
            score -= 5;
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.MissingHypothesis,
                Description = "缺少假设模板",
                Severity = QualityIssueSeverity.Medium,
                Suggestion = "添加假设模板，描述可能的故障原因"
            });
        }

        // 检查排查路径中是否有排除原因
        var troubleshootingPath = card.TroubleshootingPath.RootElement;
        if (troubleshootingPath.ValueKind == JsonValueKind.Object)
        {
            var hasEliminatedCauses = troubleshootingPath.TryGetProperty("eliminated_causes", out _);
            if (!hasEliminatedCauses && !string.IsNullOrWhiteSpace(card.HypothesisTemplate))
            {
                score -= 5;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.MissingEliminatedCauses,
                    Description = "有假设但无排除原因列表",
                    Severity = QualityIssueSeverity.Medium,
                    Suggestion = "在排查路径中添加 eliminated_causes 字段，列出需要排除的原因"
                });
            }
        }

        // 检查升级条件
        if (card.EscalationConditions == null || card.EscalationConditions.RootElement.ValueKind == JsonValueKind.Null)
        {
            score -= 3;
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.MissingFailureModes,
                Description = "缺少升级条件",
                Severity = QualityIssueSeverity.Low,
                Suggestion = "添加升级条件，定义何时需要升级处理"
            });
        }

        // 检查症状结构
        var symptomStructure = card.SymptomStructure.RootElement;
        if (symptomStructure.ValueKind == JsonValueKind.Object)
        {
            var hasKeyChecks = symptomStructure.TryGetProperty("key_checks", out var keyChecks);
            if (!hasKeyChecks || keyChecks.GetArrayLength() == 0)
            {
                score -= 5;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.MissingKeyChecks,
                    Description = "缺少关键检查项",
                    Severity = QualityIssueSeverity.High,
                    Suggestion = "在症状结构中添加 key_checks 数组，列出需要检查的关键项"
                });
            }
        }

        // 检查下一步动作模板
        if (string.IsNullOrWhiteSpace(card.NextActionTemplate))
        {
            score -= 5;
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.UnverifiableAction,
                Description = "缺少下一步动作模板",
                Severity = QualityIssueSeverity.High,
                Suggestion = "添加下一步动作模板，指导工程师如何操作"
            });
        }

        // 检查问题域与动作的一致性
        if (!string.IsNullOrWhiteSpace(card.NextActionTemplate) && !string.IsNullOrWhiteSpace(card.HypothesisTemplate))
        {
            // 简单检查：如果假设提到某个域，但动作模板中没有相关提示，可能存在问题
            // 这里只是示例，实际可以根据业务逻辑更复杂地检查
        }

        return Math.Max(0, score);
    }

    /// <summary>
    /// 逻辑一致性检查（30分）
    /// </summary>
    private int CheckLogicConsistency(JudgementCard card, List<QualityIssue> issues)
    {
        int score = 30;

        // 检查判断边界是否完整
        var troubleshootingPath = card.TroubleshootingPath.RootElement;
        if (troubleshootingPath.ValueKind == JsonValueKind.Object)
        {
            var hasDecisionBoundary = troubleshootingPath.TryGetProperty("decision_boundary", out var decisionBoundary);
            if (!hasDecisionBoundary || decisionBoundary.ValueKind == JsonValueKind.Null)
            {
                score -= 10;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.IncompleteDecisionBoundary,
                    Description = "判断边界不完整",
                    Severity = QualityIssueSeverity.High,
                    Suggestion = "在排查路径中添加 decision_boundary，定义判断条件"
                });
            }
            else if (decisionBoundary.ValueKind == JsonValueKind.Array && decisionBoundary.GetArrayLength() == 0)
            {
                score -= 5;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.IncompleteDecisionBoundary,
                    Description = "判断边界为空",
                    Severity = QualityIssueSeverity.Medium,
                    Suggestion = "补充判断边界条件"
                });
            }
        }

        // 检查关键检查项是否覆盖所有维度
        var symptomStructure = card.SymptomStructure.RootElement;
        if (symptomStructure.ValueKind == JsonValueKind.Object)
        {
            if (symptomStructure.TryGetProperty("key_checks", out var keyChecks) &&
                keyChecks.ValueKind == JsonValueKind.Array)
            {
                var domains = new HashSet<char>();
                foreach (var check in keyChecks.EnumerateArray())
                {
                    if (check.TryGetProperty("domain", out var domain))
                    {
                        var domainStr = domain.GetString();
                        if (!string.IsNullOrEmpty(domainStr) && domainStr.Length == 1)
                        {
                            domains.Add(domainStr[0]);
                        }
                    }
                }

                // 如果判断卡的问题域不在检查项中，可能存在问题
                if (!domains.Contains(card.Domain))
                {
                    score -= 5;
                    issues.Add(new QualityIssue
                    {
                        Type = QualityIssueType.MissingKeyChecks,
                        Description = $"关键检查项未覆盖问题域 {card.Domain}",
                        Severity = QualityIssueSeverity.Medium,
                        Suggestion = $"添加问题域 {card.Domain} 相关的检查项"
                    });
                }
            }
        }

        // 检查失效模式是否有对应解决方案
        if (troubleshootingPath.ValueKind == JsonValueKind.Object)
        {
            var hasFailureModes = troubleshootingPath.TryGetProperty("failure_modes", out var failureModes);
            if (hasFailureModes && failureModes.ValueKind == JsonValueKind.Array)
            {
                foreach (var mode in failureModes.EnumerateArray())
                {
                    if (mode.ValueKind == JsonValueKind.Object)
                    {
                        var hasSolution = mode.TryGetProperty("solution_hint", out var solutionHint);
                        if (!hasSolution || string.IsNullOrWhiteSpace(solutionHint.GetString()))
                        {
                            score -= 3;
                            issues.Add(new QualityIssue
                            {
                                Type = QualityIssueType.MissingFailureModes,
                                Description = "失效模式缺少解决方案提示",
                                Severity = QualityIssueSeverity.Medium,
                                Suggestion = "为每个失效模式添加 solution_hint"
                            });
                            break; // 只记录一次
                        }
                    }
                }
            }
        }

        return Math.Max(0, score);
    }

    /// <summary>
    /// 可验证性检查（20分）
    /// </summary>
    private int CheckVerifiability(JudgementCard card, List<QualityIssue> issues)
    {
        int score = 20;

        // 检查下一步动作是否可验证
        if (string.IsNullOrWhiteSpace(card.NextActionTemplate))
        {
            score -= 10;
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.UnverifiableAction,
                Description = "缺少下一步动作，无法验证",
                Severity = QualityIssueSeverity.Critical,
                Suggestion = "添加明确的下一步动作模板"
            });
        }
        else
        {
            // 检查动作模板是否包含可验证的内容
            var actionLower = card.NextActionTemplate.ToLower();
            var hasVerifiableKeywords = actionLower.Contains("检查") || 
                                       actionLower.Contains("验证") || 
                                       actionLower.Contains("测试") ||
                                       actionLower.Contains("确认");
            
            if (!hasVerifiableKeywords)
            {
                score -= 5;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.UnverifiableAction,
                    Description = "动作模板缺少可验证的关键词",
                    Severity = QualityIssueSeverity.Medium,
                    Suggestion = "在动作模板中添加明确的检查、验证或测试步骤"
                });
            }
        }

        // 检查验证清单是否完整
        var troubleshootingPath = card.TroubleshootingPath.RootElement;
        if (troubleshootingPath.ValueKind == JsonValueKind.Object)
        {
            var hasChecklist = troubleshootingPath.TryGetProperty("verification_checklist", out var checklist);
            if (!hasChecklist || checklist.ValueKind == JsonValueKind.Null)
            {
                score -= 5;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.IncompleteChecklist,
                    Description = "缺少验证清单",
                    Severity = QualityIssueSeverity.Medium,
                    Suggestion = "在排查路径中添加 verification_checklist，列出验证步骤"
                });
            }
            else if (checklist.ValueKind == JsonValueKind.Array && checklist.GetArrayLength() == 0)
            {
                score -= 3;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.IncompleteChecklist,
                    Description = "验证清单为空",
                    Severity = QualityIssueSeverity.Low,
                    Suggestion = "补充验证清单内容"
                });
            }
        }

        // 检查验收标准是否明确
        if (troubleshootingPath.ValueKind == JsonValueKind.Object)
        {
            var hasAcceptanceCriteria = troubleshootingPath.TryGetProperty("acceptance_criteria", out var criteria);
            if (!hasAcceptanceCriteria || criteria.ValueKind == JsonValueKind.Null)
            {
                score -= 2;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.UnclearAcceptanceCriteria,
                    Description = "缺少验收标准",
                    Severity = QualityIssueSeverity.Low,
                    Suggestion = "添加明确的验收标准，定义如何判断问题已解决"
                });
            }
        }

        return Math.Max(0, score);
    }

    /// <summary>
    /// 证据支撑检查（20分）
    /// </summary>
    private int CheckEvidence(JudgementCard card, List<QualityIssue> issues)
    {
        int score = 20;

        // 检查使用统计
        if (card.UsageCount == 0)
        {
            score -= 5;
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.NoHistoricalData,
                Description = "无历史使用数据",
                Severity = QualityIssueSeverity.Low,
                Suggestion = "判断卡尚未被使用，建议先在小范围试用"
            });
        }
        else
        {
            // 如果有使用记录，检查是否有成功案例
            // 这里简化处理，实际可以从 TriageNote 或 Solution 中查询
            // 暂时只检查使用次数
            if (card.UsageCount < 3)
            {
                score -= 2;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.NoSuccessCases,
                    Description = "使用次数较少，证据不足",
                    Severity = QualityIssueSeverity.Low,
                    Suggestion = "建议增加使用次数以积累更多证据"
                });
            }
        }

        // 检查最后使用时间
        if (card.LastUsedAt == null)
        {
            score -= 3;
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.NoHistoricalData,
                Description = "从未被使用",
                Severity = QualityIssueSeverity.Low,
                Suggestion = "判断卡需要实际使用来验证有效性"
            });
        }
        else
        {
            // 如果超过6个月未使用，可能已过时
            var monthsSinceLastUse = (DateTime.UtcNow - card.LastUsedAt.Value).TotalDays / 30;
            if (monthsSinceLastUse > 6)
            {
                score -= 2;
                issues.Add(new QualityIssue
                {
                    Type = QualityIssueType.NoHistoricalData,
                    Description = $"超过 {Math.Round(monthsSinceLastUse)} 个月未使用，可能已过时",
                    Severity = QualityIssueSeverity.Low,
                    Suggestion = "建议review判断卡是否仍然适用"
                });
            }
        }

        // 检查版本信息
        if (card.Version == 1 && card.UsageCount > 0)
        {
            // 如果使用过但从未更新，可能存在问题
            score -= 2;
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.NoFailureCases,
                Description = "使用过但从未更新版本，可能缺少改进",
                Severity = QualityIssueSeverity.Low,
                Suggestion = "根据使用反馈考虑更新判断卡"
            });
        }

        return Math.Max(0, score);
    }
}

