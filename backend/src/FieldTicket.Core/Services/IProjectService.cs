using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 项目服务接口
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// 获取项目列表
    /// </summary>
    Task<(List<ProjectDto> Items, int Total)> GetProjectsAsync(
        ProjectQueryFilter filter,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// 获取项目详情
    /// </summary>
    Task<ProjectDetailDto?> GetProjectAsync(Guid projectId);

    /// <summary>
    /// 获取项目的问题列表
    /// </summary>
    Task<List<FieldProblemDto>> GetProjectProblemsAsync(Guid projectId);

    /// <summary>
    /// 获取问题详情
    /// </summary>
    Task<FieldProblemDetailDto?> GetProblemAsync(Guid problemId);

    /// <summary>
    /// 获取问题统计
    /// </summary>
    Task<ProblemStatisticsDto> GetProblemStatisticsAsync(ProblemStatisticsFilter filter);
}



