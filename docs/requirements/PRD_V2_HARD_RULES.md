# v2.0 产品需求文档 - 硬规则（Hard Rules）

> **版本**：2.0  
> **目的**：定义系统级硬规则，避免系统失控  
> **最后更新**：2025-12-22

## ⚠️ 硬规则说明

硬规则是系统级约束，**不允许绕过或跳过**。违反硬规则的操作将被系统拒绝，并记录审计日志。

## 📋 硬规则清单

### 规则1：无判断卡不得结案

**规则ID**：HR-001

**规则描述**：
- 工单结案（状态变为 RESOLVED/CLOSED）时，必须关联判断卡
- 必须填写 `current_hypothesis`（当前假设）
- 必须填写 `next_action`（下一步动作）

**触发时机**：
- 工单状态变更：`Verifying → Closed`
- 工单状态变更：`Triage → Closed`（直接关闭）

**校验逻辑**：
```csharp
if (targetStatus == TicketStatus.Closed || targetStatus == TicketStatus.Resolved)
{
    if (string.IsNullOrEmpty(ticket.CurrentJcCode) ||
        string.IsNullOrEmpty(ticket.CurrentHypothesis) ||
        string.IsNullOrEmpty(ticket.NextAction))
    {
        throw new BusinessRuleException(
            "HR-001: 无法结案，必须关联判断卡并填写当前假设和下一步动作");
    }
}
```

**错误提示**：
```
无法结案：必须关联判断卡并填写以下信息：
- 当前假设（current_hypothesis）
- 下一步动作（next_action）

请先完成分诊并关联判断卡。
```

**审计要求**：
- 记录违反规则的尝试
- 记录操作人、时间、工单ID

---

### 规则2：低置信度自动升级

**规则ID**：HR-002

**规则描述**：
- 当判断卡置信度 ≤ 2（低置信度）时，自动设置 `escalation_required = true`
- 自动通知主管（Tech Lead 或 Admin）
- 工单不允许直接关闭，必须经过主管审核

**触发时机**：
- 分诊时设置置信度
- AI辅助判断时设置置信度

**校验逻辑**：
```csharp
if (triageResult.Confidence <= 2)
{
    ticket.EscalationRequired = true;
    ticket.EscalatedTo = await GetSupervisorAsync(ticket.Domain);
    ticket.EscalatedAt = DateTime.UtcNow;
    
    // 禁止直接关闭
    if (requestedStatus == TicketStatus.Closed)
    {
        throw new BusinessRuleException(
            "HR-002: 低置信度工单必须经过主管审核，无法直接关闭");
    }
    
    // 通知主管
    await NotifySupervisorAsync(ticket);
}
```

**通知内容**：
```
【低置信度工单需要审核】
工单：{ticket_no}
问题：{symptom_title}
置信度：{confidence}/5
当前假设：{current_hypothesis}
请审核并确认处理方案。
```

**审计要求**：
- 记录升级操作
- 记录主管审核结果

---

### 规则3：对外消息必须落库

**规则ID**：HR-003

**规则描述**：
- 所有客户侧沟通必须通过"口径输出层"生成
- 即使手动输入，也必须保存到 `customer_communications` 表
- 禁止绕过系统直接发送（如直接发微信、邮件）

**触发时机**：
- 任何对外沟通操作（微信、邮件、电话、现场）

**校验逻辑**：
```csharp
public async Task SendCustomerCommunicationAsync(
    Guid ticketId,
    CommunicationRequest request)
{
    // 硬规则：必须通过口径输出层
    var communication = await _communicationService.GenerateAsync(
        ticketId,
        request.Scenario,
        request.Context
    );
    
    // 必须保存到数据库
    await _communicationRepository.SaveAsync(communication);
    
    // 然后发送
    await _notificationService.SendAsync(communication);
}
```

**禁止操作**：
- ❌ 直接调用企业微信API发送消息（不经过系统）
- ❌ 直接发送邮件（不经过系统）
- ❌ 电话沟通不记录

**允许操作**：
- ✅ 通过系统生成话术（模板或AI）
- ✅ 手动编辑后发送（但必须保存）
- ✅ 复制系统生成的内容到外部工具（但必须先在系统记录）

**审计要求**：
- 所有沟通记录必须保存
- 记录生成方式（模板/AI/手动）
- 记录发送时间、接收人、内容

---

### 规则4：结案必须归因

**规则ID**：HR-004

**规则描述**：
- 工单结案时，必须填写以下字段：
  - `root_cause`（根因）
  - `responsibility_team`（责任团队）
  - `is_preventable`（是否可预防）

**触发时机**：
- 工单状态变更：`Verifying → Closed`
- 工单状态变更：`Triage → Closed`

**校验逻辑**：
```csharp
public async Task CloseTicketAsync(Guid ticketId, CloseTicketRequest request)
{
    // 硬规则检查
    if (string.IsNullOrEmpty(request.RootCause))
    {
        throw new ValidationException("HR-004: 结案必须填写根因（root_cause）");
    }
    
    if (string.IsNullOrEmpty(request.ResponsibilityTeam))
    {
        throw new ValidationException("HR-004: 结案必须填写责任团队（responsibility_team）");
    }
    
    if (request.IsPreventable == null)
    {
        throw new ValidationException("HR-004: 结案必须判断是否可预防（is_preventable）");
    }
    
    // 保存归因信息
    ticket.RootCause = request.RootCause;
    ticket.ResponsibilityTeam = request.ResponsibilityTeam;
    ticket.IsPreventable = request.IsPreventable;
    ticket.AttributedBy = currentUser.Id;
    ticket.AttributedAt = DateTime.UtcNow;
    
    // ... 其他逻辑
}
```

