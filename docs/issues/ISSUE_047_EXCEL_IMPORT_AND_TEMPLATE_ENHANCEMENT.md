# Issue #047: Excel批量导入和模板增强功能

## 📋 任务描述

基于"客服反馈问题模板"文件夹中的表格分析，实现Excel批量导入功能和模板标准化，使系统能够完美支持现场设备问题的批量录入和管理。

## 🎯 任务目标

1. 支持Excel批量导入现场设备问题
2. 统一模板格式，解决表头不一致问题
3. 支持项目-问题的一对多关系
4. 增强问题分类、验证反馈、满意度评分等字段
5. 实现根本原因分析功能
6. 关联知识库和历史问题

## 📊 现有模板问题分析

### 核心问题Top 5

| 问题 | 严重程度 | 影响 | 系统改进需求 |
|------|--------|------|------------|
| 表头不一致 | ⭐⭐⭐⭐⭐ | 高 | Excel导入时支持字段映射 |
| 日期格式混乱 | ⭐⭐⭐⭐⭐ | 高 | 日期格式自动识别和转换 |
| 问题组织不清 | ⭐⭐⭐⭐ | 高 | 支持项目-问题一对多关系 |
| 处理状态缺失 | ⭐⭐⭐ | 中 | 增强状态管理 |
| 验证反馈缺失 | ⭐⭐⭐ | 中 | 增加验证和满意度字段 |

### 详细问题清单

1. **表头不一致**：不同月份文件列数不同（19-23列），列序不同
2. **日期格式混乱**：`2025.9`、`2025.10.11`、`2025-10-11` 混用
3. **问题组织不清**：一个项目多个问题，但关系不明确
4. **处理状态缺失**：只有跟进日期，没有明确状态
5. **验证反馈缺失**：没有客户验证结果和满意度评分
6. **部门职责混乱**：主负责和协作部门不清晰
7. **缺少问题分类**：无法按类型统计分析
8. **无法追踪关联**：无法关联工单、知识库、历史问题

## 🔧 系统改进方案

### Phase 1: Excel批量导入功能（P1）

#### 1.1 Excel导入服务

**文件**：
- `backend/src/FieldTicket.Core/Services/IExcelImportService.cs`
- `backend/src/FieldTicket.Infrastructure/Services/ExcelImportService.cs`

**功能**：
- 支持多种Excel格式（.xlsx, .xls）
- 自动识别表头（支持中英文列名）
- 字段映射配置（支持不同模板格式）
- 数据验证和清洗
- 批量导入工单和问题

**字段映射配置**：
```csharp
public class ExcelFieldMapping
{
    public string ExcelColumn { get; set; }  // Excel列名（如"项目号"、"项目号(PJ)"）
    public string SystemField { get; set; }  // 系统字段（如"ProjectId"、"ProjectNo"）
    public FieldType FieldType { get; set; } // 字段类型
    public bool Required { get; set; }       // 是否必填
    public Func<string, object>? Converter { get; set; } // 转换函数
}
```

#### 1.2 日期格式自动识别

**功能**：
- 自动识别多种日期格式：
  - `2025.9` → `2025-09-01`（默认月初）
  - `2025.10.11` → `2025-10-11`
  - `2025-10-11` → `2025-10-11`
  - `2025/10/11` → `2025-10-11`
- 日期验证和错误提示

#### 1.3 项目-问题关系处理

**功能**：
- 自动识别项目主记录和问题子记录
- 通过项目号关联问题和项目
- 支持一个项目多个问题的导入
- 自动生成问题序号

