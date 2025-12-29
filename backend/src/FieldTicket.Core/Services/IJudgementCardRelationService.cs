namespace FieldTicket.Core.Services;

/// <summary>
/// 判断卡关系服务接口
/// </summary>
public interface IJudgementCardRelationService
{
    /// <summary>
    /// 挖掘判断卡关系
    /// </summary>
    Task<List<JudgementCardRelationDto>> MineRelationsAsync(string jcCode);

    /// <summary>
    /// 计算两个判断卡的相似度
    /// </summary>
    Task<decimal> CalculateSimilarityAsync(string jcCode1, string jcCode2);

    /// <summary>
    /// 批量挖掘所有判断卡关系
    /// </summary>
    Task<int> MineAllRelationsAsync();

    /// <summary>
    /// 获取判断卡的关系列表
    /// </summary>
    Task<List<JudgementCardRelationDto>> GetRelationsAsync(
        string jcCode,
        string? relationType = null);

    /// <summary>
    /// 推荐学习路径
    /// </summary>
    Task<List<LearningPathDto>> RecommendLearningPathsAsync(Guid userId);
}

/// <summary>
/// 判断卡关系DTO
/// </summary>
public class JudgementCardRelationDto
{
    public Guid RelationId { get; set; }
    public string SourceJcCode { get; set; } = string.Empty;
    public string? SourceJcTitle { get; set; }
    public string TargetJcCode { get; set; } = string.Empty;
    public string? TargetJcTitle { get; set; }
    public string RelationType { get; set; } = string.Empty;
    public string RelationTypeName { get; set; } = string.Empty;
    public decimal RelationStrength { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 学习路径DTO
/// </summary>
public class LearningPathDto
{
    public string PathId { get; set; } = string.Empty;
    public string PathName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<LearningStepDto> Steps { get; set; } = new();
    public int EstimatedDays { get; set; }
    public decimal Difficulty { get; set; }
}

/// <summary>
/// 学习步骤DTO
/// </summary>
public class LearningStepDto
{
    public int StepOrder { get; set; }
    public string JcCode { get; set; } = string.Empty;
    public string? JcTitle { get; set; }
    public string? Description { get; set; }
    public decimal Difficulty { get; set; }
    public List<string> Prerequisites { get; set; } = new();
}

















