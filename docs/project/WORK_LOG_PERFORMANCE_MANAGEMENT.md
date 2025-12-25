# 客服工程师绩效管理系统

> **日期**：2025-12-22  
> **更新**：2025-12-22 - 移除工作日志功能，所有数据从工单系统自动采集  
> **目标**：建立客服工程师绩效评估系统，通过AI分析提供人员合理安排建议

---

## 🎯 需求分析

### 核心需求

1. **绩效管理模型**
   - 多维度绩效指标（23个指标，7个维度）
   - 绩效评分和排名
   - 绩效趋势分析

2. **AI总结和建议**
   - AI自动分析工单处理记录
   - 分析工作负荷分布
   - 提供人员合理安排建议

3. **权限管理**
   - 工程师只能查看自己的绩效
   - 部门经理可以查看所有人的绩效
   - 支持按时间、人员、项目筛选

4. **绩效指标**（详见《绩效指标体系设计》和《岗位职责分析与绩效管理》文档）
   
   **核心维度（基于岗位职责）**：
   - **主动性维度**：主动创建工单率、主动跟进工单率、主动知识贡献度、主动改进建议数
   - **积极性维度**：工作负荷承受度、复杂问题处理意愿、加班处理工单率、主动学习行为
   - **及时性维度**：工单响应及时率、追问回复及时性、验证执行及时性、客户沟通及时性
   - **知识技能等级维度**：技术诊断准确率、判断卡使用准确率、复杂问题解决能力、知识掌握广度、知识掌握深度、学习成长速度
   
   **原有维度（业务指标）**：
   - **工单处理效率维度**：工单创建完整度、现场问题反馈及时率、工单响应及时率、追问回复及时性
   - **问题解决能力维度**：一次解决率、平均解决时间、验证通过率、重复问题率
   - **技术诊断能力维度**：判断卡使用准确率、判断卡命中率、AI建议采纳率、低置信度升级及时性
   - **知识贡献维度**：判断卡创建数量、判断卡质量评分、判断卡复用贡献、解决方案贡献
   - **客户服务能力维度**：客户沟通及时性、客户沟通质量、客户满意度
   - **协作能力维度**：团队协作活跃度、知识分享贡献
   - **工作规范性维度**：工单信息完整性、责任归因完成度、工作活动完整性

---

## 📊 数据模型设计

> **说明**：所有绩效数据均从工单系统自动采集，无需工作日志表。工作内容、时间、地点等都可以从工单处理记录自动生成。

### 1. 绩效指标表

```sql
-- 绩效指标表（按周期统计）
CREATE TABLE performance_metrics (
    metric_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 关联信息
    engineer_id UUID NOT NULL REFERENCES users(id),
    period_type VARCHAR(20) NOT NULL,  -- 'daily', 'weekly', 'monthly', 'quarterly', 'yearly'
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    
    -- 工单相关指标
    total_tickets INT DEFAULT 0,  -- 总工单数
    tickets_resolved INT DEFAULT 0,  -- 已解决工单数
    tickets_pending INT DEFAULT 0,  -- 待处理工单数
    average_resolution_time INTERVAL,  -- 平均解决时间
    first_time_resolution_rate DECIMAL(5, 2),  -- 一次解决率
    
    -- 响应时间指标
    average_response_time INTERVAL,  -- 平均响应时间
    response_time_p95 INTERVAL,  -- 95分位响应时间
    on_time_response_rate DECIMAL(5, 2),  -- 及时响应率
    
    -- 设备故障率
    devices_serviced INT DEFAULT 0,  -- 服务设备数
    device_failure_rate DECIMAL(5, 2),  -- 设备故障率
    repeat_failure_rate DECIMAL(5, 2),  -- 重复故障率
    
    -- 工作日志指标
    work_logs_completed INT DEFAULT 0,  -- 完成的工作日志数
    work_logs_completeness DECIMAL(5, 2),  -- 工作日志完整度
    total_work_hours DECIMAL(8, 2),  -- 总工作小时数
    average_daily_hours DECIMAL(5, 2),  -- 平均每日工作小时数
    
    -- 考勤指标
    attendance_days INT DEFAULT 0,  -- 出勤天数
    late_count INT DEFAULT 0,  -- 迟到次数
    early_leave_count INT DEFAULT 0,  -- 早退次数
    absent_count INT DEFAULT 0,  -- 缺勤次数
    overtime_hours DECIMAL(6, 2) DEFAULT 0,  -- 加班小时数
    attendance_rate DECIMAL(5, 2),  -- 出勤率
    
    -- 客户满意度
    customer_satisfaction_score DECIMAL(3, 2),  -- 客户满意度评分（1-5）
    customer_feedback_count INT DEFAULT 0,  -- 客户反馈数量
    
    -- 综合评分
    overall_score DECIMAL(5, 2),  -- 综合评分（0-100）
    performance_level VARCHAR(20),  -- 'excellent', 'good', 'average', 'below_average', 'poor'
    
    -- 排名
    rank_in_team INT,  -- 团队内排名
    rank_in_department INT,  -- 部门内排名
    
    -- 审计字段
    calculated_at TIMESTAMPTZ DEFAULT NOW(),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(engineer_id, period_type, period_start)
);

CREATE INDEX idx_performance_engineer ON performance_metrics(engineer_id, period_start DESC);
CREATE INDEX idx_performance_period ON performance_metrics(period_type, period_start DESC);
CREATE INDEX idx_performance_score ON performance_metrics(overall_score DESC);
```

