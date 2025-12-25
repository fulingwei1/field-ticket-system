# 快速测试指南

> **日期**：2025-12-22  
> **快速开始测试绩效管理系统和 AI 分析功能**

---

## 🚀 快速开始（5分钟）

### 步骤 1: 启动服务

```bash
# 1. 启动后端服务
cd backend/src/FieldTicket.Api
dotnet run

# 2. 启动前端服务（新终端）
cd web-admin
npm run dev
```

### 步骤 2: 获取 Token

1. 打开浏览器访问前端：`http://localhost:5173`
2. 使用企业微信登录
3. 打开浏览器开发者工具（F12）
4. 在 Console 中执行：
   ```javascript
   localStorage.getItem('field_ticket_token')
   ```
5. 复制返回的 Token

### 步骤 3: 运行 API 测试

```bash
# 使用测试脚本
./scripts/test-performance-api.sh http://localhost:5000 YOUR_TOKEN_HERE
```

---

## 📋 手动测试清单

### ✅ Phase 1: 绩效计算

#### 1. 计算绩效（需要管理员权限）

```bash
# 替换 YOUR_TOKEN 和 ENGINEER_ID
curl -X POST http://localhost:5000/api/performance/calculate \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "engineerId": "ENGINEER_ID",
    "periodType": "monthly",
    "periodStart": "2025-12-01",
    "periodEnd": "2025-12-31"
  }'
```

**预期**：返回计算后的绩效数据

#### 2. 查看个人绩效

在前端：
- 登录系统
- 点击"绩效管理" → "我的绩效"
- 选择周期和日期
- 查看绩效数据

**预期**：显示综合评分、排名、各项指标

#### 3. 查看团队绩效（需要经理权限）

在前端：
- 使用经理账号登录
- 点击"绩效管理" → "团队绩效"
- 查看团队绩效数据

**预期**：显示团队概览、排名、成员对比

---

### ✅ Phase 2: AI 分析

#### 1. 生成每日总结

```bash
curl -X POST http://localhost:5000/api/ai-analysis/daily-summary \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "engineerId": "ENGINEER_ID",
    "analysisDate": "2025-12-22"
  }'
```

**预期**：返回生成的每日总结

#### 2. 查看 AI 分析结果

在前端：
- 点击"AI分析"
- 查看分析结果列表
- 点击"查看详情"查看完整内容

**预期**：显示分析结果，包含摘要、洞察、建议

---

## 🔍 验证要点

### 绩效计算验证

- [ ] 可以成功计算绩效
- [ ] 综合评分在 0-100 之间
- [ ] 绩效等级正确（excellent/good/average/below_average/poor）
- [ ] 排名数据正确
- [ ] 各项指标有数值

### AI 分析验证

- [ ] 可以成功生成分析结果
- [ ] 工作摘要不为空
- [ ] 关键洞察包含数据
- [ ] 建议包含内容
- [ ] 分析结果可以查看

### 前端验证

- [ ] 页面可以正常加载
- [ ] 数据可以正常显示
- [ ] 筛选功能正常
- [ ] 周期选择器正常
- [ ] 导航菜单正常

---

## 🐛 常见问题

### Q: 绩效计算返回空数据？

**A**: 检查该周期是否有工单数据：
```sql
SELECT COUNT(*) FROM tickets 
WHERE created_by_user_id = 'ENGINEER_ID' 
  AND created_at >= '2025-12-01' 
  AND created_at <= '2025-12-31';
```

### Q: 前端显示"暂无绩效数据"？

**A**: 
1. 先触发绩效计算
2. 检查周期和日期选择是否正确
3. 检查 API 是否返回数据

### Q: AI 分析生成失败？

**A**: 
1. 确保先计算了绩效数据
2. 确保有足够的工单数据
3. 检查日志查看具体错误

---

## 📊 测试数据准备

如果需要测试数据，可以执行以下 SQL：

```sql
-- 创建测试用户（如果还没有）
INSERT INTO users (id, corp_id, wecom_userid, name, role, is_active, created_at, updated_at)
VALUES 
  (gen_random_uuid(), 'test_corp', 'test_user_1', '测试工程师', 'FieldEngineer', true, NOW(), NOW()),
  (gen_random_uuid(), 'test_corp', 'test_user_2', '测试经理', 'Manager', true, NOW(), NOW());

-- 创建测试工单（替换 user_id）
INSERT INTO tickets (ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id, 
                     domain, step_code, symptom_title, sw_version, plc_version, param_version,
                     status, priority, created_at, updated_at, submitted_at)
SELECT 
  gen_random_uuid(),
  'T' || to_char(now(), 'YYYYMMDD') || LPAD(ROW_NUMBER() OVER()::text, 4, '0'),
  gen_random_uuid(),
  gen_random_uuid(),
  gen_random_uuid(),
  'USER_ID_HERE',  -- 替换为实际用户ID
  'A',
  'STEP001',
  '测试问题 ' || ROW_NUMBER() OVER(),
  'v1.0',
  'v1.0',
  'v1.0',
  CASE WHEN ROW_NUMBER() OVER() % 3 = 0 THEN 'Closed' ELSE 'Submitted' END,
  'P3',
  NOW() - (ROW_NUMBER() OVER() || ' days')::interval,
  NOW() - (ROW_NUMBER() OVER() || ' days')::interval,
  CASE WHEN ROW_NUMBER() OVER() % 3 = 0 THEN NOW() - (ROW_NUMBER() OVER() || ' days')::interval ELSE NULL END
FROM generate_series(1, 20);
```

---

## 📝 测试报告

测试完成后，记录以下信息：

- 测试日期：___________
- 测试人员：___________
- 测试环境：___________

**测试结果**：
- Phase 1 绩效计算：✅ / ❌
- Phase 2 AI 分析：✅ / ❌
- 前端功能：✅ / ❌

**发现的问题**：
1. _________________________________
2. _________________________________

---

**详细测试指南**：请参考 [PERFORMANCE_SYSTEM_TESTING.md](./PERFORMANCE_SYSTEM_TESTING.md)


