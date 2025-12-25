# 绩效管理系统测试指南

> **日期**：2025-12-22  
> **范围**：Phase 1（绩效计算）和 Phase 2（AI分析）

---

## 📋 测试前准备

### 1. 环境准备

#### 启动后端服务

```bash
cd backend/src/FieldTicket.Api
dotnet run
```

确保 API 服务运行在 `http://localhost:5000`

#### 启动前端服务

```bash
cd web-admin
npm install  # 如果还没有安装依赖
npm run dev
```

确保前端服务运行在 `http://localhost:5173`（或配置的端口）

#### 数据库迁移

确保已执行数据库迁移，创建绩效相关表：

```bash
# 方法一：使用 EF Core 迁移
cd backend/src/FieldTicket.Infrastructure
dotnet ef migrations add AddPerformanceTables --startup-project ../FieldTicket.Api
dotnet ef database update --startup-project ../FieldTicket.Api

# 方法二：直接执行 SQL
psql -U your_username -d your_database -f backend/migrations/AddPerformanceTables.sql
```

### 2. 准备测试数据

确保数据库中有：
- 至少 1 个用户（工程师）
- 至少 1 个部门经理或管理员用户
- 至少 10 个工单（用于计算绩效）

---

## 🧪 测试用例

### Phase 1: 绩效计算功能测试

#### 测试 1.1: 手动触发绩效计算

**目标**：测试绩效计算 API

**步骤**：

1. 获取管理员 Token（通过企业微信登录）
2. 调用绩效计算 API

**API 调用**：

```bash
# 替换 {engineerId} 为实际工程师ID
# 替换 {token} 为管理员 JWT Token

curl -X POST http://localhost:5000/api/performance/calculate \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "engineerId": "{engineerId}",
    "periodType": "monthly",
    "periodStart": "2025-12-01",
    "periodEnd": "2025-12-31"
  }'
```

**预期结果**：
- HTTP 200 响应
- 返回计算后的绩效指标
- 包含综合评分、绩效等级、排名等信息

**验证点**：
- ✅ 响应状态码为 200
- ✅ 返回数据包含 `overallScore`
- ✅ 返回数据包含 `performanceLevel`
- ✅ 返回数据包含各项指标值

---

#### 测试 1.2: 获取工程师绩效

**目标**：测试获取个人绩效 API

**步骤**：

1. 使用工程师账号登录
2. 调用获取绩效 API

**API 调用**：

```bash
# 替换 {engineerId} 为工程师ID
# 替换 {token} 为工程师 JWT Token

curl -X GET "http://localhost:5000/api/performance/engineer/{engineerId}?periodType=monthly&periodStart=2025-12-01" \
  -H "Authorization: Bearer {token}"
```

**预期结果**：
- HTTP 200 响应
- 返回工程师的绩效数据

**验证点**：
- ✅ 工程师可以查看自己的绩效
- ✅ 返回数据包含所有绩效指标
- ✅ 数据格式正确

---

#### 测试 1.3: 获取团队绩效

**目标**：测试获取团队绩效 API

**步骤**：

1. 使用部门经理或管理员账号登录
2. 调用获取团队绩效 API

**API 调用**：

```bash
# 替换 {token} 为经理/管理员 JWT Token

curl -X GET "http://localhost:5000/api/performance/team?periodType=monthly&periodStart=2025-12-01" \
  -H "Authorization: Bearer {token}"
```

**预期结果**：
- HTTP 200 响应
- 返回团队所有成员的绩效数据

**验证点**：
- ✅ 经理可以查看团队绩效
- ✅ 返回多个成员的绩效数据
- ✅ 数据按综合评分排序

---

#### 测试 1.4: 获取绩效排名

**目标**：测试获取绩效排名 API

**步骤**：

1. 使用任意账号登录
2. 调用获取排名 API

**API 调用**：

```bash
curl -X GET "http://localhost:5000/api/performance/ranking?periodType=monthly&periodStart=2025-12-01" \
  -H "Authorization: Bearer {token}"
```

**预期结果**：
- HTTP 200 响应
- 返回排名列表，按综合评分降序排列

**验证点**：
- ✅ 返回排名数据
- ✅ 排名从 1 开始
- ✅ 包含工程师姓名、评分、等级等信息

---

#### 测试 1.5: 获取绩效趋势

**目标**：测试获取绩效趋势 API

**步骤**：

1. 使用工程师账号登录
2. 调用获取趋势 API

**API 调用**：

```bash
curl -X GET "http://localhost:5000/api/performance/trends?engineerId={engineerId}&periodType=monthly&fromDate=2025-09-01&toDate=2025-12-31" \
  -H "Authorization: Bearer {token}"
```

**预期结果**：
- HTTP 200 响应
- 返回多个周期的绩效趋势数据

**验证点**：
- ✅ 返回趋势数据数组
- ✅ 数据按时间顺序排列
- ✅ 包含每个周期的综合评分

---

#### 测试 1.6: 权限控制测试

**目标**：验证权限控制是否正确

**测试场景**：