**数据结构**：
```csharp
public class ProjectProblemImport
{
    public ProjectInfo Project { get; set; }
    public List<ProblemInfo> Problems { get; set; }
}

public class ProjectInfo
{
    public string ProjectNo { get; set; }      // 项目号
    public string ProjectName { get; set; }     // 项目名称
    public string CustomerName { get; set; }   // 客户名称
    public string DeviceType { get; set; }      // 设备类型
    public DateTime? OrderDate { get; set; }    // 下单日期
    public DateTime? RequiredDeliveryDate { get; set; } // 要求交货日期
    public DateTime? ActualDeliveryDate { get; set; }  // 实际交货日期
    public decimal? SalesAmount { get; set; }  // 销售金额
}

public class ProblemInfo
{
    public int ProblemSequence { get; set; }    // 问题序号
    public string ProblemCategory { get; set; } // 问题分类
    public string ProblemDescription { get; set; } // 问题描述
    public string Department { get; set; }      // 负责部门
    public string ResponsiblePerson { get; set; } // 负责人
    public string Status { get; set; }          // 处理状态
    public string Solution { get; set; }        // 处理方案
    public DateTime? FoundDate { get; set; }    // 发现日期
    public DateTime? CompletedDate { get; set; } // 完成日期
    public string VerificationStatus { get; set; } // 验证状态
    public string CustomerFeedback { get; set; } // 客户反馈
    public int? SatisfactionScore { get; set; }  // 满意度评分
}
```

### Phase 2: 模板标准化和字段增强（P1）

#### 2.1 标准模板定义

**文件**：
- `backend/src/FieldTicket.Core/Models/ExcelTemplateDefinition.cs`

**标准模板结构**：

**Sheet1: 项目主表**
| 列名 | 系统字段 | 类型 | 必填 | 说明 |
|------|---------|------|------|------|
| 项目号 | ProjectNo | String | 是 | 主键 |
| 项目名称 | ProjectName | String | 是 | |
| 客户名称 | CustomerName | String | 是 | |
| 设备类型 | DeviceType | String | 否 | 线体/单机/其他 |
| 行业类型 | IndustryType | String | 否 | 汽车/白电/3C等 |
| 销售金额 | SalesAmount | Decimal | 否 | |
| 下单日期 | OrderDate | Date | 否 | YYYY-MM-DD |
| 要求交货日期 | RequiredDeliveryDate | Date | 否 | YYYY-MM-DD |
| 实际交货日期 | ActualDeliveryDate | Date | 否 | YYYY-MM-DD |
| 交货延期天数 | DeliveryDelayDays | Int | 否 | 自动计算 |
| 项目状态 | ProjectStatus | String | 否 | 进行中/已交付/已验证 |

**Sheet2: 现场问题表**
| 列名 | 系统字段 | 类型 | 必填 | 说明 |
|------|---------|------|------|------|
| 问题ID | ProblemId | String | 否 | 自动生成 |
| 项目号 | ProjectNo | String | 是 | 外键 |
| 问题序号 | ProblemSequence | Int | 是 | 项目内序号 |
| 问题分类 | ProblemCategory | String | 是 | 硬件/软件/工艺/设计/安装/需求/配置/其他 |
| 问题描述 | ProblemDescription | String | 是 | |
| 发现日期 | FoundDate | Date | 是 | YYYY-MM-DD |
| 主负责部门 | PrimaryDepartment | String | 是 | |
| 主负责人 | PrimaryResponsible | String | 是 | |
| 协作部门 | CollaboratingDepartment | String | 否 | |
| 协作人员 | CollaboratingPerson | String | 否 | |
| 处理状态 | Status | String | 是 | 待分配/处理中/待验证/验证中/已验证/验证失败/已关闭 |
| 处理方案 | Solution | String | 否 | |
| 处理完成日期 | CompletedDate | Date | 否 | YYYY-MM-DD |
| 处理周期天数 | ProcessingDays | Int | 否 | 自动计算 |
| 验证状态 | VerificationStatus | String | 否 | 未验证/验证通过/验证失败 |
| 验证反馈 | CustomerFeedback | String | 否 | |
| 满意度评分 | SatisfactionScore | Int | 否 | 1-5分 |
| 关联工单号 | RelatedTicketNo | String | 否 | |
| 知识库ID | KnowledgeBaseId | String | 否 | |
| 是否重复问题 | IsRepeatProblem | Boolean | 否 | |
| 相关历史问题ID | RelatedHistoryProblemId | String | 否 | |
| 备注 | Notes | String | 否 | |

