using System.Text.Json;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 知识版本服务实现
/// </summary>
public class KnowledgeVersionService : IKnowledgeVersionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<KnowledgeVersionService> _logger;

    public KnowledgeVersionService(
        ApplicationDbContext dbContext,
        ILogger<KnowledgeVersionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<KnowledgeVersionDto> CreateVersionAsync(CreateVersionRequest request, Guid userId)
    {
        _logger.LogInformation("Creating version for knowledge {KnowledgeId}, type: {KnowledgeType}",
            request.KnowledgeId, request.KnowledgeType);

        // 获取当前版本
        var currentVersion = await _dbContext.KnowledgeVersions
            .FirstOrDefaultAsync(v => v.KnowledgeId == request.KnowledgeId &&
                                     v.KnowledgeType == request.KnowledgeType &&
                                     v.IsCurrent);

        // 计算新版本号
        var newVersionNumber = CalculateNextVersionNumber(currentVersion);

        // 将之前的版本设为非当前
        if (currentVersion != null)
        {
            currentVersion.IsCurrent = false;
        }

        // 创建新版本
        var newVersion = new KnowledgeVersion
        {
            VersionId = Guid.NewGuid(),
            KnowledgeId = request.KnowledgeId,
            KnowledgeType = request.KnowledgeType,
            VersionNumber = newVersionNumber,
            VersionDescription = request.VersionDescription,
            ContentJson = request.Content,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            IsCurrent = true,
            ChangeType = currentVersion == null ? "created" : "updated",
            ChangeReason = request.ChangeReason
        };

        _dbContext.KnowledgeVersions.Add(newVersion);

        // 创建版本关联
        if (currentVersion != null)
        {
            var relation = new KnowledgeVersionRelation
            {
                RelationId = Guid.NewGuid(),
                SourceVersionId = newVersion.VersionId,
                TargetVersionId = currentVersion.VersionId,
                RelationType = "evolves_from",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.KnowledgeVersionRelations.Add(relation);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created version {VersionNumber} for knowledge {KnowledgeId}",
            newVersionNumber, request.KnowledgeId);

        return await MapToDtoAsync(newVersion);
    }

    public async Task<List<KnowledgeVersionDto>> GetVersionHistoryAsync(Guid knowledgeId, string knowledgeType)
    {
        _logger.LogInformation("Getting version history for knowledge {KnowledgeId}, type: {KnowledgeType}",
            knowledgeId, knowledgeType);

        var versions = await _dbContext.KnowledgeVersions
            .Where(v => v.KnowledgeId == knowledgeId && v.KnowledgeType == knowledgeType)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        var result = new List<KnowledgeVersionDto>();
        foreach (var version in versions)
        {
            result.Add(await MapToDtoAsync(version));
        }

        return result;
    }

    public async Task<FieldTicket.Shared.Models.VersionComparisonDto> CompareVersionsAsync(Guid versionId1, Guid versionId2)
    {
        _logger.LogInformation("Comparing versions {VersionId1} and {VersionId2}", versionId1, versionId2);

        var version1 = await _dbContext.KnowledgeVersions
            .FirstOrDefaultAsync(v => v.VersionId == versionId1);

        var version2 = await _dbContext.KnowledgeVersions
            .FirstOrDefaultAsync(v => v.VersionId == versionId2);

        if (version1 == null || version2 == null)
        {
            throw new KeyNotFoundException("版本不存在");
        }

        var differences = CompareContent(version1.ContentJson, version2.ContentJson);

        return new FieldTicket.Shared.Models.VersionComparisonDto
        {
            Version1 = await MapToDtoAsync(version1),
            Version2 = await MapToDtoAsync(version2),
            Differences = differences
        };
    }

    public async Task<KnowledgeVersionDto> RollbackVersionAsync(
        Guid knowledgeId,
        string knowledgeType,
        RollbackVersionRequest request,
        Guid userId)
    {
        _logger.LogInformation("Rolling back to version {VersionId} for knowledge {KnowledgeId}",
            request.TargetVersionId, knowledgeId);

        // 获取目标版本
        var targetVersion = await _dbContext.KnowledgeVersions
            .FirstOrDefaultAsync(v => v.VersionId == request.TargetVersionId &&
                                     v.KnowledgeId == knowledgeId &&
                                     v.KnowledgeType == knowledgeType);

        if (targetVersion == null)
        {
            throw new KeyNotFoundException("目标版本不存在");
        }

        // 将当前版本设为非当前
        var currentVersion = await _dbContext.KnowledgeVersions
            .FirstOrDefaultAsync(v => v.KnowledgeId == knowledgeId &&
                                     v.KnowledgeType == knowledgeType &&
                                     v.IsCurrent);

        if (currentVersion != null)
        {
            currentVersion.IsCurrent = false;
        }

        // 创建回滚版本（基于目标版本）
        var rollbackVersion = new KnowledgeVersion
        {
            VersionId = Guid.NewGuid(),
            KnowledgeId = knowledgeId,
            KnowledgeType = knowledgeType,
            VersionNumber = CalculateNextVersionNumber(currentVersion),
            VersionDescription = $"回滚到 {targetVersion.VersionNumber}",
            ContentJson = targetVersion.ContentJson,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            IsCurrent = true,
            ChangeType = "updated",
            ChangeReason = request.ChangeReason,
            ChangeSummary = $"回滚到版本 {targetVersion.VersionNumber}"
        };

        _dbContext.KnowledgeVersions.Add(rollbackVersion);

        // 创建版本关联
        if (currentVersion != null)
        {
            var relation = new KnowledgeVersionRelation
            {
                RelationId = Guid.NewGuid(),
                SourceVersionId = rollbackVersion.VersionId,
                TargetVersionId = targetVersion.VersionId,
                RelationType = "replaces",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.KnowledgeVersionRelations.Add(relation);
        }

        await _dbContext.SaveChangesAsync();

        return await MapToDtoAsync(rollbackVersion);
    }

    public async Task<List<ExpiredKnowledgeDto>> CheckExpiredKnowledgeAsync()
    {
        _logger.LogInformation("Checking expired knowledge");

        // 简化实现：检查知识版本（这里假设知识有过期日期字段在properties中）
        var currentVersions = await _dbContext.KnowledgeVersions
            .Where(v => v.IsCurrent)
            .ToListAsync();

        var expiredKnowledge = new List<ExpiredKnowledgeDto>();

        foreach (var version in currentVersions)
        {
            // 检查properties中是否有expiry_date
            if (version.ContentJson.RootElement.TryGetProperty("expiry_date", out var expiryElement))
            {
                if (expiryElement.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(expiryElement.GetString(), out var expiryDate))
                {
                    if (expiryDate < DateTime.UtcNow)
                    {
                        var daysSinceExpiry = (DateTime.UtcNow - expiryDate).Days;

                        expiredKnowledge.Add(new ExpiredKnowledgeDto
                        {
                            KnowledgeId = version.KnowledgeId,
                            KnowledgeType = version.KnowledgeType,
                            KnowledgeName = version.ContentJson.RootElement.TryGetProperty("title", out var title)
                                ? title.GetString() ?? string.Empty
                                : string.Empty,
                            CurrentVersion = version.VersionNumber,
                            ExpiryDate = expiryDate,
                            DaysSinceExpiry = daysSinceExpiry
                        });
                    }
                }
            }
        }

        return expiredKnowledge;
    }

    // 私有辅助方法

    private string CalculateNextVersionNumber(KnowledgeVersion? currentVersion)
    {
        if (currentVersion == null)
        {
            return "v1.0";
        }

        var versionParts = currentVersion.VersionNumber.TrimStart('v').Split('.');
        if (versionParts.Length != 2)
        {
            return "v1.0";
        }

        if (int.TryParse(versionParts[0], out var major) && int.TryParse(versionParts[1], out var minor))
        {
            // 如果是重大变更，增加主版本号；否则增加次版本号
            // 这里简化处理，总是增加次版本号
            return $"v{major}.{minor + 1}";
        }

        return "v1.0";
    }

    private List<VersionDifference> CompareContent(JsonDocument content1, JsonDocument content2)
    {
        var differences = new List<VersionDifference>();

        var dict1 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            content1.RootElement.GetRawText()) ?? new Dictionary<string, JsonElement>();

        var dict2 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            content2.RootElement.GetRawText()) ?? new Dictionary<string, JsonElement>();

        var allKeys = dict1.Keys.Union(dict2.Keys).Distinct();

        foreach (var key in allKeys)
        {
            var hasKey1 = dict1.ContainsKey(key);
            var hasKey2 = dict2.ContainsKey(key);

            if (!hasKey1)
            {
                differences.Add(new VersionDifference
                {
                    Field = key,
                    OldValue = null,
                    NewValue = dict2[key].ToString(),
                    ChangeType = "added"
                });
            }
            else if (!hasKey2)
            {
                differences.Add(new VersionDifference
                {
                    Field = key,
                    OldValue = dict1[key].ToString(),
                    NewValue = null,
                    ChangeType = "deleted"
                });
            }
            else if (dict1[key].ToString() != dict2[key].ToString())
            {
                differences.Add(new VersionDifference
                {
                    Field = key,
                    OldValue = dict1[key].ToString(),
                    NewValue = dict2[key].ToString(),
                    ChangeType = "modified"
                });
            }
        }

        return differences;
    }

    private async Task<KnowledgeVersionDto> MapToDtoAsync(KnowledgeVersion version)
    {
        var dto = new KnowledgeVersionDto
        {
            VersionId = version.VersionId,
            KnowledgeId = version.KnowledgeId,
            KnowledgeType = version.KnowledgeType,
            VersionNumber = version.VersionNumber,
            VersionDescription = version.VersionDescription,
            Content = version.ContentJson,
            CreatedBy = version.CreatedBy,
            CreatedAt = version.CreatedAt,
            IsCurrent = version.IsCurrent,
            ChangeType = version.ChangeType,
            ChangeReason = version.ChangeReason,
            ChangeSummary = version.ChangeSummary
        };

        if (version.CreatedBy.HasValue)
        {
            var creator = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == version.CreatedBy.Value);
            dto.CreatedByName = creator?.Name;
        }

        return dto;
    }
}