1. **工程师查看他人绩效**（应返回 403）
   ```bash
   # 使用工程师A的Token查看工程师B的绩效
   curl -X GET "http://localhost:5000/api/performance/engineer/{engineerB_id}?periodType=monthly&periodStart=2025-12-01" \
     -H "Authorization: Bearer {engineerA_token}"
   ```
   - 预期：HTTP 403 Forbidden

2. **工程师查看团队绩效**（应返回 403）
   ```bash
   curl -X GET "http://localhost:5000/api/performance/team?periodType=monthly&periodStart=2025-12-01" \
     -H "Authorization: Bearer {engineer_token}"
   ```
   - 预期：HTTP 403 Forbidden

3. **非管理员触发绩效计算**（应返回 403）
   ```bash
   curl -X POST http://localhost:5000/api/performance/calculate \
     -H "Authorization: Bearer {engineer_token}" \
     -H "Content-Type: application/json" \
     -d '{...}'
   ```
   - 预期：HTTP 403 Forbidden

---

### Phase 2: AI 分析功能测试

#### 测试 2.1: 生成每日工作总结

**目标**：测试生成每日总结 API

**步骤**：

1. 使用工程师账号登录
2. 调用生成每日总结 API

**API 调用**：

```bash
curl -X POST http://localhost:5000/api/ai-analysis/daily-summary \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "engineerId": "{engineerId}",
    "analysisDate": "2025-12-22"
  }'
```

**预期结果**：
- HTTP 200 响应
- 返回生成的每日总结
- 包含工作摘要、关键洞察、建议

**验证点**：
- ✅ 成功生成分析结果
- ✅ 包含 `summary` 字段
- ✅ 包含 `keyInsights` 字段
- ✅ 包含 `suggestions` 字段

---

#### 测试 2.2: 生成每周总结

**目标**：测试生成每周总结 API

**API 调用**：

```bash
curl -X POST http://localhost:5000/api/ai-analysis/weekly-summary \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "engineerId": "{engineerId}",
    "weekStart": "2025-12-15"
  }'
```

**预期结果**：
- HTTP 200 响应
- 返回生成的每周总结

**验证点**：
- ✅ 成功生成分析结果
- ✅ 分析类型为 `weekly_summary`
- ✅ 包含周度绩效数据

---

#### 测试 2.3: 生成团队分析

**目标**：测试生成团队分析 API

**步骤**：

1. 使用部门经理或管理员账号登录
2. 调用生成团队分析 API

**API 调用**：

```bash
curl -X POST http://localhost:5000/api/ai-analysis/team-analysis \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "departmentId": "{departmentId}",
    "analysisDate": "2025-12-22",
    "periodType": "monthly"
  }'
```

**预期结果**：
- HTTP 200 响应
- 返回生成的团队分析

**验证点**：
- ✅ 成功生成分析结果
- ✅ 分析类型为 `team_analysis`
- ✅ 包含团队绩效数据
- ✅ 包含工作负荷分布分析

---

#### 测试 2.4: 生成人员安排建议

**目标**：测试生成人员安排建议 API

**步骤**：

1. 使用部门经理或管理员账号登录
2. 调用生成人员安排建议 API

**API 调用**：

```bash
curl -X POST http://localhost:5000/api/ai-analysis/scheduling-suggestion \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "departmentId": "{departmentId}",
    "analysisDate": "2025-12-22"
  }'
```

**预期结果**：
- HTTP 200 响应
- 返回生成的人员安排建议

**验证点**：
- ✅ 成功生成分析结果
- ✅ 分析类型为 `scheduling_suggestion`
- ✅ 包含人员分配建议
- ✅ 包含工作负荷平衡建议

---

#### 测试 2.5: 获取分析结果列表

**目标**：测试获取分析结果列表 API

**API 调用**：

```bash
curl -X GET "http://localhost:5000/api/ai-analysis/results?analysisType=daily_summary&page=1&pageSize=20" \
  -H "Authorization: Bearer {token}"
```

**预期结果**：
- HTTP 200 响应
- 返回分析结果列表

**验证点**：
- ✅ 返回分页数据
- ✅ 包含 `items` 和 `total` 字段
- ✅ 可以按类型筛选

---

#### 测试 2.6: 获取分析结果详情

**目标**：测试获取分析结果详情 API

**步骤**：

1. 先获取一个分析结果 ID（从列表 API）
2. 调用获取详情 API

**API 调用**：

```bash
curl -X GET "http://localhost:5000/api/ai-analysis/results/{analysisId}" \
  -H "Authorization: Bearer {token}"
```

**预期结果**：
- HTTP 200 响应
- 返回完整的分析结果详情

**验证点**：
- ✅ 返回完整的分析数据
- ✅ 包含所有字段（summary、keyInsights、suggestions 等）

---

## 🖥️ 前端功能测试

### 测试 3.1: 个人绩效看板

**步骤**：

1. 使用工程师账号登录前端
2. 点击侧边栏"绩效管理" → "我的绩效"
3. 选择周期类型（如"月度"）
4. 选择日期（如"2025-12"）
5. 查看绩效数据