#### 2.2 问题分类标准化

**问题分类枚举**：
```csharp
public enum ProblemCategory
{
    HardwareFault,      // 硬件故障
    HardwareMissing,    // 硬件缺失
    SoftwareDefect,     // 软件缺陷
    ProcessIssue,       // 工艺问题
    DesignIssue,        // 设计问题
    InstallationIssue,  // 安装问题
    ConfigurationIssue, // 配置问题
    RequirementChange,  // 需求变更
    Other               // 其他
}
```

#### 2.3 处理状态标准化

**处理状态枚举**：
```csharp
public enum ProblemStatus
{
    PendingAssignment,  // 待分配
    InProgress,         // 处理中
    PendingVerification, // 待验证
    Verifying,          // 验证中
    Verified,           // 已验证
    VerificationFailed, // 验证失败
    Closed              // 已关闭
}
```

### Phase 3: 根本原因分析功能（P1）

#### 3.1 根本原因分析实体

**文件**：
- `backend/src/FieldTicket.Domain/Entities/RootCauseAnalysis.cs`

**字段**：
- ProblemId（问题ID）
- Why1-Why5（5Why分析）
- RootCause（根本原因）
- PreventiveMeasures（预防措施）
- VerificationMethod（验证方法）
- AnalyzedBy（分析人）
- AnalyzedAt（分析时间）

#### 3.2 根本原因分析服务

**文件**：
- `backend/src/FieldTicket.Core/Services/IRootCauseAnalysisService.cs`
- `backend/src/FieldTicket.Infrastructure/Services/RootCauseAnalysisService.cs`

**功能**：
- 创建和更新根本原因分析
- 5Why分析模板
- 预防措施建议
- 关联历史问题和知识库

### Phase 4: 验证和满意度管理（P1）

#### 4.1 验证记录增强

**现有功能增强**：
- 在 `Verification` 实体中增加：
  - CustomerFeedback（客户反馈）
  - SatisfactionScore（满意度评分 1-5）
  - VerifiedBy（验证人）
  - VerificationMethod（验证方法）

#### 4.2 满意度统计

**功能**：
- 按项目统计平均满意度
- 按部门统计平均满意度
- 按问题分类统计平均满意度
- 满意度趋势分析

### Phase 5: 项目-问题关联管理（P1）

#### 5.1 项目实体扩展

**文件**：
- `backend/src/FieldTicket.Domain/Entities/Project.cs`（新建）

**字段**：
- ProjectId（项目ID）
- ProjectNo（项目号）
- ProjectName（项目名称）
- CustomerId（客户ID）
- DeviceType（设备类型）
- IndustryType（行业类型）
- SalesAmount（销售金额）
- OrderDate（下单日期）
- RequiredDeliveryDate（要求交货日期）
- ActualDeliveryDate（实际交货日期）
- DeliveryDelayDays（交货延期天数）
- ProjectStatus（项目状态）

#### 5.2 问题实体扩展

**文件**：
- `backend/src/FieldTicket.Domain/Entities/FieldProblem.cs`（新建）

**字段**：
- ProblemId（问题ID）
- ProjectId（项目ID，外键）
- ProblemSequence（问题序号）
- ProblemCategory（问题分类）
- ProblemDescription（问题描述）
- PrimaryDepartment（主负责部门）
- PrimaryResponsible（主负责人）
- CollaboratingDepartment（协作部门）
- CollaboratingPerson（协作人员）
- Status（处理状态）
- Solution（处理方案）
- FoundDate（发现日期）
- CompletedDate（完成日期）
- ProcessingDays（处理周期天数）
- VerificationStatus（验证状态）
- CustomerFeedback（客户反馈）
- SatisfactionScore（满意度评分）
- RelatedTicketId（关联工单ID）
- KnowledgeBaseId（知识库ID）
- IsRepeatProblem（是否重复问题）
- RelatedHistoryProblemId（相关历史问题ID）

#### 5.3 项目-问题关联服务

**文件**：
- `backend/src/FieldTicket.Core/Services/IProjectProblemService.cs`
- `backend/src/FieldTicket.Infrastructure/Services/ProjectProblemService.cs`

