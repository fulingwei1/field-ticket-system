using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// Excel导入服务接口
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// 解析Excel文件，返回项目-问题数据
    /// </summary>
    Task<ExcelImportResult> ParseExcelAsync(Stream excelStream, string fileName);

    /// <summary>
    /// 验证导入数据
    /// </summary>
    Task<ValidationResult> ValidateImportDataAsync(ExcelImportResult importResult);

    /// <summary>
    /// 执行导入，保存到数据库
    /// </summary>
    Task<ImportExecutionResult> ExecuteImportAsync(ExcelImportResult importResult, Guid userId);
}






