# 数据库迁移说明

## 绩效管理系统数据库迁移

### 方法一：使用 Entity Framework Core 迁移（推荐）

```bash
# 进入 Infrastructure 项目目录
cd backend/src/FieldTicket.Infrastructure

# 创建迁移
dotnet ef migrations add AddPerformanceTables --startup-project ../FieldTicket.Api

# 应用迁移
dotnet ef database update --startup-project ../FieldTicket.Api
```

### 方法二：直接执行 SQL 脚本

如果不想使用 EF Core 迁移，可以直接执行 SQL 脚本：

```bash
# 使用 psql 执行
psql -U your_username -d your_database -f AddPerformanceTables.sql

# 或者使用其他 PostgreSQL 客户端工具执行脚本
```

### 验证迁移

执行以下 SQL 查询验证表是否创建成功：

```sql
-- 检查表是否存在
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name IN ('performance_metrics', 'ai_analysis_results');

-- 检查索引是否创建
SELECT indexname 
FROM pg_indexes 
WHERE tablename IN ('performance_metrics', 'ai_analysis_results');
```

### 回滚迁移（如果需要）

如果使用 EF Core 迁移，可以回滚：

```bash
# 回滚到上一个迁移
dotnet ef database update PreviousMigrationName --startup-project ../FieldTicket.Api

# 删除迁移（如果还没有应用到数据库）
dotnet ef migrations remove --startup-project ../FieldTicket.Api
```

如果直接执行 SQL，需要手动删除表：

```sql
DROP TABLE IF EXISTS ai_analysis_results;
DROP TABLE IF EXISTS performance_metrics;
```

## 表结构说明

### performance_metrics（绩效指标表）

存储按周期统计的工程师绩效指标，包括：
- 工单处理效率指标
- 问题解决能力指标
- 技术诊断能力指标
- 知识贡献指标
- 客户服务能力指标
- 协作能力指标
- 工作规范性指标
- 综合评分和排名

### ai_analysis_results（AI分析结果表）

存储 AI 生成的绩效分析结果，包括：
- 每日/每周工作总结
- 团队分析
- 人员安排建议
- 绩效改进建议

## 注意事项

1. **外键约束**：`performance_metrics.engineer_id` 和 `ai_analysis_results.engineer_id` 都引用 `users.id`
2. **唯一约束**：每个工程师在每个周期的绩效数据是唯一的（`engineer_id`, `period_type`, `period_start`）
3. **索引**：已创建必要的索引以优化查询性能
4. **JSONB 字段**：`ai_analysis_results` 表中的 `key_insights`、`suggestions`、`performance_analysis` 使用 JSONB 类型，支持高效的 JSON 查询

## 后续步骤

迁移完成后，可以：

1. 通过 API 手动触发绩效计算：`POST /api/performance/calculate`
2. 查看绩效数据：`GET /api/performance/metrics`
3. 在前端访问绩效页面：`/performance/my` 和 `/performance/team`


