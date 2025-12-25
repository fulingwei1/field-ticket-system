namespace FieldTicket.Shared.Models;

/// <summary>
/// 缺失信息项
/// </summary>
public class MissingInfoItem
{
    /// <summary>
    /// 字段名
    /// </summary>
    public string Field { get; set; } = string.Empty;
    
    /// <summary>
    /// 问题描述
    /// </summary>
    public string Question { get; set; } = string.Empty;
    
    /// <summary>
    /// 问题类型（yes_no/number/text/file/select）
    /// </summary>
    public string Type { get; set; } = "yes_no";
    
    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; }
    
    /// <summary>
    /// 问题域
    /// </summary>
    public string? Domain { get; set; }
    
    /// <summary>
    /// 选项列表（用于select类型）
    /// </summary>
    public List<string>? Options { get; set; }
    
    /// <summary>
    /// 提示信息
    /// </summary>
    public string? Hint { get; set; }
}

/// <summary>
/// 问诊式问题项
/// </summary>
public class QuestionItem
{
    /// <summary>
    /// 问题ID
    /// </summary>
    public string QuestionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 问题文本
    /// </summary>
    public string Question { get; set; } = string.Empty;
    
    /// <summary>
    /// 问题类型
    /// </summary>
    public string Type { get; set; } = "yes_no";
    
    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; }
    
    /// <summary>
    /// 选项列表（用于select类型）
    /// </summary>
    public List<string>? Options { get; set; }
    
    /// <summary>
    /// 提示信息
    /// </summary>
    public string? Hint { get; set; }
    
    /// <summary>
    /// 关联的字段名
    /// </summary>
    public string? Field { get; set; }
}

/// <summary>
/// 补全缺失信息请求
/// </summary>
public class CompleteMissingInfoRequest
{
    /// <summary>
    /// 问题答案（QuestionId -> Answer）
    /// </summary>
    public Dictionary<string, object> Answers { get; set; } = new();
    
    /// <summary>
    /// 附件ID列表（用于文件类型问题）
    /// </summary>
    public Dictionary<string, List<Guid>> Attachments { get; set; } = new();
}

/// <summary>
/// 缺失信息分析结果
/// </summary>
public class MissingInfoAnalysisResult
{
    /// <summary>
    /// 缺失信息项列表
    /// </summary>
    public List<MissingInfoItem> MissingInfo { get; set; } = new();
    
    /// <summary>
    /// 问诊式问题清单
    /// </summary>
    public List<QuestionItem> Questions { get; set; } = new();
    
    /// <summary>
    /// 是否有关键信息缺失
    /// </summary>
    public bool HasCriticalMissing { get; set; }
}