**功能**：
- 创建项目和问题
- 查询项目下的所有问题
- 项目问题统计（问题总数、处理中、已完成等）
- 项目健康度评估

### Phase 6: 前端功能增强（P1）

#### 6.1 Excel导入页面

**文件**：
- `web-admin/src/pages/projects/ExcelImport.tsx`

**功能**：
- Excel文件上传
- 字段映射配置
- 数据预览和验证
- 导入结果展示
- 错误处理和提示

#### 6.2 项目-问题管理页面

**文件**：
- `web-admin/src/pages/projects/ProjectList.tsx`
- `web-admin/src/pages/projects/ProjectDetail.tsx`

**功能**：
- 项目列表展示
- 项目详情（包含问题列表）
- 问题创建和编辑
- 根本原因分析
- 验证和满意度评分

#### 6.3 问题分类统计

**文件**：
- `web-admin/src/pages/projects/ProblemStatistics.tsx`

**功能**：
- 按问题分类统计
- 按部门统计
- 按客户统计
- 处理周期分析
- 满意度分析

## 📐 技术实现细节

### Excel导入技术栈

**后端**：
- `EPPlus` 或 `ClosedXML`（.NET Excel处理库）
- `NPOI`（跨平台Excel处理）

**前端**：
- `xlsx` 或 `exceljs`（JavaScript Excel处理库）
- 文件上传组件

### 数据验证规则

1. **必填字段验证**：项目号、问题描述、发现日期等
2. **格式验证**：日期格式、数字格式、枚举值
3. **业务规则验证**：完成日期不能早于发现日期、满意度评分1-5
4. **关联验证**：项目号必须存在、负责人必须存在

### 错误处理

1. **导入错误分类**：
   - 格式错误（日期格式不对）
   - 数据错误（必填字段缺失）
   - 业务错误（项目号不存在）
   - 关联错误（负责人不存在）

2. **错误报告**：
   - 行号定位
   - 错误类型
   - 错误描述
   - 修复建议

## ✅ 验收标准

- [ ] 支持Excel批量导入（.xlsx格式）
- [ ] 自动识别和映射字段（支持不同模板格式）
- [ ] 日期格式自动转换（支持多种格式）
- [ ] 支持项目-问题一对多关系导入
- [ ] 数据验证和错误提示
- [ ] 根本原因分析功能
- [ ] 验证和满意度评分功能
- [ ] 项目-问题关联管理
- [ ] 问题分类统计和分析
- [ ] 前端Excel导入界面

## 🔗 相关文档

- [现场设备问题模板分析和改进建议](../../客服反馈问题模板/files/现场设备问题模板分析和改进建议.md)
- [现场问题管理快速改进行动清单](../../客服反馈问题模板/files/现场问题管理_快速改进行动清单.md)
- [问题分析汇总快速参考](../../客服反馈问题模板/files (1)/00_问题分析汇总_快速参考.md)

## 📅 实施计划

### Sprint 1（2周）
- Phase 1: Excel批量导入功能
- Phase 2: 模板标准化和字段增强

### Sprint 2（2周）
- Phase 3: 根本原因分析功能
- Phase 4: 验证和满意度管理

### Sprint 3（2周）
- Phase 5: 项目-问题关联管理
- Phase 6: 前端功能增强

**总工作量**：约6周（30-40人天）

## 📚 详细设计文档

详细的实现设计文档请参考：
- [Issue #047 详细设计文档](./ISSUE_047_DETAILED_DESIGN.md)

详细设计文档包含：
- 完整的数据结构设计（实体类、DTO、配置类）
- 详细的API接口设计（请求/响应模型、端点定义）
- 完整的前端界面设计（组件结构、交互流程）
- 字段映射配置设计（支持多种Excel格式）
- 数据验证规则设计（必填、格式、范围、业务规则）
- 错误处理机制设计（错误分类、报告、修复建议）
- 导入流程设计（流程图、实现逻辑）
- 数据库设计（表结构、索引、EF配置）

