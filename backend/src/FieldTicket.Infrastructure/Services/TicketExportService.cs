using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Text;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单导出服务实现
/// </summary>
public class TicketExportService : ITicketExportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketExportService> _logger;

    public TicketExportService(
        ApplicationDbContext dbContext,
        ITicketService ticketService,
        ILogger<TicketExportService> logger)
    {
        _dbContext = dbContext;
        _ticketService = ticketService;
        _logger = logger;
    }

    public async Task<byte[]> ExportToExcelAsync(
        TicketQueryFilter filter,
        List<string>? fields = null)
    {
        // 获取工单列表
        var (items, _) = await _ticketService.GetTicketsAsync(filter, 1, int.MaxValue);

        // 默认导出字段
        var defaultFields = new List<string>
        {
            "ticketNo", "status", "domain", "stepCode", "symptomTitle",
            "priority", "customerName", "deviceSn", "createdByName", "createdAt"
        };

        var exportFields = fields ?? defaultFields;

        // 使用 EPPlus 生成真正的 Excel 文件
        return GenerateExcelData(items, exportFields);
    }

    public async Task<byte[]> ExportToCsvAsync(
        TicketQueryFilter filter,
        List<string>? fields = null)
    {
        // 获取工单列表
        var (items, _) = await _ticketService.GetTicketsAsync(filter, 1, int.MaxValue);

        // 默认导出字段
        var defaultFields = new List<string>
        {
            "ticketNo", "status", "domain", "stepCode", "symptomTitle",
            "priority", "customerName", "deviceSn", "createdByName", "createdAt"
        };

        var exportFields = fields ?? defaultFields;

        return GenerateCsvData(items, exportFields);
    }

    public async Task<byte[]> ExportTicketDetailToExcelAsync(Guid ticketId)
    {
        var ticket = await _ticketService.GetTicketAsync(ticketId);
        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 生成工单详情的 CSV 格式
        var sb = new StringBuilder();
        sb.AppendLine("字段,值");
        sb.AppendLine($"工单编号,{ticket.TicketNo ?? "草稿"}");
        sb.AppendLine($"状态,{ticket.Status}");
        sb.AppendLine($"问题域,{ticket.Domain}");
        sb.AppendLine($"步骤代码,{ticket.StepCode}");
        sb.AppendLine($"步骤名称,{ticket.StepName ?? ""}");
        sb.AppendLine($"症状标题,{ticket.SymptomTitle}");
        sb.AppendLine($"症状详情,{ticket.SymptomDetail ?? ""}");
        sb.AppendLine($"优先级,{ticket.Priority}");
        sb.AppendLine($"复现率,{ticket.ReproRate ?? 0}%");
        sb.AppendLine($"重启恢复,{ticket.RebootRecovers ?? false}");
        sb.AppendLine($"环境相关,{ticket.EnvRelated ?? false}");
        sb.AppendLine($"软件版本,{ticket.SwVersion}");
        sb.AppendLine($"PLC版本,{ticket.PlcVersion}");
        sb.AppendLine($"参数版本,{ticket.ParamVersion}");
        sb.AppendLine($"创建时间,{ticket.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"更新时间,{ticket.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

        // 添加事实表
        if (ticket.FactsJson != null && ticket.FactsJson.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            sb.AppendLine("事实表,");
            foreach (var prop in ticket.FactsJson.RootElement.EnumerateObject())
            {
                sb.AppendLine($"{prop.Name},{prop.Value}");
            }
        }

        // 添加已采取行动
        if (ticket.ActionsTaken != null && ticket.ActionsTaken.Count > 0)
        {
            sb.AppendLine("已采取行动,");
            foreach (var action in ticket.ActionsTaken)
            {
                sb.AppendLine($",{action}");
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private byte[] GenerateCsvData(List<TicketListItemDto> items, List<string> fields)
    {
        var sb = new StringBuilder();

        // 字段名映射
        var fieldMap = new Dictionary<string, (string Header, Func<TicketListItemDto, string> GetValue)>
        {
            ["ticketNo"] = ("工单编号", t => t.TicketNo ?? "草稿"),
            ["status"] = ("状态", t => t.Status),
            ["domain"] = ("问题域", t => t.Domain.ToString()),
            ["stepCode"] = ("步骤代码", t => t.StepCode ?? ""),
            ["symptomTitle"] = ("症状标题", t => t.SymptomTitle ?? ""),
            ["priority"] = ("优先级", t => t.Priority ?? "P3"),
            ["customerName"] = ("客户名称", t => t.CustomerName ?? ""),
            ["deviceSn"] = ("设备SN", t => t.DeviceSn ?? ""),
            ["createdByName"] = ("创建人", t => t.CreatedByName ?? ""),
            ["createdAt"] = ("创建时间", t => t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
        };

        // 生成表头
        var headers = fields
            .Where(f => fieldMap.ContainsKey(f))
            .Select(f => fieldMap[f].Header)
            .ToList();
        sb.AppendLine(string.Join(",", headers));

        // 生成数据行
        foreach (var item in items)
        {
            var values = fields
                .Where(f => fieldMap.ContainsKey(f))
                .Select(f => EscapeCsvValue(fieldMap[f].GetValue(item)))
                .ToList();
            sb.AppendLine(string.Join(",", values));
        }

        // 添加 BOM 以支持中文
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var bom = Encoding.UTF8.GetPreamble();
        return bom.Concat(bytes).ToArray();
    }

    private string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        // 如果包含逗号、引号或换行符，需要用引号包裹
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            // 转义引号
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }

    /// <summary>
    /// 生成 Excel 数据
    /// </summary>
    private byte[] GenerateExcelData(List<TicketListItemDto> items, List<string> fields)
    {
        // 设置 EPPlus 许可证上下文（非商业使用）
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("工单列表");

        // 字段名映射
        var fieldMap = new Dictionary<string, (string Header, Func<TicketListItemDto, object> GetValue)>
        {
            ["ticketNo"] = ("工单编号", t => (object)(t.TicketNo ?? "草稿")),
            ["status"] = ("状态", t => (object)(t.Status)),
            ["domain"] = ("问题域", t => (object)t.Domain.ToString()),
            ["stepCode"] = ("步骤代码", t => (object)(t.StepCode ?? "")),
            ["symptomTitle"] = ("症状标题", t => (object)(t.SymptomTitle ?? "")),
            ["priority"] = ("优先级", t => (object)(t.Priority ?? "P3")),
            ["customerName"] = ("客户名称", t => (object)(t.CustomerName ?? "")),
            ["deviceSn"] = ("设备SN", t => (object)(t.DeviceSn ?? "")),
            ["createdByName"] = ("创建人", t => (object)(t.CreatedByName ?? "")),
            ["createdAt"] = ("创建时间", t => (object)t.CreatedAt),
        };

        // 生成表头
        var headers = fields
            .Where(f => fieldMap.ContainsKey(f))
            .Select(f => fieldMap[f].Header)
            .ToList();

        // 设置表头样式
        var headerRange = worksheet.Cells[1, 1, 1, headers.Count];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        headerRange.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

        // 写入表头
        for (int i = 0; i < headers.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // 写入数据
        for (int row = 0; row < items.Count; row++)
        {
            var item = items[row];
            int col = 1;

            foreach (var field in fields.Where(f => fieldMap.ContainsKey(f)))
            {
                var value = fieldMap[field].GetValue(item);
                worksheet.Cells[row + 2, col].Value = value;

                // 格式化日期列
                if (field == "createdAt" && value is DateTime dateTime)
                {
                    worksheet.Cells[row + 2, col].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                }

                col++;
            }

            // 设置边框
            var rowRange = worksheet.Cells[row + 2, 1, row + 2, headers.Count];
            rowRange.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
        }

        // 自动调整列宽
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }
}








