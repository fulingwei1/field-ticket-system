using System.Text.Json;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 判断卡关系服务实现
/// </summary>
public class JudgementCardRelationService : IJudgementCardRelationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<JudgementCardRelationService> _logger;

    private static readonly Dictionary<string, string> RelationTypeNames = new()
    {
        { "similar", "相似" },
        { "depends_on", "依赖" },
        { "evolves_from", "演进" },
        { "related_to", "相关" }
    };

    public JudgementCardRelationService(
        ApplicationDbContext dbContext,
        ILogger<JudgementCardRelationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<JudgementCardRelationDto>> MineRelationsAsync(string jcCode)
    {
        _logger.LogInformation("Mining relations for judgement card {JcCode}", jcCode);

        var sourceJc = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode);

        if (sourceJc == null)
        {
            throw new KeyNotFoundException($"判断卡 {jcCode} 不存在");
        }

        var relations = new List<JudgementCardRelationDto>();

        // 获取所有其他判断卡
        var allJcs = await _dbContext.JudgementCards
            .Where(jc => jc.JcCode != jcCode && jc.Status == "Active")
            .ToListAsync();

        foreach (var targetJc in allJcs)
        {
            // 计算相似度
            var similarity = CalculateSimilarity(sourceJc, targetJc);
            if (similarity >= 0.6m)
            {
                var relation = new JudgementCardRelation
                {
                    RelationId = Guid.NewGuid(),
                    SourceJcCode = jcCode,
                    TargetJcCode = targetJc.JcCode,
                    RelationType = "similar",
                    RelationStrength = similarity,
                    Metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        similarity_score = similarity,
                        common_domains = sourceJc.Domain == targetJc.Domain ? new[] { sourceJc.Domain.ToString() } : Array.Empty<string>()
                    })),
                    CreatedAt = DateTime.UtcNow
                };

                // 检查是否已存在
                var existing = await _dbContext.JudgementCardRelations
                    .FirstOrDefaultAsync(r => r.SourceJcCode == jcCode && r.TargetJcCode == targetJc.JcCode && r.RelationType == "similar");

                if (existing == null)
                {
                    _dbContext.JudgementCardRelations.Add(relation);
                }
                else
                {
                    existing.RelationStrength = similarity;
                    existing.Metadata = relation.Metadata;
                }

                relations.Add(await MapToDtoAsync(relation));
            }

            // 检查依赖关系
            if (HasDependency(sourceJc, targetJc))
            {
                var relation = new JudgementCardRelation
                {
                    RelationId = Guid.NewGuid(),
                    SourceJcCode = jcCode,
                    TargetJcCode = targetJc.JcCode,
                    RelationType = "depends_on",
                    RelationStrength = 0.8m,
                    CreatedAt = DateTime.UtcNow
                };

                var existing = await _dbContext.JudgementCardRelations
                    .FirstOrDefaultAsync(r => r.SourceJcCode == jcCode && r.TargetJcCode == targetJc.JcCode && r.RelationType == "depends_on");

                if (existing == null)
                {
                    _dbContext.JudgementCardRelations.Add(relation);
                    relations.Add(await MapToDtoAsync(relation));
                }
            }

            // 检查演进关系
            if (HasEvolution(sourceJc, targetJc))
            {
                var relation = new JudgementCardRelation
                {
                    RelationId = Guid.NewGuid(),
                    SourceJcCode = jcCode,
                    TargetJcCode = targetJc.JcCode,
                    RelationType = "evolves_from",
                    RelationStrength = 0.9m,
                    CreatedAt = DateTime.UtcNow
                };

                var existing = await _dbContext.JudgementCardRelations
                    .FirstOrDefaultAsync(r => r.SourceJcCode == jcCode && r.TargetJcCode == targetJc.JcCode && r.RelationType == "evolves_from");

                if (existing == null)
                {
                    _dbContext.JudgementCardRelations.Add(relation);
                    relations.Add(await MapToDtoAsync(relation));
                }
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Mined {Count} relations for judgement card {JcCode}", relations.Count, jcCode);

        return relations;
    }

    public async Task<decimal> CalculateSimilarityAsync(string jcCode1, string jcCode2)
    {
        var jc1 = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode1);

        var jc2 = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode2);

        if (jc1 == null || jc2 == null)
        {
            return 0m;
        }

        return CalculateSimilarity(jc1, jc2);
    }

    private decimal CalculateSimilarity(JudgementCard jc1, JudgementCard jc2)
    {
        var score = 0.0m;

        // 1. 问题域匹配（权重30%）
        if (jc1.Domain == jc2.Domain)
        {
            score += 0.3m;
        }

        // 2. 标题相似度（权重20%）
        var titleSimilarity = CalculateStringSimilarity(jc1.Title, jc2.Title);
        score += titleSimilarity * 0.2m;

        // 3. 症状相似度（权重40%）
        // 简化实现：比较 SymptomStructure JSON
        var symptomSimilarity = CalculateJsonSimilarity(jc1.SymptomStructure, jc2.SymptomStructure);
        score += symptomSimilarity * 0.4m;

        // 4. 排查路径相似度（权重10%）
        var pathSimilarity = CalculateJsonSimilarity(jc1.TroubleshootingPath, jc2.TroubleshootingPath);
        score += pathSimilarity * 0.1m;

        return Math.Min(score, 1.0m);
    }

    private decimal CalculateStringSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
        {
            return 0m;
        }

        // 简单的字符串相似度计算（基于共同字符）
        var commonChars = str1.Intersect(str2).Count();
        var totalChars = Math.Max(str1.Length, str2.Length);
        return totalChars > 0 ? (decimal)commonChars / totalChars : 0m;
    }

    private decimal CalculateJsonSimilarity(JsonDocument doc1, JsonDocument doc2)
    {
        // 简化实现：比较 JSON 字符串
        var json1 = doc1.RootElement.GetRawText();
        var json2 = doc2.RootElement.GetRawText();
        return CalculateStringSimilarity(json1, json2);
    }

    private bool HasDependency(JudgementCard source, JudgementCard target)
    {
        // 简化实现：如果源判断卡的排查路径中包含目标判断卡的引用
        // 这里可以根据实际业务逻辑实现
        return false;
    }

    private bool HasEvolution(JudgementCard source, JudgementCard target)
    {
        // 如果目标判断卡是源判断卡的改进版本
        if (target.ParentJcId == source.JudgementCardId)
        {
            return true;
        }

        // 如果目标判断卡的版本号更高且创建时间更晚
        if (target.Version > source.Version && target.CreatedAt > source.CreatedAt)
        {
            return true;
        }

        return false;
    }

    public async Task<int> MineAllRelationsAsync()
    {
        _logger.LogInformation("Mining all judgement card relations");

        var allJcs = await _dbContext.JudgementCards
            .Where(jc => jc.Status == "Active")
            .Select(jc => jc.JcCode)
            .ToListAsync();

        var totalRelations = 0;

        foreach (var jcCode in allJcs)
        {
            try
            {
                var relations = await MineRelationsAsync(jcCode);
                totalRelations += relations.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mining relations for {JcCode}", jcCode);
            }
        }

        _logger.LogInformation("Mined {Count} relations for {JcCount} judgement cards", totalRelations, allJcs.Count);

        return totalRelations;
    }

    public async Task<List<JudgementCardRelationDto>> GetRelationsAsync(
        string jcCode,
        string? relationType = null)
    {
        var query = _dbContext.JudgementCardRelations
            .Where(r => r.SourceJcCode == jcCode || r.TargetJcCode == jcCode);

        if (!string.IsNullOrEmpty(relationType))
        {
            query = query.Where(r => r.RelationType == relationType);
        }

        var relations = await query.ToListAsync();

        var result = new List<JudgementCardRelationDto>();
        foreach (var relation in relations)
        {
            result.Add(await MapToDtoAsync(relation));
        }

        return result;
    }

    public async Task<List<LearningPathDto>> RecommendLearningPathsAsync(Guid userId)
    {
        _logger.LogInformation("Recommending learning paths for user {UserId}", userId);

        // 简化实现：基于依赖关系推荐学习路径
        var allJcs = await _dbContext.JudgementCards
            .Where(jc => jc.Status == "Active")
            .OrderBy(jc => jc.Domain)
            .ThenBy(jc => jc.UsageCount)
            .Take(10)
            .ToListAsync();

        var paths = new List<LearningPathDto>();

        // 按问题域分组
        var groupedByDomain = allJcs.GroupBy(jc => jc.Domain);

        foreach (var group in groupedByDomain)
        {
            var steps = group.Select((jc, index) => new LearningStepDto
            {
                StepOrder = index + 1,
                JcCode = jc.JcCode,
                JcTitle = jc.Title,
                Difficulty = 0.5m, // 简化实现
                Prerequisites = new List<string>()
            }).ToList();

            paths.Add(new LearningPathDto
            {
                PathId = $"path-{group.Key}",
                PathName = $"问题域 {group.Key} 学习路径",
                Description = $"学习问题域 {group.Key} 相关的判断卡",
                Steps = steps,
                EstimatedDays = steps.Count * 2,
                Difficulty = 0.5m
            });
        }

        return paths;
    }

    private async Task<JudgementCardRelationDto> MapToDtoAsync(JudgementCardRelation relation)
    {
        var dto = new JudgementCardRelationDto
        {
            RelationId = relation.RelationId,
            SourceJcCode = relation.SourceJcCode,
            TargetJcCode = relation.TargetJcCode,
            RelationType = relation.RelationType,
            RelationTypeName = RelationTypeNames.GetValueOrDefault(relation.RelationType, relation.RelationType),
            RelationStrength = relation.RelationStrength,
            CreatedAt = relation.CreatedAt
        };

        // 获取判断卡标题
        var sourceJc = await _dbContext.JudgementCards
            .Where(jc => jc.JcCode == relation.SourceJcCode)
            .Select(jc => jc.Title)
            .FirstOrDefaultAsync();
        dto.SourceJcTitle = sourceJc;

        var targetJc = await _dbContext.JudgementCards
            .Where(jc => jc.JcCode == relation.TargetJcCode)
            .Select(jc => jc.Title)
            .FirstOrDefaultAsync();
        dto.TargetJcTitle = targetJc;

        // 解析元数据
        if (relation.Metadata != null)
        {
            dto.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(
                relation.Metadata.RootElement.GetRawText());
        }

        return dto;
    }
}






