using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 根本原因分析服务实现
/// </summary>
public class RootCauseAnalysisService : IRootCauseAnalysisService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RootCauseAnalysisService> _logger;

    public RootCauseAnalysisService(
        ApplicationDbContext dbContext,
        ILogger<RootCauseAnalysisService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RootCauseAnalysisDto> CreateOrUpdateAnalysisAsync(
        Guid problemId,
        CreateRootCauseAnalysisRequest request)
    {
        // 检查问题是否存在
        var problem = await _dbContext.FieldProblems.FindAsync(problemId);
        if (problem == null)
        {
            throw new ArgumentException($"Problem {problemId} not found");
        }

        // 查找是否已存在分析
        var existingAnalysis = await _dbContext.RootCauseAnalyses
            .FirstOrDefaultAsync(a => a.ProblemId == problemId);

        RootCauseAnalysis analysis;
        if (existingAnalysis != null)
        {
            // 更新现有分析
            analysis = existingAnalysis;
            analysis.Why1 = request.Why1;
            analysis.Why2 = request.Why2;
            analysis.Why3 = request.Why3;
            analysis.Why4 = request.Why4;
            analysis.Why5 = request.Why5;
            analysis.RootCause = request.RootCause;
            analysis.RootCauseCategory = request.RootCauseCategory;
            analysis.PreventiveMeasures = request.PreventiveMeasures;
            analysis.VerificationMethod = request.VerificationMethod;
            analysis.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // 创建新分析
            analysis = new RootCauseAnalysis
            {
                AnalysisId = Guid.NewGuid(),
                ProblemId = problemId,
                Why1 = request.Why1,
                Why2 = request.Why2,
                Why3 = request.Why3,
                Why4 = request.Why4,
                Why5 = request.Why5,
                RootCause = request.RootCause,
                RootCauseCategory = request.RootCauseCategory,
                PreventiveMeasures = request.PreventiveMeasures,
                VerificationMethod = request.VerificationMethod,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.RootCauseAnalyses.Add(analysis);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Root cause analysis saved for problem {ProblemId}", problemId);

        return await MapToDtoAsync(analysis);
    }

    public async Task<RootCauseAnalysisDto?> GetAnalysisByProblemIdAsync(Guid problemId)
    {
        var analysis = await _dbContext.RootCauseAnalyses
            .FirstOrDefaultAsync(a => a.ProblemId == problemId);

        return analysis != null ? await MapToDtoAsync(analysis) : null;
    }

    public async Task<RootCauseAnalysisDto?> GetAnalysisAsync(Guid analysisId)
    {
        var analysis = await _dbContext.RootCauseAnalyses.FindAsync(analysisId);
        return analysis != null ? await MapToDtoAsync(analysis) : null;
    }

    public async Task DeleteAnalysisAsync(Guid analysisId)
    {
        var analysis = await _dbContext.RootCauseAnalyses.FindAsync(analysisId);
        if (analysis == null)
        {
            throw new ArgumentException($"Analysis {analysisId} not found");
        }

        _dbContext.RootCauseAnalyses.Remove(analysis);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Root cause analysis {AnalysisId} deleted", analysisId);
    }

    public async Task<FiveWhyTemplate> GetFiveWhyTemplateAsync(string problemCategory)
    {
        // 根据问题分类返回不同的模板
        var template = new FiveWhyTemplate
        {
            ProblemCategory = problemCategory,
            WhyQuestions = new List<string>
            {
                "为什么会出现这个问题？",
                "为什么会有这个原因？",
                "为什么？",
                "为什么？",
                "为什么？"
            }
        };

        // 根据问题分类提供常见根本原因和建议
        switch (problemCategory.ToLower())
        {
            case "设计":
            case "design":
                template.CommonRootCauses = new List<string>
                {
                    "设计规范不完善",
                    "设计评审不充分",
                    "设计变更未及时更新文档"
                };
                template.SuggestedPreventiveMeasures = new List<string>
                {
                    "完善设计规范和检查清单",
                    "加强设计评审流程",
                    "建立设计变更管理机制"
                };
                break;
            case "工艺":
            case "process":
                template.CommonRootCauses = new List<string>
                {
                    "工艺参数设置不当",
                    "工艺文件不清晰",
                    "操作人员培训不足"
                };
                template.SuggestedPreventiveMeasures = new List<string>
                {
                    "优化工艺参数设置",
                    "完善工艺文件",
                    "加强操作人员培训"
                };
                break;
            case "管理":
            case "management":
                template.CommonRootCauses = new List<string>
                {
                    "流程管理不规范",
                    "责任划分不明确",
                    "沟通协调不足"
                };
                template.SuggestedPreventiveMeasures = new List<string>
                {
                    "规范流程管理",
                    "明确责任划分",
                    "加强沟通协调"
                };
                break;
            default:
                template.CommonRootCauses = new List<string>
                {
                    "原因待分析"
                };
                template.SuggestedPreventiveMeasures = new List<string>
                {
                    "根据具体情况制定预防措施"
                };
                break;
        }

        return await Task.FromResult(template);
    }

    public async Task<List<PreventiveMeasureSuggestion>> GetPreventiveMeasureSuggestionsAsync(
        string rootCauseCategory)
    {
        var suggestions = new List<PreventiveMeasureSuggestion>();

        switch (rootCauseCategory?.ToLower())
        {
            case "设计":
            case "design":
                suggestions.AddRange(new[]
                {
                    new PreventiveMeasureSuggestion
                    {
                        Category = "设计",
                        Measure = "完善设计规范",
                        Description = "建立并完善设计规范和检查清单，确保设计质量",
                        VerificationMethod = "设计评审检查",
                        Priority = 1
                    },
                    new PreventiveMeasureSuggestion
                    {
                        Category = "设计",
                        Measure = "加强设计评审",
                        Description = "建立多级设计评审机制，确保设计问题及时发现",
                        VerificationMethod = "评审记录检查",
                        Priority = 2
                    }
                });
                break;
            case "工艺":
            case "process":
                suggestions.AddRange(new[]
                {
                    new PreventiveMeasureSuggestion
                    {
                        Category = "工艺",
                        Measure = "优化工艺参数",
                        Description = "根据历史问题优化工艺参数设置",
                        VerificationMethod = "参数验证测试",
                        Priority = 1
                    },
                    new PreventiveMeasureSuggestion
                    {
                        Category = "工艺",
                        Measure = "完善工艺文件",
                        Description = "更新和完善工艺文件，确保操作指导清晰",
                        VerificationMethod = "文件审核",
                        Priority = 2
                    }
                });
                break;
            case "管理":
            case "management":
                suggestions.AddRange(new[]
                {
                    new PreventiveMeasureSuggestion
                    {
                        Category = "管理",
                        Measure = "规范流程管理",
                        Description = "建立规范的流程管理制度",
                        VerificationMethod = "流程审计",
                        Priority = 1
                    },
                    new PreventiveMeasureSuggestion
                    {
                        Category = "管理",
                        Measure = "明确责任划分",
                        Description = "明确各部门和人员的责任范围",
                        VerificationMethod = "责任矩阵检查",
                        Priority = 2
                    }
                });
                break;
        }

        return await Task.FromResult(suggestions);
    }

    private async Task<RootCauseAnalysisDto> MapToDtoAsync(RootCauseAnalysis analysis)
    {
        var dto = new RootCauseAnalysisDto
        {
            AnalysisId = analysis.AnalysisId,
            ProblemId = analysis.ProblemId,
            Why1 = analysis.Why1,
            Why2 = analysis.Why2,
            Why3 = analysis.Why3,
            Why4 = analysis.Why4,
            Why5 = analysis.Why5,
            RootCause = analysis.RootCause,
            RootCauseCategory = analysis.RootCauseCategory,
            PreventiveMeasures = analysis.PreventiveMeasures,
            VerificationMethod = analysis.VerificationMethod,
            AnalyzedBy = analysis.AnalyzedBy,
            AnalyzedAt = analysis.AnalyzedAt,
            CreatedAt = analysis.CreatedAt,
            UpdatedAt = analysis.UpdatedAt
        };

        // 获取分析人姓名
        if (analysis.AnalyzedBy.HasValue)
        {
            var user = await _dbContext.Users.FindAsync(analysis.AnalyzedBy.Value);
            dto.AnalyzedByName = user?.Name;
        }

        return dto;
    }
}













