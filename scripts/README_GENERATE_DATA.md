# 生成测试数据说明

## 概述

`generate-comprehensive-test-data.sql` 脚本用于生成符合非标自动化行业特点的全面测试数据，包括：

- **客户**：5个不同行业的客户（华为、比亚迪、宁德时代、三一重工、美的）
- **项目**：每个客户2-3个项目，共10-15个项目
- **设备**：每个项目3-5台设备，共30-75台设备
- **工单**：过去90天，每天5-15个工单，共约900-1350个工单

## 数据特点

### 问题域分布
- **A（机械/动作）**：20%
- **B（电气/IO）**：25%
- **C（PLC/程序）**：20%
- **D（测试/判定）**：25%
- **E（系统/环境）**：10%

### 工单状态分布
- **Closed（已关闭）**：60%
- **Verifying（验证中）**：15%
- **SolutionIssued（方案已发布）**：10%
- **Triage（分诊中）**：10%
- **Submitted（已提交）**：5%

### 优先级分布
- **P1（高优先级）**：10%
- **P2（中高优先级）**：40%
- **P3（中优先级）**：40%
- **P4（低优先级）**：10%

### 时间分布
- 工单创建时间：过去90天内随机分布
- 已关闭工单的关闭时间：创建后1-30天随机分布

## 执行方法

### 方法1：通过Docker执行（推荐）

```bash
# 确保数据库容器正在运行
docker compose -p fieldticket ps

# 将脚本复制到容器中并执行
docker compose -p fieldticket cp scripts/generate-comprehensive-test-data.sql db:/tmp/
docker compose -p fieldticket exec db psql -U fieldticket_user -d fieldticket_db -f /tmp/generate-comprehensive-test-data.sql
```

### 方法2：直接连接数据库执行

```bash
# 如果数据库在本地运行
psql -h localhost -U fieldticket_user -d fieldticket_db -f scripts/generate-comprehensive-test-data.sql

# 或者通过docker exec直接执行
docker compose -p fieldticket exec -T db psql -U fieldticket_user -d fieldticket_db < scripts/generate-comprehensive-test-data.sql
```

### 方法3：通过数据库管理工具

1. 打开数据库管理工具（如pgAdmin、DBeaver等）
2. 连接到数据库
3. 打开 `scripts/generate-comprehensive-test-data.sql` 文件
4. 执行脚本

## 验证数据

执行脚本后，可以运行以下SQL验证数据：

```sql
-- 查看数据统计
SELECT 
    '客户数' as type, COUNT(*)::TEXT as count FROM customers
UNION ALL
SELECT '项目数', COUNT(*)::TEXT FROM projects
UNION ALL
SELECT '设备数', COUNT(*)::TEXT FROM devices
UNION ALL
SELECT '工单总数', COUNT(*)::TEXT FROM tickets
UNION ALL
SELECT '已关闭工单', COUNT(*)::TEXT FROM tickets WHERE status = 'Closed'
UNION ALL
SELECT '开放工单', COUNT(*)::TEXT FROM tickets WHERE status != 'Closed' AND status != 'Draft';

-- 查看问题域分布
SELECT domain, COUNT(*) as count, 
       ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM tickets), 2) as percentage
FROM tickets
GROUP BY domain
ORDER BY domain;

-- 查看状态分布
SELECT status, COUNT(*) as count,
       ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM tickets), 2) as percentage
FROM tickets
GROUP BY status
ORDER BY status;

-- 查看时间分布
SELECT DATE(created_at) as date, COUNT(*) as count
FROM tickets
GROUP BY DATE(created_at)
ORDER BY date DESC
LIMIT 10;
```

## 注意事项

1. **数据覆盖**：脚本会检查现有数据，如果已有足够的客户、项目、设备，会复用现有数据
2. **工单生成**：每次执行都会生成新的工单，如果多次执行会产生重复数据
3. **性能**：生成900+工单可能需要几分钟时间，请耐心等待
4. **清理数据**：如果需要重新生成，可以先清理现有工单：
   ```sql
   DELETE FROM tickets;
   ```

## 生成后的效果

执行脚本后，统计分析页面应该能够显示：

- ✅ **Top问题域统计**：显示各问题域的工单数量和占比
- ✅ **工单状态统计**：显示各状态的工单数量和占比
- ✅ **闭环时间分布**：显示不同时间段的闭环工单分布
- ✅ **工单数趋势**：显示过去90天的工单数量趋势

## 故障排查

如果执行失败，请检查：

1. 数据库连接是否正常
2. 用户权限是否足够
3. 表结构是否正确（需要先执行数据库迁移脚本）
4. 是否有足够的工程师用户（脚本需要至少1个工程师用户）

如果遇到问题，可以查看脚本输出的NOTICE信息来定位问题。

