# 绩效管理系统测试检查清单

> **快速验证清单** - 用于快速检查功能是否正常

---

## ✅ 前置检查

- [ ] 后端服务已启动（`http://localhost:5000`）
- [ ] 前端服务已启动（`http://localhost:5173`）
- [ ] 数据库迁移已执行（绩效表已创建）
- [ ] 已有测试用户和工单数据
- [ ] 已获取 JWT Token

---

## 📊 Phase 1: 绩效计算功能

### API 测试

- [ ] **1.1** 手动触发绩效计算（管理员）
  - 端点：`POST /api/performance/calculate`
  - 预期：HTTP 200，返回绩效数据

- [ ] **1.2** 获取工程师绩效
  - 端点：`GET /api/performance/engineer/{engineerId}`
  - 预期：HTTP 200，返回个人绩效数据

- [ ] **1.3** 获取团队绩效（经理/管理员）
  - 端点：`GET /api/performance/team`
  - 预期：HTTP 200，返回团队绩效列表

- [ ] **1.4** 获取绩效排名
  - 端点：`GET /api/performance/ranking`
  - 预期：HTTP 200，返回排名列表

- [ ] **1.5** 获取绩效趋势
  - 端点：`GET /api/performance/trends`
  - 预期：HTTP 200，返回趋势数据

- [ ] **1.6** 权限控制测试
  - 工程师查看他人绩效 → 预期：HTTP 403
  - 工程师查看团队绩效 → 预期：HTTP 403
  - 非管理员触发计算 → 预期：HTTP 403

### 前端测试

- [ ] **1.7** 个人绩效看板
  - 路径：`/performance/my`
  - 检查项：
    - [ ] 页面正常加载
    - [ ] 综合评分显示
    - [ ] 团队排名显示
    - [ ] 关键指标表格显示
    - [ ] 周期选择器工作正常
    - [ ] 日期选择器工作正常

- [ ] **1.8** 团队绩效看板（经理/管理员）
  - 路径：`/performance/team`
  - 检查项：
    - [ ] 页面正常加载
    - [ ] 团队概览统计显示
    - [ ] 绩效排名表格显示
    - [ ] 排名前三有奖牌图标
    - [ ] 团队绩效详情表格显示
    - [ ] 刷新按钮工作正常

---

## 🤖 Phase 2: AI 分析功能

### API 测试

- [ ] **2.1** 生成每日工作总结
  - 端点：`POST /api/ai-analysis/daily-summary`
  - 预期：HTTP 200，返回分析结果

- [ ] **2.2** 生成每周总结
  - 端点：`POST /api/ai-analysis/weekly-summary`
  - 预期：HTTP 200，返回分析结果

- [ ] **2.3** 生成团队分析（经理/管理员）
  - 端点：`POST /api/ai-analysis/team-analysis`
  - 预期：HTTP 200，返回分析结果

- [ ] **2.4** 生成人员安排建议（经理/管理员）
  - 端点：`POST /api/ai-analysis/scheduling-suggestion`
  - 预期：HTTP 200，返回分析结果

- [ ] **2.5** 获取分析结果列表
  - 端点：`GET /api/ai-analysis/results`
  - 预期：HTTP 200，返回分析结果列表

- [ ] **2.6** 获取分析结果详情
  - 端点：`GET /api/ai-analysis/results/{analysisId}`
  - 预期：HTTP 200，返回完整分析结果

- [ ] **2.7** 权限控制测试
  - 工程师生成他人总结 → 预期：HTTP 403
  - 工程师生成团队分析 → 预期：HTTP 403

### 前端测试

- [ ] **2.8** AI 分析结果列表
  - 路径：`/ai-analysis`
  - 检查项：
    - [ ] 页面正常加载
    - [ ] 分析结果列表显示
    - [ ] 可以按类型筛选
    - [ ] 可以按日期范围筛选
    - [ ] 分页功能正常
    - [ ] "查看详情"按钮工作正常