### 2. AI分析结果表

```sql
-- AI分析结果表
CREATE TABLE ai_analysis_results (
    analysis_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 分析范围
    analysis_type VARCHAR(50) NOT NULL,  -- 'daily_summary', 'weekly_summary', 'team_analysis', 'scheduling_suggestion'
    analysis_date DATE NOT NULL,
    engineer_id UUID REFERENCES users(id),  -- 如果为空，则为团队分析
    department_id UUID REFERENCES departments(id),
    
    -- 分析内容
    summary TEXT NOT NULL,  -- AI生成的总结
    key_insights JSONB,  -- 关键洞察
    /*
    {
      "workload_distribution": {
        "field_service": 60,
        "remote_support": 30,
        "other": 10
      },
      "peak_periods": ["09:00-11:00", "14:00-16:00"],
      "bottlenecks": ["设备故障处理", "客户沟通"]
    }
    */
    
    -- 建议
    suggestions JSONB,  -- AI生成的建议
    /*
    {
      "scheduling_suggestions": [
        {
          "engineer_id": "uuid",
          "suggestion": "建议增加现场服务时间，减少远程支持时间",
          "reason": "现场服务效率更高",
          "priority": "high"
        }
      ],
      "workload_balance": [
        {
          "from_engineer": "uuid",
          "to_engineer": "uuid",
          "suggestion": "建议将部分工单从A工程师转移到B工程师",
          "reason": "B工程师在该领域更专业",
          "expected_improvement": "预计提升20%效率"
        }
      ]
    }
    */
    
    -- 绩效分析
    performance_analysis JSONB,  -- 绩效分析
    /*
    {
      "top_performers": ["engineer1", "engineer2"],
      "improvement_areas": [
        {
          "engineer_id": "uuid",
          "area": "响应时间",
          "current": "2小时",
          "target": "1小时",
          "improvement_suggestion": "建议优化工作流程"
        }
      ]
    }
    */
    
    -- AI模型信息
    ai_model VARCHAR(100),  -- 使用的AI模型
    confidence_score DECIMAL(3, 2),  -- 置信度（0-1）
    
    -- 审计字段
    created_at TIMESTAMPTZ DEFAULT NOW(),
    created_by UUID REFERENCES users(id)
);

CREATE INDEX idx_ai_analysis_type ON ai_analysis_results(analysis_type, analysis_date DESC);
CREATE INDEX idx_ai_analysis_engineer ON ai_analysis_results(engineer_id, analysis_date DESC);
```

---

## 🔄 工作数据自动生成

> **说明**：所有工作相关数据（工作内容、时间、地点、成果）都从工单处理记录自动生成，无需工作日志。

### 1. 工作内容自动生成

基于工单处理记录自动生成：
- 处理的工单数
- 解决的工单数
- 创建的判断卡数
- 发布的解决方案数

### 2. 工作时间自动计算

基于工单处理时间自动计算：
- 工单处理总时长
- 平均处理时长
- 工作时间分布

### 3. 工作地点自动获取

从工单关联的设备位置自动获取：
- 工作地点列表
- 客户拜访记录

### 4. 工作成果自动统计

从工单统计自动生成：
- 工单处理统计
- 判断卡创建统计
- 解决方案发布统计

详见：[工作日志必要性分析](./WORK_LOG_ANALYSIS.md)

---

## 🔌 API接口设计

### 1. 绩效管理API

```csharp
// 绩效指标
GET    /api/performance/metrics           // 获取绩效指标列表
GET    /api/performance/metrics/{metricId}  // 获取绩效指标详情
GET    /api/performance/engineer/{engineerId}  // 获取工程师绩效
GET    /api/performance/team             // 获取团队绩效
GET    /api/performance/ranking         // 获取绩效排名

// 绩效计算
POST   /api/performance/calculate        // 手动触发绩效计算
GET    /api/performance/trends            // 获取绩效趋势

// 绩效报告
GET    /api/performance/reports          // 获取绩效报告
POST   /api/performance/reports/generate // 生成绩效报告
```

### 2. AI分析API