**表单字段**：
- **根因**：下拉选择（设计/软件/参数/装配/文档/其他/未知）
- **责任团队**：下拉选择（设计部/软件部/工程部/装配部/其他）
- **是否可预防**：单选（是/否）

**错误提示**：
```
无法结案：必须填写以下信息：
- 根因（root_cause）
- 责任团队（responsibility_team）
- 是否可预防（is_preventable）
```

**审计要求**：
- 记录归因信息
- 记录归因人、时间

---

### 规则5：重复问题阈值触发CAPA

**规则ID**：HR-005

**规则描述**：
- 当满足以下条件时，自动生成整改任务（CAPA）：
  - 相同设备型号
  - 相同根因（root_cause）
  - 30天内出现 ≥ N次（默认N=3，可配置）

**触发时机**：
- 工单结案时（状态变为 Closed）

**校验逻辑**：
```csharp
public async Task OnTicketClosedAsync(Guid ticketId)
{
    var ticket = await GetTicketAsync(ticketId);
    
    // 硬规则：必须已归因（规则4保证）
    if (string.IsNullOrEmpty(ticket.RootCause))
    {
        return; // 规则4会阻止结案，这里不会执行到
    }
    
    // 查找相似工单
    var similarTickets = await _ticketRepository.FindSimilarAsync(
        deviceModel: ticket.Device.Model,
        rootCause: ticket.RootCause,
        withinDays: 30,
        excludeTicketId: ticketId
    );
    
    // 检查阈值
    var threshold = await _configService.GetCAPAThresholdAsync();
    if (similarTickets.Count >= threshold.Count)
    {
        // 自动生成整改任务
        var correctiveAction = await _correctiveActionService.CreateAsync(
            new CreateCorrectiveActionRequest
            {
                TriggerType = "threshold",
                RelatedTicketIds = similarTickets.Select(t => t.Id).Append(ticketId).ToList(),
                ProblemDescription = $"设备型号 {ticket.Device.Model} 在30天内出现 {similarTickets.Count + 1} 次相同根因问题：{ticket.RootCause}",
                RootResponsibility = ticket.RootCause,
                ResponsiblePersonId = await GetResponsiblePersonAsync(ticket.RootCause)
            }
        );
        
        // 通知相关人员
        await NotifyCAPACreatedAsync(correctiveAction);
    }
}
```

**匹配条件**（可配置）：
```json
{
  "threshold": {
    "days": 30,
    "count": 3,
    "match_criteria": {
      "same_device_model": true,
      "same_root_cause": true,
      "same_symptom": false  // 可选
    }
  }
}
```

**整改任务生成**：
- 自动生成任务编号：`CAPA-YYYY-NNN`
- 关联所有相关工单
- 自动分配负责人（根据根因类型）
- 设置目标完成日期（默认30天）

**审计要求**：
- 记录触发条件
- 记录关联工单
- 记录整改任务创建

---

## 🔧 硬规则实现要求

### 1. 统一校验入口

所有硬规则校验应在统一的服务层进行：

```csharp
public class HardRuleValidator
{
    public async Task ValidateBeforeCloseAsync(Ticket ticket)
    {
        // 规则1：无判断卡不得结案
        await ValidateRule1_JudgementCardRequired(ticket);
        
        // 规则4：结案必须归因
        await ValidateRule4_AttributionRequired(ticket);
    }
    
    public async Task ValidateConfidenceAsync(Ticket ticket, int confidence)
    {
        // 规则2：低置信度自动升级
        if (confidence <= 2)
        {
            await ApplyRule2_AutoEscalation(ticket);
        }
    }
    
    public async Task ValidateCustomerCommunicationAsync(CommunicationRequest request)
    {
        // 规则3：对外消息必须落库
        await ValidateRule3_MustBeLogged(request);
    }
    
    public async Task OnTicketClosedAsync(Ticket ticket)
    {
        // 规则5：重复问题阈值触发CAPA
        await CheckRule5_CAPATrigger(ticket);
    }
}
```

### 2. 错误处理

硬规则违反时：
- 返回明确的错误码（如 `HR-001`）
- 提供清晰的错误消息
- 记录审计日志
- 不允许静默失败

### 3. 配置化

部分硬规则参数可配置：
- CAPA阈值（N次）
- 置信度阈值（≤2）
- 时间窗口（30天）

**配置位置**：系统设置 → 硬规则配置

---

## 📊 硬规则监控

### 监控指标

| 指标 | 说明 | 目标 |
|------|------|------|
| 硬规则违反次数 | 违反硬规则的尝试次数 | 0（不允许违反） |
| 规则1违反率 | 无判断卡尝试结案的比例 | 0% |
| 规则2触发率 | 低置信度自动升级的比例 | 100% |
| 规则3完整率 | 对外消息落库的比例 | 100% |
| 规则4完整率 | 结案归因完整的比例 | 100% |
| 规则5触发率 | 满足阈值触发CAPA的比例 | 100% |

### 审计报表

定期生成硬规则执行报告：
- 违反规则统计
- 规则执行情况
- 异常情况分析

---

## 🔄 硬规则变更流程

硬规则的变更需要：
1. 产品经理提出变更需求
2. 技术评审（评估影响）
3. 业务评审（评估风险）
4. 版本发布（记录变更历史）

**变更记录**：
- 规则ID
- 变更前规则
- 变更后规则
- 变更原因
- 变更时间
- 变更人

---

**最后更新**：2025-12-22  
**版本**：2.0