- [ ] **2.9** AI 分析结果详情
  - 路径：`/ai-analysis/{analysisId}`
  - 检查项：
    - [ ] 页面正常加载
    - [ ] 基本信息显示
    - [ ] 工作摘要显示
    - [ ] 关键洞察显示（JSON 格式正确）
    - [ ] 建议显示（格式正确）
    - [ ] 绩效分析显示（如果有）
    - [ ] "返回列表"按钮工作正常

---

## 🎨 UI/UX 测试

- [ ] **3.1** 导航菜单
  - [ ] "绩效管理"菜单项显示
  - [ ] "AI分析"菜单项显示
  - [ ] 菜单点击跳转正常
  - [ ] 当前页面高亮显示

- [ ] **3.2** 响应式布局
  - [ ] 侧边栏可以折叠/展开
  - [ ] 移动端显示正常（如果支持）

- [ ] **3.3** 加载状态
  - [ ] 数据加载时显示 Loading
  - [ ] 加载完成后 Loading 消失

- [ ] **3.4** 错误处理
  - [ ] 无数据时显示友好提示
  - [ ] 错误时显示错误消息
  - [ ] 权限不足时显示提示

---

## 🔍 数据验证

### 绩效数据验证

- [ ] 综合评分在 0-100 范围内
- [ ] 绩效等级正确（excellent/good/average/below_average/poor）
- [ ] 排名从 1 开始，连续递增
- [ ] 各项指标有合理的数值
- [ ] 时间格式正确

### AI 分析数据验证

- [ ] 工作摘要不为空
- [ ] 关键洞察包含数据
- [ ] 建议包含内容
- [ ] JSON 数据格式正确
- [ ] 置信度在 0-1 范围内

---

## 🚀 快速测试命令

### 使用测试脚本

```bash
# 1. 获取 Token（在前端登录后，浏览器 Console 执行）
localStorage.getItem('field_ticket_token')

# 2. 运行测试脚本
./scripts/test-performance-api.sh http://localhost:5000 YOUR_TOKEN_HERE
```

### 手动测试（使用 curl）

```bash
# 设置变量
TOKEN="YOUR_TOKEN_HERE"
ENGINEER_ID="ENGINEER_ID_HERE"
API_URL="http://localhost:5000"

# 测试绩效计算
curl -X POST "$API_URL/api/performance/calculate" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"engineerId\":\"$ENGINEER_ID\",\"periodType\":\"monthly\",\"periodStart\":\"2025-12-01\",\"periodEnd\":\"2025-12-31\"}"

# 测试获取绩效
curl -X GET "$API_URL/api/performance/engineer/$ENGINEER_ID?periodType=monthly&periodStart=2025-12-01" \
  -H "Authorization: Bearer $TOKEN"

# 测试生成每日总结
curl -X POST "$API_URL/api/ai-analysis/daily-summary" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"engineerId\":\"$ENGINEER_ID\",\"analysisDate\":\"2025-12-22\"}"
```

---

## 📝 测试记录

**测试日期**：___________

**测试人员**：___________

**测试环境**：___________

**测试结果**：

| 功能模块 | 状态 | 备注 |
|---------|------|------|
| Phase 1: 绩效计算 | ✅/❌ | |
| Phase 2: AI 分析 | ✅/❌ | |
| 前端功能 | ✅/❌ | |
| 权限控制 | ✅/❌ | |

**发现的问题**：

1. _________________________________
2. _________________________________
3. _________________________________

**建议**：

1. _________________________________
2. _________________________________

---

## 📚 相关文档

- [快速测试指南](./QUICK_TEST_GUIDE.md) - 5分钟快速开始
- [详细测试指南](./PERFORMANCE_SYSTEM_TESTING.md) - 完整测试文档
- [实施总结](./PERFORMANCE_SYSTEM_IMPLEMENTATION.md) - 功能说明
- [AI 分析实施总结](./AI_ANALYSIS_IMPLEMENTATION.md) - AI 功能说明

---

**最后更新**：2025-12-22


