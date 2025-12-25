using FieldTicket.Shared.Models;
using System.Text.Json;

namespace FieldTicket.Core.Validators;

/// <summary>
/// 工单校验器
/// </summary>
public class TicketValidator
{
    /// <summary>
    /// 校验工单提交
    /// </summary>
    public ValidationResult ValidateForSubmit(TicketDto ticket, int attachmentCount)
    {
        var errors = new List<ValidationError>();

        // 1. domain 必填，必须是 A/B/C/D/E 之一
        if (ticket.Domain == '\0' || !"ABCDE".Contains(ticket.Domain))
        {
            errors.Add(new ValidationError
            {
                Field = "domain",
                Code = "REQUIRED",
                Message = "问题域不能为空，必须是 A/B/C/D/E 之一"
            });
        }

        // 2. stepCode 必填
        if (string.IsNullOrWhiteSpace(ticket.StepCode))
        {
            errors.Add(new ValidationError
            {
                Field = "stepCode",
                Code = "REQUIRED",
                Message = "步骤代码不能为空"
            });
        }

        // 3. symptomTitle 必填
        if (string.IsNullOrWhiteSpace(ticket.SymptomTitle))
        {
            errors.Add(new ValidationError
            {
                Field = "symptomTitle",
                Code = "REQUIRED",
                Message = "问题描述不能为空"
            });
        }
        else if (ticket.SymptomTitle.Length < 10 || ticket.SymptomTitle.Length > 200)
        {
            errors.Add(new ValidationError
            {
                Field = "symptomTitle",
                Code = "INVALID_LENGTH",
                Message = "问题描述长度必须在 10-200 字符之间"
            });
        }

        // 4. 版本信息必填
        if (string.IsNullOrWhiteSpace(ticket.SwVersion))
        {
            errors.Add(new ValidationError
            {
                Field = "swVersion",
                Code = "REQUIRED",
                Message = "软件版本不能为空"
            });
        }

        if (string.IsNullOrWhiteSpace(ticket.PlcVersion))
        {
            errors.Add(new ValidationError
            {
                Field = "plcVersion",
                Code = "REQUIRED",
                Message = "PLC版本不能为空"
            });
        }

        if (string.IsNullOrWhiteSpace(ticket.ParamVersion))
        {
            errors.Add(new ValidationError
            {
                Field = "paramVersion",
                Code = "REQUIRED",
                Message = "参数版本不能为空"
            });
        }

        // 5. factsJson 校验
        if (ticket.FactsJson == null || ticket.FactsJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ValidationError
            {
                Field = "factsJson",
                Code = "REQUIRED",
                Message = "事实表不能为空"
            });
        }
        else
        {
            var factsValidation = ValidateFactsJson(ticket.FactsJson, ticket.Domain);
            errors.AddRange(factsValidation);
        }

        // 6. 至少1个附件
        if (attachmentCount < 1)
        {
            errors.Add(new ValidationError
            {
                Field = "attachments",
                Code = "MIN_COUNT",
                Message = "至少需要上传1个证据附件"
            });
        }

        // 7. confirmedAsFact 必须为 true
        if (!ticket.ConfirmedAsFact)
        {
            errors.Add(new ValidationError
            {
                Field = "confirmedAsFact",
                Code = "REQUIRED",
                Message = "请确认以上为现场事实，不包含个人判断"
            });
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// 校验 factsJson
    /// </summary>
    private List<ValidationError> ValidateFactsJson(JsonDocument factsJson, char domain)
    {
        var errors = new List<ValidationError>();
        var root = factsJson.RootElement;

        // environment.repro_rate 必填（0-100）
        if (root.TryGetProperty("environment", out var env))
        {
            if (!env.TryGetProperty("repro_rate", out var reproRate) || 
                !reproRate.TryGetInt32(out var rate) || 
                rate < 0 || rate > 100)
            {
                errors.Add(new ValidationError
                {
                    Field = "factsJson.environment.repro_rate",
                    Code = "REQUIRED",
                    Message = "复现率必填，且必须在 0-100 之间"
                });
            }
        }
        else
        {
            errors.Add(new ValidationError
            {
                Field = "factsJson.environment",
                Code = "REQUIRED",
                Message = "环境事实表必填"
            });
        }

        // 根据问题域校验对应的维度
        var domainMap = new Dictionary<char, string>
        {
            { 'A', "mechanical" },
            { 'B', "electrical" },
            { 'C', "plc" },
            { 'D', "test" },
            { 'E', "environment" }
        };

        if (domainMap.TryGetValue(domain, out var requiredDomain))
        {
            if (!root.TryGetProperty(requiredDomain, out var domainFacts))
            {
                errors.Add(new ValidationError
                {
                    Field = $"factsJson.{requiredDomain}",
                    Code = "REQUIRED",
                    Message = $"选择问题域 {domain} 时，{requiredDomain} 事实表必填"
                });
            }
            else
            {
                // 检查至少1项非NA
                var hasNonNa = false;
                foreach (var prop in domainFacts.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = prop.Value.GetString();
                        if (value != "NA" && value != null)
                        {
                            hasNonNa = true;
                            break;
                        }
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.True || 
                             prop.Value.ValueKind == JsonValueKind.False)
                    {
                        hasNonNa = true;
                        break;
                    }
                }

                if (!hasNonNa)
                {
                    errors.Add(new ValidationError
                    {
                        Field = $"factsJson.{requiredDomain}",
                        Code = "INCOMPLETE",
                        Message = $"{requiredDomain} 事实表至少填写1项（非全NA）"
                    });
                }
            }

            // 特殊校验：domain=C 时，plc.stuck_step_code 必填
            if (domain == 'C' && root.TryGetProperty("plc", out var plc))
            {
                if (!plc.TryGetProperty("stuck_step_code", out var stuckStep) || 
                    string.IsNullOrWhiteSpace(stuckStep.GetString()))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "factsJson.plc.stuck_step_code",
                        Code = "REQUIRED",
                        Message = "选择PLC域时，卡在步骤代码必填"
                    });
                }
            }

            // 特殊校验：domain=D 时，如果有FAIL，test.fail_item 必填
            if (domain == 'D' && root.TryGetProperty("test", out var test))
            {
                // 这里可以根据实际业务逻辑判断是否有FAIL
                // 暂时跳过，因为需要更复杂的逻辑判断
            }
        }

        return errors;
    }
}