```csharp
// AI分析
POST   /api/ai-analysis/daily-summary    // 生成每日总结
POST   /api/ai-analysis/weekly-summary   // 生成每周总结
POST   /api/ai-analysis/team-analysis   // 生成团队分析
POST   /api/ai-analysis/scheduling-suggestion  // 生成人员安排建议

// 分析结果查询
GET    /api/ai-analysis/results          // 获取分析结果列表
GET    /api/ai-analysis/results/{analysisId}  // 获取分析结果详情
```

---

## 🤖 AI分析功能设计

### 1. 每日工作总结

**功能**：
- 分析工程师当日工作日志
- 生成工作摘要
- 识别关键工作内容
- 评估工作负荷

**输入**：
- 工程师当日工作日志
- 关联的工单信息
- 考勤信息

**输出**：
- 工作摘要（文字描述）
- 关键指标（处理的工单数、工作时长等）
- 工作负荷评估（正常/繁忙/超负荷）

### 2. 每周/每月团队分析

**功能**：
- 分析团队整体工作状况
- 识别工作负荷不均衡
- 发现潜在问题
- 提供改进建议

**输入**：
- 团队所有成员的工作日志
- 团队绩效指标
- 工单处理情况

**输出**：
- 团队工作摘要
- 工作负荷分布分析
- 人员合理安排建议
- 绩效改进建议

### 3. 人员合理安排建议

**功能**：
- 分析每个工程师的工作负荷
- 分析工程师的专业领域和擅长方向
- 分析工单分布和难度
- 提供最优人员分配建议

**输入**：
- 工程师工作日志
- 工程师绩效指标
- 待分配工单
- 工程师专业领域标签

**输出**：
- 人员分配建议（谁应该处理哪些工单）
- 工作负荷平衡建议
- 专业匹配建议
- 预期效果评估

### 4. 绩效分析

**功能**：
- 分析工程师绩效趋势
- 识别优秀表现者
- 识别需要改进的领域
- 提供个性化改进建议

**输入**：
- 工程师历史绩效数据
- 工作日志数据
- 工单处理数据

**输出**：
- 绩效趋势分析
- 优秀表现者识别
- 改进领域识别
- 个性化改进建议

---

## 📊 绩效指标计算

### 1. 设备故障率

```csharp
public decimal CalculateDeviceFailureRate(
    Guid engineerId,
    DateTime fromDate,
    DateTime toDate)
{
    // 获取工程师服务的设备数
    var devicesServiced = await GetDevicesServicedAsync(engineerId, fromDate, toDate);
    
    // 获取故障设备数
    var failedDevices = await GetFailedDevicesAsync(engineerId, fromDate, toDate);
    
    if (devicesServiced == 0) return 0;
    
    return (decimal)failedDevices / devicesServiced * 100;
}
```

### 2. 故障处理时间

```csharp
public TimeSpan CalculateAverageResolutionTime(
    Guid engineerId,
    DateTime fromDate,
    DateTime toDate)
{
    var tickets = await GetResolvedTicketsAsync(engineerId, fromDate, toDate);
    
    if (tickets.Count == 0) return TimeSpan.Zero;
    
    var totalTime = tickets.Sum(t => 
        (t.ResolvedAt - t.CreatedAt).TotalHours);
    
    return TimeSpan.FromHours(totalTime / tickets.Count);
}
```

### 3. 现场问题反馈及时率

```csharp
public decimal CalculateFeedbackTimelinessRate(
    Guid engineerId,
    DateTime fromDate,
    DateTime toDate)
{
    var tickets = await GetTicketsAsync(engineerId, fromDate, toDate);
    
    // 定义及时反馈标准（例如：2小时内）
    var timelyThreshold = TimeSpan.FromHours(2);
    
    var timelyTickets = tickets.Count(t => 
        t.FirstResponseTime <= timelyThreshold);
    
    if (tickets.Count == 0) return 0;
    
    return (decimal)timelyTickets / tickets.Count * 100;
}
```

### 4. 响应及时性

```csharp
public decimal CalculateResponseTimeliness(
    Guid engineerId,
    DateTime fromDate,
    DateTime toDate)
{
    var tickets = await GetTicketsAsync(engineerId, fromDate, toDate);
    
    // 定义及时响应标准（例如：30分钟内）
    var timelyThreshold = TimeSpan.FromMinutes(30);
    
    var timelyResponses = tickets.Count(t => 
        t.FirstResponseTime <= timelyThreshold);
    
    if (tickets.Count == 0) return 0;
    
    return (decimal)timelyResponses / tickets.Count * 100;
}
```

### 5. 工作日志完整性

```csharp
public decimal CalculateWorkLogCompleteness(
    Guid engineerId,
    DateTime fromDate,
    DateTime toDate)
{
    var workingDays = GetWorkingDays(fromDate, toDate);
    var completedLogs = await GetCompletedLogsAsync(engineerId, fromDate, toDate);
    
    if (workingDays == 0) return 0;
    
    return (decimal)completedLogs.Count / workingDays * 100;
}
```

