using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 事实表相关端点
/// </summary>
public static class FactTableEndpoints
{
    /// <summary>
    /// 事实表问题定义
    /// </summary>
    private static readonly Dictionary<char, List<FactQuestionDto>> FactQuestions = new()
    {
        {
            'A', new List<FactQuestionDto>
            {
                new() { Key = "mechanical_action_completed", Label = "动作是否完成", Type = "yes_no_na" },
                new() { Key = "mechanical_position_reliable", Label = "到位是否可靠", Type = "yes_no_na" },
                new() { Key = "mechanical_jam_or_noise", Label = "是否卡滞/异音", Type = "yes_no_na" },
                new() { Key = "mechanical_manual_help_recovers", Label = "人工辅助后是否恢复", Type = "yes_no_na" },
            }
        },
        {
            'B', new List<FactQuestionDto>
            {
                new() { Key = "electrical_sensor_physical_ok", Label = "传感器物理状态正常", Type = "yes_no_na" },
                new() { Key = "electrical_plc_io_changes", Label = "PLC中IO有变化", Type = "yes_no_na" },
                new() { Key = "electrical_similar_points_ok", Label = "同类点位是否正常", Type = "yes_no_na" },
            }
        },
        {
            'C', new List<FactQuestionDto>
            {
                new() { Key = "plc_stuck_step_code", Label = "卡在步骤代码", Type = "text", Required = true },
                new() { Key = "plc_stuck_fixed", Label = "是否固定卡在该步骤", Type = "yes_no_na" },
                new() { Key = "plc_manual_single_step_pass", Label = "手动/单步是否可通过", Type = "yes_no_na" },
                new() { Key = "plc_alarm_code", Label = "报警代码", Type = "text" },
            }
        },
        {
            'D', new List<FactQuestionDto>
            {
                new() { Key = "test_same_unit_repeat_consistent", Label = "同一产品重复测试结果一致", Type = "yes_no_na" },
                new() { Key = "test_swap_unit_recovers", Label = "更换产品是否恢复", Type = "yes_no_na" },
                new() { Key = "test_near_limits", Label = "测试值接近上下限", Type = "yes_no_na" },
            }
        },
        {
            'E', new List<FactQuestionDto>
            {
                new() { Key = "repro_rate", Label = "复现率 (%)", Type = "number", Required = true },
                new() { Key = "reboot_recovers", Label = "重启后是否恢复", Type = "boolean" },
                new() { Key = "env_related", Label = "是否与环境相关", Type = "boolean" },
            }
        }
    };

    public static void MapFactTableEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/fact-table")
            .WithTags("FactTable")
            .RequireAuthorization();

        // 获取事实表问题列表（根据问题域）
        group.MapGet("questions", GetFactQuestions)
            .WithName("GetFactQuestions")
            .WithSummary("获取事实表问题列表")
            .Produces<List<FactQuestionDto>>();
    }

    /// <summary>
    /// 获取事实表问题列表
    /// </summary>
    private static IResult GetFactQuestions([FromQuery] char domain)
    {
        if (!"ABCDE".Contains(domain))
        {
            return Results.BadRequest(new { message = "问题域必须是 A/B/C/D/E 之一" });
        }

        var questions = FactQuestions.TryGetValue(domain, out var q) ? q : new List<FactQuestionDto>();
        return Results.Ok(questions);
    }
}

/// <summary>
/// 事实表问题DTO
/// </summary>
public class FactQuestionDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "yes_no_na"; // yes_no_na, text, number, boolean
    public bool Required { get; set; } = false;
}