**验证点**：
- ✅ 页面正常加载
- ✅ 显示综合评分卡片
- ✅ 显示团队排名
- ✅ 显示关键指标表格
- ✅ 显示多维度指标
- ✅ 周期选择器正常工作

---

### 测试 3.2: 团队绩效看板

**步骤**：

1. 使用部门经理或管理员账号登录
2. 点击侧边栏"绩效管理" → "团队绩效"
3. 选择周期类型和日期
4. 查看团队绩效数据

**验证点**：
- ✅ 页面正常加载
- ✅ 显示团队概览统计
- ✅ 显示绩效排名表格（带奖牌图标）
- ✅ 显示团队绩效详情对比
- ✅ 可以刷新数据

---

### 测试 3.3: AI 分析结果列表

**步骤**：

1. 登录系统
2. 点击侧边栏"AI分析"
3. 查看分析结果列表
4. 尝试筛选（按类型、日期）

**验证点**：
- ✅ 页面正常加载
- ✅ 显示分析结果列表
- ✅ 可以按类型筛选
- ✅ 可以按日期范围筛选
- ✅ 分页功能正常

---

### 测试 3.4: AI 分析结果详情

**步骤**：

1. 在 AI 分析结果列表中点击"查看详情"
2. 查看完整的分析结果

**验证点**：
- ✅ 页面正常加载
- ✅ 显示基本信息
- ✅ 显示工作摘要
- ✅ 显示关键洞察（JSON 格式正确）
- ✅ 显示建议（格式正确）
- ✅ 显示绩效分析（如果有）

---

## 🔍 端到端测试流程

### 完整测试流程

1. **准备数据**
   - 确保有工单数据
   - 确保有用户数据

2. **计算绩效**
   - 管理员触发绩效计算
   - 验证计算成功

3. **查看绩效**
   - 工程师查看个人绩效
   - 经理查看团队绩效

4. **生成 AI 分析**
   - 生成每日总结
   - 生成团队分析
   - 生成人员安排建议

5. **查看分析结果**
   - 在前端查看分析结果列表
   - 查看分析结果详情

---

## 🐛 常见问题排查

### 问题 1: 绩效计算返回空数据

**原因**：
- 该周期没有工单数据
- 工程师ID不正确

**解决方案**：
- 检查数据库中是否有该周期的工单
- 验证工程师ID是否正确

---

### 问题 2: 前端无法加载绩效数据

**原因**：
- API 地址配置错误
- Token 过期或无效
- CORS 配置问题

**解决方案**：
- 检查 `.env` 文件中的 `VITE_API_URL`
- 重新登录获取新 Token
- 检查后端 CORS 配置

---

### 问题 3: AI 分析生成失败

**原因**：
- 没有绩效数据
- 工单数据不足

**解决方案**：
- 先计算绩效数据
- 确保有足够的工单数据

---

## 📊 测试检查清单

### Phase 1: 绩效计算

- [ ] 可以手动触发绩效计算
- [ ] 可以查看个人绩效
- [ ] 可以查看团队绩效
- [ ] 可以查看绩效排名
- [ ] 可以查看绩效趋势
- [ ] 权限控制正确（工程师不能查看他人绩效）
- [ ] 权限控制正确（工程师不能查看团队绩效）
- [ ] 权限控制正确（非管理员不能触发计算）

### Phase 2: AI 分析

- [ ] 可以生成每日工作总结
- [ ] 可以生成每周总结
- [ ] 可以生成团队分析
- [ ] 可以生成人员安排建议
- [ ] 可以查看分析结果列表
- [ ] 可以查看分析结果详情
- [ ] 权限控制正确（工程师只能生成自己的总结）
- [ ] 权限控制正确（只有经理可以生成团队分析）

### 前端功能

- [ ] 个人绩效看板正常显示
- [ ] 团队绩效看板正常显示
- [ ] AI 分析结果列表正常显示
- [ ] AI 分析结果详情正常显示
- [ ] 导航菜单正常工作
- [ ] 周期选择器正常工作
- [ ] 筛选功能正常工作

---

## 📝 测试报告模板

测试完成后，请填写以下信息：

```
测试日期：2025-12-22
测试人员：[姓名]
测试环境：[开发/测试/生产]

Phase 1 测试结果：
- 绩效计算：✅/❌
- 个人绩效查看：✅/❌
- 团队绩效查看：✅/❌
- 绩效排名：✅/❌
- 绩效趋势：✅/❌
- 权限控制：✅/❌

Phase 2 测试结果：
- 每日总结生成：✅/❌
- 每周总结生成：✅/❌
- 团队分析生成：✅/❌
- 人员安排建议：✅/❌
- 分析结果查看：✅/❌

前端功能测试结果：
- 个人绩效看板：✅/❌
- 团队绩效看板：✅/❌
- AI 分析列表：✅/❌
- AI 分析详情：✅/❌

发现的问题：
1. [问题描述]
2. [问题描述]

建议：
1. [建议内容]
2. [建议内容]
```

---

**最后更新**：2025-12-22


