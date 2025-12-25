namespace FieldTicket.Infrastructure.WeCom;

/// <summary>
/// 企业微信通知模板
/// </summary>
public static class NotificationTemplates
{
    /// <summary>
    /// 工单提交通知模板
    /// </summary>
    public const string TicketSubmitted = @"【新工单】{ticket_no}
客户：{customer_name}
设备：{device_sn}
问题：{symptom_title}
紧急度：{priority}
提交人：{creator_name}
<a href=""{web_url}"">点击处理</a>";

    /// <summary>
    /// 解决方案发布通知模板
    /// </summary>
    public const string SolutionPublished = @"【解决方案】{solution_code} 已发布
工单：{ticket_no}
方案：{summary}
请按验证清单执行并反馈结果
<a href=""{app_deeplink}"">打开App查看</a>";

    /// <summary>
    /// 替换模板变量
    /// </summary>
    public static string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var kvp in variables)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value ?? string.Empty);
        }
        return result;
    }
}


