# Issue #047: Excel批量导入 - 快速开始指南

## 🚀 快速开始

### 1. 数据库迁移

执行数据库迁移脚本：

```bash
# 在PostgreSQL中执行
psql -U your_user -d your_database -f backend/migrations/20250124_add_project_field_problem_tables.sql
```

或者使用EF Core迁移：

```bash
cd backend/src/FieldTicket.Infrastructure
dotnet ef migrations add AddProjectFieldProblemTables
dotnet ef database update
```

### 2. 后端配置

确保已安装EPPlus包：

```bash
cd backend/src/FieldTicket.Infrastructure
dotnet add package EPPlus --version 7.0.0
```

服务已自动注册在 `Program.cs` 中。

### 3. 前端配置

前端页面已创建在 `web-admin/src/pages/projects/ExcelImport.tsx`。

路由已配置在 `web-admin/src/routes.tsx`：
- `/projects/excel-import`

导航菜单已添加在 `web-admin/src/components/AppLayout.tsx`：
- 项目管理 > Excel导入

### 4. 使用流程

1. **访问导入页面**
   - 登录系统
   - 导航到"项目管理" > "Excel导入"

2. **上传Excel文件**
   - 点击"选择Excel文件"
   - 选择要导入的Excel文件（.xlsx或.xls格式）

3. **预览数据**
   - 系统自动解析Excel文件
   - 显示解析结果（项目列表、问题列表、错误信息）

4. **验证数据**
   - 点击"验证数据"按钮
   - 系统检查数据完整性和正确性
   - 显示验证结果

5. **执行导入**
   - 验证通过后，点击"执行导入"按钮
   - 系统将数据导入到数据库
   - 显示导入结果统计

## 📋 Excel模板格式

### 支持的列名（中英文均可）

**项目信息**：
- 项目号 / ProjectNo / 项目号(PJ)
- 项目名称 / ProjectName / 项目名
- 客户名称 / CustomerName / 客户名
- 设备类型 / DeviceType
- 行业类型 / IndustryType / 行业
- 销售金额 / SalesAmount
- 下单日期 / OrderDate
- 要求交货日期 / RequiredDeliveryDate / 交货日期
- 实际交货日期 / ActualDeliveryDate
- 项目状态 / ProjectStatus / 状态
- 项目经理 / ProjectManager / 负责人

**问题信息**：
- 问题序号 / ProblemSequence / 序号
- 问题分类 / ProblemCategory / 分类
- 问题描述 / ProblemDescription / 描述
- 优先级 / Priority
- 发现日期 / FoundDate / 日期
- 处理完成日期 / CompletedDate / 完成日期
- 主负责部门 / PrimaryDepartment / 负责部门 / 部门
- 主负责人 / PrimaryResponsible / 负责人
- 协作部门 / CollaboratingDepartment
- 协作人员 / CollaboratingPerson
- 处理状态 / Status / 状态
- 处理方案 / Solution / 方案
- 验证状态 / VerificationStatus
- 客户反馈 / CustomerFeedback / 反馈
- 满意度评分 / SatisfactionScore / 满意度
- 验证人 / VerifiedBy
- 关联工单号 / RelatedTicketNo / 工单号
- 备注 / Notes

### 日期格式支持

系统支持多种日期格式：
- `2025.9` → `2025-09-01`（默认月初）
- `2025.10.11` → `2025-10-11`
- `2025-10-11` → `2025-10-11`
- `2025/10/11` → `2025-10-11`

### 数据验证规则

1. **必填字段**：
   - 项目号
   - 项目名称
   - 问题描述
   - 发现日期

2. **日期逻辑**：
   - 完成日期不能早于发现日期
   - 要求交货日期不能早于下单日期

3. **数值范围**：
   - 满意度评分必须在1-5之间

## 🔧 API端点

### 1. 解析Excel文件

```http
POST /api/excel-import/parse
Content-Type: multipart/form-data

file: <Excel文件>
```

**响应**：
```json
{
  "projects": [...],
  "errors": [...],
  "totalRows": 100,
  "successRows": 95,
  "errorRows": 5
}
```

### 2. 验证导入数据

```http
POST /api/excel-import/validate
Content-Type: application/json

{
  "projects": [...],
  "errors": [...],
  ...
}
```

**响应**：
```json
{
  "isValid": true,
  "errors": []
}
```

### 3. 执行导入

```http
POST /api/excel-import/execute
Content-Type: application/json

{
  "importResult": {
    "projects": [...],
    ...
  }
}
```

**响应**：
```json
{
  "success": true,
  "projectsCreated": 10,
  "projectsUpdated": 5,
  "problemsCreated": 50,
  "problemsUpdated": 20,
  "message": "导入成功"
}
```

## 📚 相关文档

- [Issue #047 详细设计](./ISSUE_047_DETAILED_DESIGN.md)
- [Issue #047 实施总结](./ISSUE_047_IMPLEMENTATION_SUMMARY.md)
- [Issue #047 数据对比分析](./ISSUE_047_DATA_COMPARISON_ANALYSIS.md)

## ⚠️ 注意事项

1. **客户ID处理**：当前实现中，如果找不到客户，会使用空GUID。建议后续完善客户查找或创建逻辑。

2. **字段映射**：系统支持多种列名变体，但如果Excel列名完全不匹配，可能无法正确解析。

3. **日期格式**：如果日期格式无法识别，该字段将为空。

4. **批量导入**：建议单次导入不超过1000行，避免超时。

## 🐛 常见问题

### Q: 解析失败，提示"Excel文件为空或格式不正确"
A: 检查Excel文件是否损坏，确保是有效的.xlsx或.xls文件。

### Q: 某些列无法识别
A: 检查列名是否与支持的列名匹配，或联系管理员添加新的列名映射。

### Q: 日期解析失败
A: 确保日期格式符合支持格式，或手动调整Excel中的日期格式。

### Q: 导入后找不到数据
A: 检查导入结果统计，确认是否有错误。检查数据库连接和权限。


















