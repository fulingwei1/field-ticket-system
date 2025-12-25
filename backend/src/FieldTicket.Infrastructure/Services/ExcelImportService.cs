using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Globalization;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// Excel导入服务实现
/// </summary>
public class ExcelImportService : IExcelImportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(
        ApplicationDbContext dbContext,
        ILogger<ExcelImportService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // EPPlus 7.0+ 需要设置许可证上下文
    }

    public async Task<ExcelImportResult> ParseExcelAsync(Stream excelStream, string fileName)
    {
        var result = new ExcelImportResult();
        var projectsDict = new Dictionary<string, ProjectImportData>();

        try
        {
            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets[0]; // 读取第一个Sheet

            if (worksheet == null || worksheet.Dimension == null)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = 0,
                    Field = "File",
                    ErrorType = "Format",
                    Message = "Excel文件为空或格式不正确"
                });
                return result;
            }

            // 读取表头
            var headers = ReadHeaders(worksheet);
            result.TotalRows = worksheet.Dimension.End.Row - 1; // 减去表头行

            // 读取数据行
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                try
                {
                    var projectData = ParseProjectRow(worksheet, row, headers);
                    if (projectData == null)
                    {
                        result.ErrorRows++;
                        continue;
                    }

                    // 按项目号分组
                    if (!projectsDict.ContainsKey(projectData.ProjectNo))
                    {
                        projectsDict[projectData.ProjectNo] = projectData;
                    }

                    // 解析问题数据（同一行可能包含问题信息）
                    var problemData = ParseProblemRow(worksheet, row, headers, projectData.ProjectNo);
                    if (problemData != null)
                    {
                        projectsDict[projectData.ProjectNo].Problems.Add(problemData);
                    }

                    result.SuccessRows++;
                }
                catch (Exception ex)
                {
                    result.ErrorRows++;
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = row,
                        Field = "Row",
                        ErrorType = "Format",
                        Message = $"解析第{row}行时出错: {ex.Message}"
                    });
                    _logger.LogError(ex, "解析Excel第{Row}行时出错", row);
                }
            }

            result.Projects = projectsDict.Values.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析Excel文件时出错");
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Field = "File",
                ErrorType = "Format",
                Message = $"解析Excel文件失败: {ex.Message}"
            });
        }

        return result;
    }

    public async Task<ValidationResult> ValidateImportDataAsync(ExcelImportResult importResult)
    {
        var validationResult = new ValidationResult { IsValid = true };

        foreach (var project in importResult.Projects)
        {
            // 验证项目必填字段
            if (string.IsNullOrWhiteSpace(project.ProjectNo))
            {
                validationResult.IsValid = false;
                validationResult.Errors.Add(new ValidationError
                {
                    Field = "ProjectNo",
                    Code = "Required",
                    Message = $"第{project.ExcelRowNumber}行：项目号不能为空"
                });
            }

            if (string.IsNullOrWhiteSpace(project.ProjectName))
            {
                validationResult.IsValid = false;
                validationResult.Errors.Add(new ValidationError
                {
                    Field = "ProjectName",
                    Code = "Required",
                    Message = $"第{project.ExcelRowNumber}行：项目名称不能为空"
                });
            }

            // 验证日期逻辑
            if (project.RequiredDeliveryDate.HasValue && project.OrderDate.HasValue)
            {
                if (project.RequiredDeliveryDate < project.OrderDate)
                {
                    validationResult.IsValid = false;
                    validationResult.Errors.Add(new ValidationError
                    {
                        Field = "RequiredDeliveryDate",
                        Code = "Business",
                        Message = $"第{project.ExcelRowNumber}行：要求交货日期不能早于下单日期"
                    });
                }
            }

            // 验证问题数据
            foreach (var problem in project.Problems)
            {
                if (string.IsNullOrWhiteSpace(problem.ProblemDescription))
                {
                    validationResult.IsValid = false;
                    validationResult.Errors.Add(new ValidationError
                    {
                        Field = "ProblemDescription",
                        Code = "Required",
                        Message = $"第{problem.ExcelRowNumber}行：问题描述不能为空"
                    });
                }

                if (problem.FoundDate == default)
                {
                    validationResult.IsValid = false;
                    validationResult.Errors.Add(new ValidationError
                    {
                        Field = "FoundDate",
                        Code = "Required",
                        Message = $"第{problem.ExcelRowNumber}行：发现日期不能为空"
                    });
                }

                // 验证满意度评分范围
                if (problem.SatisfactionScore.HasValue && (problem.SatisfactionScore < 1 || problem.SatisfactionScore > 5))
                {
                    validationResult.IsValid = false;
                    validationResult.Errors.Add(new ValidationError
                    {
                        Field = "SatisfactionScore",
                        Code = "Range",
                        Message = $"第{problem.ExcelRowNumber}行：满意度评分必须在1-5之间"
                    });
                }

                // 验证完成日期不能早于发现日期
                if (problem.CompletedDate.HasValue && problem.FoundDate != default)
                {
                    if (problem.CompletedDate < problem.FoundDate)
                    {
                        validationResult.IsValid = false;
                        validationResult.Errors.Add(new ValidationError
                        {
                            Field = "CompletedDate",
                            Code = "Business",
                            Message = $"第{problem.ExcelRowNumber}行：完成日期不能早于发现日期"
                        });
                    }
                }
            }
        }

        return validationResult;
    }

    public async Task<ImportExecutionResult> ExecuteImportAsync(ExcelImportResult importResult, Guid userId)
    {
        var executionResult = new ImportExecutionResult { Success = true };

        try
        {
            foreach (var projectData in importResult.Projects)
            {
                // 查找或创建客户
                var customerId = await FindOrCreateCustomerAsync(projectData.CustomerName, userId);

                // 查找或创建项目
                var project = await _dbContext.Projects
                    .FirstOrDefaultAsync(p => p.ProjectNo == projectData.ProjectNo);

                if (project == null)
                {
                    project = new Project
                    {
                        ProjectId = Guid.NewGuid(),
                        ProjectNo = projectData.ProjectNo,
                        ProjectName = projectData.ProjectName,
                        CustomerId = customerId,
                        CustomerName = projectData.CustomerName,
                        DeviceType = projectData.DeviceType ?? string.Empty,
                        IndustryType = projectData.IndustryType,
                        SalesAmount = projectData.SalesAmount,
                        Quantity = projectData.Quantity,
                        OrderDate = projectData.OrderDate,
                        RequiredDeliveryDate = projectData.RequiredDeliveryDate,
                        ActualDeliveryDate = projectData.ActualDeliveryDate,
                        DeliveryDelayDays = CalculateDeliveryDelayDays(projectData.RequiredDeliveryDate, projectData.ActualDeliveryDate),
                        ProjectStatus = projectData.ProjectStatus,
                        ProjectManagerName = projectData.ProjectManagerName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    _dbContext.Projects.Add(project);
                    executionResult.ProjectsCreated++;
                }
                else
                {
                    // 更新项目信息
                    project.ProjectName = projectData.ProjectName;
                    project.CustomerName = projectData.CustomerName;
                    project.DeviceType = projectData.DeviceType ?? project.DeviceType;
                    project.IndustryType = projectData.IndustryType ?? project.IndustryType;
                    project.SalesAmount = projectData.SalesAmount ?? project.SalesAmount;
                    project.Quantity = projectData.Quantity;
                    project.OrderDate = projectData.OrderDate ?? project.OrderDate;
                    project.RequiredDeliveryDate = projectData.RequiredDeliveryDate ?? project.RequiredDeliveryDate;
                    project.ActualDeliveryDate = projectData.ActualDeliveryDate ?? project.ActualDeliveryDate;
                    project.DeliveryDelayDays = CalculateDeliveryDelayDays(project.RequiredDeliveryDate, project.ActualDeliveryDate);
                    project.ProjectStatus = projectData.ProjectStatus;
                    project.UpdatedAt = DateTime.UtcNow;
                    project.UpdatedBy = userId;
                    executionResult.ProjectsUpdated++;
                }

                // 保存项目以获取ProjectId
                await _dbContext.SaveChangesAsync();

                // 导入问题
                foreach (var problemData in projectData.Problems)
                {
                    var existingProblem = await _dbContext.FieldProblems
                        .FirstOrDefaultAsync(p => p.ProjectId == project.ProjectId && p.ProblemSequence == problemData.ProblemSequence);

                    if (existingProblem == null)
                    {
                        var problem = new FieldProblem
                        {
                            ProblemId = Guid.NewGuid(),
                            ProjectId = project.ProjectId,
                            ProblemSequence = problemData.ProblemSequence,
                            ProblemCategory = problemData.ProblemCategory,
                            ProblemDescription = problemData.ProblemDescription,
                            Priority = problemData.Priority,
                            FoundDate = problemData.FoundDate,
                            CompletedDate = problemData.CompletedDate,
                            ProcessingDays = CalculateProcessingDays(problemData.FoundDate, problemData.CompletedDate),
                            PrimaryDepartment = problemData.PrimaryDepartment,
                            PrimaryResponsible = problemData.PrimaryResponsible,
                            CollaboratingDepartment = problemData.CollaboratingDepartment,
                            CollaboratingPerson = problemData.CollaboratingPerson,
                            Status = problemData.Status,
                            Solution = problemData.Solution,
                            SolutionDetails = problemData.SolutionDetails,
                            VerificationStatus = problemData.VerificationStatus,
                            CustomerFeedback = problemData.CustomerFeedback,
                            SatisfactionScore = problemData.SatisfactionScore,
                            VerifiedAt = problemData.VerifiedAt,
                            VerifiedBy = problemData.VerifiedBy,
                            RelatedTicketNo = problemData.RelatedTicketNo,
                            KnowledgeBaseId = problemData.KnowledgeBaseId,
                            IsRepeatProblem = problemData.IsRepeatProblem,
                            Notes = problemData.Notes,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = userId
                        };
                        _dbContext.FieldProblems.Add(problem);
                        executionResult.ProblemsCreated++;
                    }
                    else
                    {
                        // 更新问题
                        existingProblem.ProblemCategory = problemData.ProblemCategory;
                        existingProblem.ProblemDescription = problemData.ProblemDescription;
                        existingProblem.Priority = problemData.Priority;
                        existingProblem.FoundDate = problemData.FoundDate;
                        existingProblem.CompletedDate = problemData.CompletedDate;
                        existingProblem.ProcessingDays = CalculateProcessingDays(problemData.FoundDate, problemData.CompletedDate);
                        existingProblem.PrimaryDepartment = problemData.PrimaryDepartment;
                        existingProblem.PrimaryResponsible = problemData.PrimaryResponsible;
                        existingProblem.CollaboratingDepartment = problemData.CollaboratingDepartment;
                        existingProblem.CollaboratingPerson = problemData.CollaboratingPerson;
                        existingProblem.Status = problemData.Status;
                        existingProblem.Solution = problemData.Solution;
                        existingProblem.SolutionDetails = problemData.SolutionDetails;
                        existingProblem.VerificationStatus = problemData.VerificationStatus;
                        existingProblem.CustomerFeedback = problemData.CustomerFeedback;
                        existingProblem.SatisfactionScore = problemData.SatisfactionScore;
                        existingProblem.VerifiedAt = problemData.VerifiedAt;
                        existingProblem.VerifiedBy = problemData.VerifiedBy;
                        existingProblem.RelatedTicketNo = problemData.RelatedTicketNo;
                        existingProblem.KnowledgeBaseId = problemData.KnowledgeBaseId;
                        existingProblem.IsRepeatProblem = problemData.IsRepeatProblem;
                        existingProblem.Notes = problemData.Notes;
                        existingProblem.UpdatedAt = DateTime.UtcNow;
                        existingProblem.UpdatedBy = userId;
                        executionResult.ProblemsUpdated++;
                    }
                }

                await _dbContext.SaveChangesAsync();
            }

            executionResult.Message = $"成功导入 {executionResult.ProjectsCreated} 个新项目，更新 {executionResult.ProjectsUpdated} 个项目，创建 {executionResult.ProblemsCreated} 个新问题，更新 {executionResult.ProblemsUpdated} 个问题";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Excel导入时出错");
            executionResult.Success = false;
            executionResult.Message = $"导入失败: {ex.Message}";
            executionResult.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Field = "Import",
                ErrorType = "System",
                Message = ex.Message
            });
        }

        return executionResult;
    }

    #region 私有方法

    private Dictionary<string, int> ReadHeaders(ExcelWorksheet worksheet)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var headerValue = worksheet.Cells[1, col].Text?.Trim();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                headers[headerValue] = col;
            }
        }
        return headers;
    }

    private ProjectImportData? ParseProjectRow(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers)
    {
        var projectData = new ProjectImportData { ExcelRowNumber = row };

        // 尝试多种可能的列名
        projectData.ProjectNo = GetCellValue(worksheet, row, headers, new[] { "项目号", "项目号(PJ)", "ProjectNo", "项目编号" });
        projectData.ProjectName = GetCellValue(worksheet, row, headers, new[] { "项目名称", "ProjectName", "项目名" });
        projectData.CustomerName = GetCellValue(worksheet, row, headers, new[] { "客户名称", "CustomerName", "客户名" });
        projectData.DeviceType = GetCellValue(worksheet, row, headers, new[] { "设备类型", "DeviceType" });
        projectData.IndustryType = GetCellValue(worksheet, row, headers, new[] { "行业类型", "IndustryType", "行业" });
        projectData.ProjectStatus = GetCellValue(worksheet, row, headers, new[] { "项目状态", "ProjectStatus", "状态" }) ?? "进行中";
        projectData.ProjectManagerName = GetCellValue(worksheet, row, headers, new[] { "项目经理", "ProjectManager", "负责人" });

        // 解析日期
        projectData.OrderDate = ParseDate(GetCellValue(worksheet, row, headers, new[] { "下单日期", "OrderDate" }));
        projectData.RequiredDeliveryDate = ParseDate(GetCellValue(worksheet, row, headers, new[] { "要求交货日期", "RequiredDeliveryDate", "交货日期" }));
        projectData.ActualDeliveryDate = ParseDate(GetCellValue(worksheet, row, headers, new[] { "实际交货日期", "ActualDeliveryDate" }));

        // 解析数字
        projectData.SalesAmount = ParseDecimal(GetCellValue(worksheet, row, headers, new[] { "销售金额", "SalesAmount" }));
        projectData.Quantity = ParseInt(GetCellValue(worksheet, row, headers, new[] { "数量", "Quantity" })) ?? 1;

        if (string.IsNullOrWhiteSpace(projectData.ProjectNo))
        {
            return null;
        }

        return projectData;
    }

    private ProblemImportData? ParseProblemRow(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string projectNo)
    {
        var problemData = new ProblemImportData { ExcelRowNumber = row };

        // 尝试多种可能的列名
        problemData.ProblemSequence = ParseInt(GetCellValue(worksheet, row, headers, new[] { "问题序号", "ProblemSequence", "序号" })) ?? 1;
        problemData.ProblemCategory = GetCellValue(worksheet, row, headers, new[] { "问题分类", "ProblemCategory", "分类" }) ?? "其他";
        problemData.ProblemDescription = GetCellValue(worksheet, row, headers, new[] { "问题描述", "ProblemDescription", "描述" }) ?? string.Empty;
        problemData.Priority = GetCellValue(worksheet, row, headers, new[] { "优先级", "Priority" });
        problemData.PrimaryDepartment = GetCellValue(worksheet, row, headers, new[] { "主负责部门", "PrimaryDepartment", "负责部门", "部门" }) ?? string.Empty;
        problemData.PrimaryResponsible = GetCellValue(worksheet, row, headers, new[] { "主负责人", "PrimaryResponsible", "负责人" }) ?? string.Empty;
        problemData.CollaboratingDepartment = GetCellValue(worksheet, row, headers, new[] { "协作部门", "CollaboratingDepartment" });
        problemData.CollaboratingPerson = GetCellValue(worksheet, row, headers, new[] { "协作人员", "CollaboratingPerson" });
        problemData.Status = GetCellValue(worksheet, row, headers, new[] { "处理状态", "Status", "状态" }) ?? "待分配";
        problemData.Solution = GetCellValue(worksheet, row, headers, new[] { "处理方案", "Solution", "方案" });
        problemData.SolutionDetails = GetCellValue(worksheet, row, headers, new[] { "处理详情", "SolutionDetails" });
        problemData.VerificationStatus = GetCellValue(worksheet, row, headers, new[] { "验证状态", "VerificationStatus" });
        problemData.CustomerFeedback = GetCellValue(worksheet, row, headers, new[] { "客户反馈", "CustomerFeedback", "反馈" });
        problemData.VerifiedBy = GetCellValue(worksheet, row, headers, new[] { "验证人", "VerifiedBy" });
        problemData.RelatedTicketNo = GetCellValue(worksheet, row, headers, new[] { "关联工单号", "RelatedTicketNo", "工单号" });
        problemData.KnowledgeBaseId = GetCellValue(worksheet, row, headers, new[] { "知识库ID", "KnowledgeBaseId" });
        problemData.Notes = GetCellValue(worksheet, row, headers, new[] { "备注", "Notes" });

        // 解析日期
        problemData.FoundDate = ParseDate(GetCellValue(worksheet, row, headers, new[] { "发现日期", "FoundDate", "日期" })) ?? DateTime.UtcNow;
        problemData.CompletedDate = ParseDate(GetCellValue(worksheet, row, headers, new[] { "处理完成日期", "CompletedDate", "完成日期" }));
        problemData.VerifiedAt = ParseDate(GetCellValue(worksheet, row, headers, new[] { "验证时间", "VerifiedAt" }));

        // 解析数字
        problemData.SatisfactionScore = ParseInt(GetCellValue(worksheet, row, headers, new[] { "满意度评分", "SatisfactionScore", "满意度" }));
        problemData.IsRepeatProblem = GetCellValue(worksheet, row, headers, new[] { "是否重复问题", "IsRepeatProblem" })?.ToLower() == "是" || 
                                      GetCellValue(worksheet, row, headers, new[] { "是否重复问题", "IsRepeatProblem" })?.ToLower() == "true";

        if (string.IsNullOrWhiteSpace(problemData.ProblemDescription))
        {
            return null;
        }

        return problemData;
    }

    private string? GetCellValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string[] possibleColumnNames)
    {
        foreach (var columnName in possibleColumnNames)
        {
            if (headers.TryGetValue(columnName, out int col))
            {
                var value = worksheet.Cells[row, col].Text?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        return null;
    }

    private DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return null;
        }

        // 支持多种日期格式
        var formats = new[]
        {
            "yyyy.M.d", "yyyy.MM.dd", "yyyy-MM-dd", "yyyy/MM/dd",
            "yyyy.M", "yyyy.MM", "yyyy-MM", "yyyy/MM"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                // 如果只有年月，默认取月初
                if (format.Contains("yyyy.M") && !format.Contains("d"))
                {
                    return new DateTime(date.Year, date.Month, 1);
                }
                return date;
            }
        }

        // 尝试通用解析
        if (DateTime.TryParse(dateStr, out var parsedDate))
        {
            return parsedDate;
        }

        return null;
    }

    private decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }

    private int? CalculateDeliveryDelayDays(DateTime? requiredDate, DateTime? actualDate)
    {
        if (requiredDate.HasValue && actualDate.HasValue)
        {
            return (int)(actualDate.Value - requiredDate.Value).TotalDays;
        }
        return null;
    }

    private int? CalculateProcessingDays(DateTime foundDate, DateTime? completedDate)
    {
        if (completedDate.HasValue)
        {
            return (int)(completedDate.Value - foundDate).TotalDays;
        }
        return null;
    }

    private async Task<Guid> FindOrCreateCustomerAsync(string? customerName, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            // 如果没有客户名称，创建一个默认客户
            return Guid.Empty; // 或者创建一个默认客户
        }

        // 尝试从Ticket中查找客户
        var ticket = await _dbContext.Tickets
            .Where(t => t.CustomerId != Guid.Empty)
            .FirstOrDefaultAsync();

        if (ticket != null)
        {
            return ticket.CustomerId; // 使用现有客户ID
        }

        // 如果找不到，返回空GUID（需要后续处理）
        return Guid.Empty;
    }

    #endregion
}