### 6. 考勤情况

```csharp
public AttendanceMetrics CalculateAttendanceMetrics(
    Guid engineerId,
    DateTime fromDate,
    DateTime toDate)
{
    var logs = await GetWorkLogsAsync(engineerId, fromDate, toDate);
    var workingDays = GetWorkingDays(fromDate, toDate);
    
    return new AttendanceMetrics
    {
        AttendanceDays = logs.Count(l => !l.IsAbsent),
        LateCount = logs.Count(l => l.IsLate),
        EarlyLeaveCount = logs.Count(l => l.IsEarlyLeave),
        AbsentCount = workingDays - logs.Count(l => !l.IsAbsent),
        AttendanceRate = (decimal)logs.Count(l => !l.IsAbsent) / workingDays * 100,
        OvertimeHours = logs.Sum(l => l.OvertimeHours ?? 0)
    };
}
```

### 7. 综合评分

```csharp
public decimal CalculateOverallScore(
    PerformanceMetrics metrics)
{
    // 权重配置
    var weights = new Dictionary<string, decimal>
    {
        { "resolution_time", 0.25m },
        { "response_timeliness", 0.20m },
        { "first_time_resolution", 0.20m },
        { "work_log_completeness", 0.15m },
        { "attendance_rate", 0.10m },
        { "customer_satisfaction", 0.10m }
    };
    
    // 标准化各项指标（0-100分）
    var normalizedScores = new Dictionary<string, decimal>
    {
        { "resolution_time", NormalizeResolutionTime(metrics.AverageResolutionTime) },
        { "response_timeliness", metrics.ResponseTimelinessRate ?? 0 },
        { "first_time_resolution", metrics.FirstTimeResolutionRate ?? 0 },
        { "work_log_completeness", metrics.WorkLogsCompleteness ?? 0 },
        { "attendance_rate", metrics.AttendanceRate ?? 0 },
        { "customer_satisfaction", (metrics.CustomerSatisfactionScore ?? 0) * 20 }
    };
    
    // 加权平均
    var overallScore = weights.Sum(w => 
        normalizedScores[w.Key] * w.Value);
    
    return overallScore;
}
```

---

## 🖥️ 前端功能设计

### 1. 绩效看板（工程师）

**路径**：`/performance/my`

**功能**：
- 个人绩效指标
- 绩效趋势图
- 排名信息
- 改进建议

### 2. 团队绩效看板（部门经理）

**路径**：`/performance/team`

**功能**：
- 团队绩效概览
- 成员绩效对比
- 绩效排名
- 工作负荷分布

### 3. AI分析结果页面

**路径**：`/ai-analysis`

**功能**：
- AI生成的总结
- 人员安排建议
- 绩效分析
- 改进建议

---

## 🔐 权限控制

### 1. 工作日志权限

- **工程师**：
  - 可以创建、查看、编辑自己的工作日志
  - 可以提交自己的工作日志
  - 不能查看其他人的工作日志

- **部门经理**：
  - 可以查看部门内所有工程师的工作日志
  - 可以审核工作日志
  - 可以查看统计信息

- **管理员**：
  - 可以查看所有工作日志
  - 可以管理所有工作日志

### 2. 绩效管理权限

- **工程师**：
  - 可以查看自己的绩效指标
  - 可以查看自己的排名（匿名化处理）

- **部门经理**：
  - 可以查看部门内所有工程师的绩效
  - 可以查看团队绩效分析
  - 可以查看AI分析结果

- **管理员**：
  - 可以查看所有绩效数据
  - 可以配置绩效指标权重

---

## 🚀 实施计划

### Phase 1: 绩效计算（Sprint 4）

1. 绩效指标计算逻辑
2. 绩效数据统计
3. 绩效看板页面
4. 绩效排名功能

### Phase 2: AI分析（Sprint 5）

1. AI分析服务
2. 每日/每周总结生成
3. 人员安排建议
4. AI分析结果展示

### Phase 4: 高级功能（Sprint 6）

1. 绩效趋势分析
2. 个性化改进建议
3. 绩效报告生成
4. 数据导出功能

---

## 🔗 相关文档

- [绩效指标体系设计](./PERFORMANCE_METRICS_DESIGN.md) - **详细的绩效指标定义和计算方法**
- [工作日志必要性分析](./WORK_LOG_ANALYSIS.md) - **为什么不需要工作日志**
- [系统架构v2.0](./ARCHITECTURE_V2.md)
- [工单管理系统](../.github/issues/sprint-1/002-工单创建和提交.md)
- [统计看板](../.github/issues/sprint-2/011-统计看板.md)

---

**最后更新**：2025-12-22

