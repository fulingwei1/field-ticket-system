# Issue #047: Excel批量导入和模板增强 - 详细设计文档

## 📋 目录

1. [数据结构详细设计](#数据结构详细设计)
2. [API接口详细设计](#api接口详细设计)
3. [前端界面详细设计](#前端界面详细设计)
4. [字段映射配置设计](#字段映射配置设计)
5. [数据验证规则设计](#数据验证规则设计)
6. [错误处理机制设计](#错误处理机制设计)
7. [导入流程设计](#导入流程设计)
8. [数据库设计](#数据库设计)

---

## 数据结构详细设计

### 1. 项目实体（Project）

**文件**：`backend/src/FieldTicket.Domain/Entities/Project.cs`

```csharp
namespace FieldTicket.Domain.Entities;

/// <summary>
/// 项目实体
/// </summary>
public class Project
{
    public Guid ProjectId { get; set; }
    
    // 基本信息
    public string ProjectNo { get; set; } = string.Empty; // 项目号（唯一）
    public string ProjectName { get; set; } = string.Empty; // 项目名称
    public Guid CustomerId { get; set; } // 客户ID（关联Customer表）
    public string? CustomerName { get; set; } // 客户名称（冗余字段，便于查询）
    
    // 设备信息
    public string DeviceType { get; set; } = string.Empty; // 设备类型：线体/单机/其他
    public string? IndustryType { get; set; } // 行业类型：汽车/白电/3C等
    
    // 财务信息
    public decimal? SalesAmount { get; set; } // 销售金额
    public int Quantity { get; set; } = 1; // 数量
    
    // 时间信息
    public DateTime? OrderDate { get; set; } // 下单日期
    public DateTime? RequiredDeliveryDate { get; set; } // 要求交货日期
    public DateTime? ActualDeliveryDate { get; set; } // 实际交货日期
    public int? DeliveryDelayDays { get; set; } // 交货延期天数（自动计算）
    
    // 状态信息
    public string ProjectStatus { get; set; } = "进行中"; // 进行中/已交付/已验证/有问题
    
    // 人员信息
    public Guid? ProjectManagerId { get; set; } // 项目经理ID
    public string? ProjectManagerName { get; set; } // 项目经理名称
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // 关联关系
    public virtual ICollection<FieldProblem> Problems { get; set; } = new List<FieldProblem>();
}
```

### 2. 现场问题实体（FieldProblem）

**文件**：`backend/src/FieldTicket.Domain/Entities/FieldProblem.cs`

```csharp
namespace FieldTicket.Domain.Entities;

/// <summary>
/// 现场问题实体
/// </summary>
public class FieldProblem
{
    public Guid ProblemId { get; set; }
    
    // 关联信息
    public Guid ProjectId { get; set; } // 项目ID（外键）
    public virtual Project Project { get; set; } = null!;
    public int ProblemSequence { get; set; } // 问题序号（项目内的第几个问题）
    
    // 问题基本信息
    public string ProblemCategory { get; set; } = string.Empty; // 问题分类
    public string ProblemDescription { get; set; } = string.Empty; // 问题描述
    public string? Priority { get; set; } // 优先级：P1/P2/P3/P4
    
    // 时间信息
    public DateTime FoundDate { get; set; } // 发现日期
    public DateTime? CompletedDate { get; set; } // 处理完成日期
    public int? ProcessingDays { get; set; } // 处理周期天数（自动计算）
    
    // 责任信息
    public string PrimaryDepartment { get; set; } = string.Empty; // 主负责部门
    public string PrimaryResponsible { get; set; } = string.Empty; // 主负责人
    public Guid? PrimaryResponsibleId { get; set; } // 主负责人ID（关联User）
    public string? CollaboratingDepartment { get; set; } // 协作部门
    public string? CollaboratingPerson { get; set; } // 协作人员
    public Guid? CollaboratingPersonId { get; set; } // 协作人员ID
    
    // 处理信息
    public string Status { get; set; } = "待分配"; // 处理状态
    public string? Solution { get; set; } // 处理方案
    public string? SolutionDetails { get; set; } // 处理详情
    
    // 验证信息
    public string? VerificationStatus { get; set; } // 验证状态：未验证/验证通过/验证失败
    public string? CustomerFeedback { get; set; } // 客户反馈
    public int? SatisfactionScore { get; set; } // 满意度评分（1-5）
    public DateTime? VerifiedAt { get; set; } // 验证时间
    public string? VerifiedBy { get; set; } // 验证人
    
    // 关联信息
    public Guid? RelatedTicketId { get; set; } // 关联工单ID
    public string? RelatedTicketNo { get; set; } // 关联工单号
    public string? KnowledgeBaseId { get; set; } // 知识库ID
    public bool IsRepeatProblem { get; set; } = false; // 是否重复问题
    public Guid? RelatedHistoryProblemId { get; set; } // 相关历史问题ID
    
    // 备注
    public string? Notes { get; set; } // 备注
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // 关联关系
    public virtual RootCauseAnalysis? RootCauseAnalysis { get; set; } // 根本原因分析
}
```

### 3. 根本原因分析实体（RootCauseAnalysis）

**文件**：`backend/src/FieldTicket.Domain/Entities/RootCauseAnalysis.cs`

```csharp
namespace FieldTicket.Domain.Entities;

/// <summary>
/// 根本原因分析实体
/// </summary>
public class RootCauseAnalysis
{
    public Guid AnalysisId { get; set; }
    
    // 关联信息
    public Guid ProblemId { get; set; } // 问题ID（外键）
    public virtual FieldProblem Problem { get; set; } = null!;
    
    // 5Why分析
    public string? Why1 { get; set; } // 为什么会出现这个问题？
    public string? Why2 { get; set; } // 为什么会有这个原因？
    public string? Why3 { get; set; } // 为什么？
    public string? Why4 { get; set; } // 为什么？
    public string? Why5 { get; set; } // 为什么？
    
    // 根本原因
    public string RootCause { get; set; } = string.Empty; // 根本原因
    public string? RootCauseCategory { get; set; } // 根本原因分类：设计/工艺/管理/其他
    
    // 预防措施
    public string? PreventiveMeasures { get; set; } // 预防措施
    public string? VerificationMethod { get; set; } // 验证方法
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? AnalyzedBy { get; set; } // 分析人
    public DateTime? AnalyzedAt { get; set; } // 分析时间
}
```

### 4. Excel导入配置实体（ExcelImportTemplate）

**文件**：`backend/src/FieldTicket.Domain/Entities/ExcelImportTemplate.cs`

```csharp
namespace FieldTicket.Domain.Entities;

/// <summary>
/// Excel导入模板配置实体
/// </summary>
public class ExcelImportTemplate
{
    public Guid TemplateId { get; set; }
    
    // 模板基本信息
    public string TemplateName { get; set; } = string.Empty; // 模板名称
    public string? Description { get; set; } // 描述
    public string TemplateType { get; set; } = "ProjectProblem"; // 模板类型：ProjectProblem/Project/Problem
    
    // 字段映射配置（JSONB）
    public JsonDocument FieldMappings { get; set; } = JsonDocument.Parse("{}");
    /*
    示例结构：
    {
      "projectSheet": {
        "sheetName": "项目主表",
        "startRow": 2,
        "mappings": {
          "项目号": { "field": "ProjectNo", "type": "string", "required": true },
          "项目名称": { "field": "ProjectName", "type": "string", "required": true },
          "客户名称": { "field": "CustomerName", "type": "string", "required": true },
          "下单日期": { "field": "OrderDate", "type": "date", "required": false, "formats": ["yyyy-MM-dd", "yyyy.M.d", "yyyy/M/d"] }
        }
      },
      "problemSheet": {
        "sheetName": "现场问题表",
        "startRow": 2,
        "mappings": {
          "问题描述": { "field": "ProblemDescription", "type": "string", "required": true },
          "问题分类": { "field": "ProblemCategory", "type": "enum", "enumType": "ProblemCategory", "required": true }
        }
      }
    }
    */
    
    // 数据验证规则（JSONB）
    public JsonDocument ValidationRules { get; set; } = JsonDocument.Parse("{}");
    
    // 是否默认模板
    public bool IsDefault { get; set; } = false;
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
```

---

## API接口详细设计

### 1. Excel导入服务接口

**文件**：`backend/src/FieldTicket.Core/Services/IExcelImportService.cs`

```csharp
namespace FieldTicket.Core.Services;

/// <summary>
/// Excel导入服务接口
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// 解析Excel文件，返回预览数据
    /// </summary>
    Task<ExcelPreviewResult> PreviewExcelAsync(Stream excelStream, Guid? templateId = null);
    
    /// <summary>
    /// 导入Excel数据
    /// </summary>
    Task<ExcelImportResult> ImportExcelAsync(Stream excelStream, ExcelImportOptions options);
    
    /// <summary>
    /// 获取可用的导入模板列表
    /// </summary>
    Task<List<ExcelImportTemplateDto>> GetTemplatesAsync();
    
    /// <summary>
    /// 创建或更新导入模板
    /// </summary>
    Task<Guid> SaveTemplateAsync(ExcelImportTemplateDto template, Guid userId);
    
    /// <summary>
    /// 验证Excel数据
    /// </summary>
    Task<ExcelValidationResult> ValidateExcelDataAsync(ExcelPreviewResult preview);
}
```

### 2. 项目-问题服务接口

**文件**：`backend/src/FieldTicket.Core/Services/IProjectProblemService.cs`

```csharp
namespace FieldTicket.Core.Services;

/// <summary>
/// 项目-问题服务接口
/// </summary>
public interface IProjectProblemService
{
    /// <summary>
    /// 创建项目
    /// </summary>
    Task<Guid> CreateProjectAsync(CreateProjectRequest request, Guid userId);
    
    /// <summary>
    /// 更新项目
    /// </summary>
    Task UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, Guid userId);
    
    /// <summary>
    /// 获取项目列表
    /// </summary>
    Task<PagedResult<ProjectDto>> GetProjectsAsync(ProjectQueryRequest request);
    
    /// <summary>
    /// 获取项目详情（包含问题列表）
    /// </summary>
    Task<ProjectDetailDto> GetProjectDetailAsync(Guid projectId);
    
    /// <summary>
    /// 创建问题
    /// </summary>
    Task<Guid> CreateProblemAsync(Guid projectId, CreateProblemRequest request, Guid userId);
    
    /// <summary>
    /// 更新问题
    /// </summary>
    Task UpdateProblemAsync(Guid problemId, UpdateProblemRequest request, Guid userId);
    
    /// <summary>
    /// 获取问题列表
    /// </summary>
    Task<PagedResult<FieldProblemDto>> GetProblemsAsync(ProblemQueryRequest request);
    
    /// <summary>
    /// 获取项目健康度评估
    /// </summary>
    Task<ProjectHealthDto> GetProjectHealthAsync(Guid projectId);
}
```

### 3. 根本原因分析服务接口

**文件**：`backend/src/FieldTicket.Core/Services/IRootCauseAnalysisService.cs`

```csharp
namespace FieldTicket.Core.Services;

/// <summary>
/// 根本原因分析服务接口
/// </summary>
public interface IRootCauseAnalysisService
{
    /// <summary>
    /// 创建或更新根本原因分析
    /// </summary>
    Task<Guid> SaveRootCauseAnalysisAsync(Guid problemId, RootCauseAnalysisRequest request, Guid userId);
    
    /// <summary>
    /// 获取根本原因分析
    /// </summary>
    Task<RootCauseAnalysisDto?> GetRootCauseAnalysisAsync(Guid problemId);
    
    /// <summary>
    /// 获取根本原因分析统计
    /// </summary>
    Task<RootCauseStatisticsDto> GetRootCauseStatisticsAsync(RootCauseStatisticsRequest request);
}
```

### 4. API端点设计

**文件**：`backend/src/FieldTicket.Api/Endpoints/ExcelImportEndpoints.cs`

```csharp
namespace FieldTicket.Api.Endpoints;

public static class ExcelImportEndpoints
{
    public static void MapExcelImportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/excel-import")
            .WithTags("ExcelImport")
            .RequireAuthorization();
        
        // 预览Excel文件
        group.MapPost("/preview", PreviewExcel)
            .WithName("PreviewExcel")
            .WithSummary("预览Excel文件，返回解析结果");
        
        // 导入Excel数据
        group.MapPost("/import", ImportExcel)
            .WithName("ImportExcel")
            .WithSummary("导入Excel数据");
        
        // 获取导入模板列表
        group.MapGet("/templates", GetTemplates)
            .WithName("GetImportTemplates")
            .WithSummary("获取导入模板列表");
        
        // 创建或更新导入模板
        group.MapPost("/templates", SaveTemplate)
            .WithName("SaveImportTemplate")
            .WithSummary("创建或更新导入模板");
    }
    
    private static async Task<IResult> PreviewExcel(
        IFormFile file,
        [FromQuery] Guid? templateId,
        IExcelImportService service)
    {
        // 实现预览逻辑
    }
    
    private static async Task<IResult> ImportExcel(
        IFormFile file,
        [FromBody] ExcelImportOptions options,
        HttpContext context,
        IExcelImportService service)
    {
        // 实现导入逻辑
    }
}
```

**文件**：`backend/src/FieldTicket.Api/Endpoints/ProjectProblemEndpoints.cs`

```csharp
namespace FieldTicket.Api.Endpoints;

public static class ProjectProblemEndpoints
{
    public static void MapProjectProblemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();
        
        // 项目相关
        group.MapGet("", GetProjects)
            .WithName("GetProjects")
            .WithSummary("获取项目列表");
        
        group.MapGet("{projectId:guid}", GetProjectDetail)
            .WithName("GetProjectDetail")
            .WithSummary("获取项目详情");
        
        group.MapPost("", CreateProject)
            .WithName("CreateProject")
            .WithSummary("创建项目");
        
        group.MapPut("{projectId:guid}", UpdateProject)
            .WithName("UpdateProject")
            .WithSummary("更新项目");
        
        // 问题相关
        group.MapGet("{projectId:guid}/problems", GetProjectProblems)
            .WithName("GetProjectProblems")
            .WithSummary("获取项目的问题列表");
        
        group.MapPost("{projectId:guid}/problems", CreateProblem)
            .WithName("CreateProblem")
            .WithSummary("创建问题");
        
        group.MapPut("problems/{problemId:guid}", UpdateProblem)
            .WithName("UpdateProblem")
            .WithSummary("更新问题");
        
        // 健康度评估
        group.MapGet("{projectId:guid}/health", GetProjectHealth)
            .WithName("GetProjectHealth")
            .WithSummary("获取项目健康度评估");
    }
}
```

---

## 前端界面详细设计

### 1. Excel导入页面

**文件**：`web-admin/src/pages/projects/ExcelImport.tsx`

**功能模块**：

#### 1.1 文件上传区域
```tsx
<Card title="上传Excel文件">
  <Upload
    accept=".xlsx,.xls"
    beforeUpload={(file) => {
      // 文件大小限制：10MB
      if (file.size > 10 * 1024 * 1024) {
        message.error('文件大小不能超过10MB');
        return false;
      }
      return true;
    }}
    customRequest={handleUpload}
  >
    <Button icon={<UploadOutlined />}>选择Excel文件</Button>
  </Upload>
  
  <div style={{ marginTop: 16 }}>
    <Text type="secondary">
      支持格式：.xlsx, .xls | 最大文件大小：10MB
    </Text>
  </div>
</Card>
```

#### 1.2 模板选择区域
```tsx
<Card title="选择导入模板">
  <Select
    value={selectedTemplateId}
    onChange={setSelectedTemplateId}
    style={{ width: '100%' }}
    placeholder="选择导入模板（可选，系统会自动识别）"
  >
    {templates.map(template => (
      <Option key={template.templateId} value={template.templateId}>
        {template.templateName}
      </Option>
    ))}
  </Select>
  
  <Button
    type="link"
    onClick={() => setTemplateConfigVisible(true)}
  >
    配置字段映射
  </Button>
</Card>
```

#### 1.3 数据预览区域
```tsx
<Card title="数据预览" loading={previewLoading}>
  {previewData && (
    <>
      <Alert
        message={`检测到 ${previewData.projects.length} 个项目，${previewData.totalProblems} 个问题`}
        type="info"
        style={{ marginBottom: 16 }}
      />
      
      <Tabs>
        <TabPane tab="项目数据" key="projects">
          <Table
            columns={projectColumns}
            dataSource={previewData.projects}
            pagination={false}
            size="small"
          />
        </TabPane>
        
        <TabPane tab="问题数据" key="problems">
          <Table
            columns={problemColumns}
            dataSource={previewData.problems}
            pagination={false}
            size="small"
          />
        </TabPane>
        
        <TabPane tab="验证结果" key="validation">
          <ValidationResultTable
            errors={previewData.validationErrors}
            warnings={previewData.validationWarnings}
          />
        </TabPane>
      </Tabs>
    </>
  )}
</Card>
```

#### 1.4 导入选项区域
```tsx
<Card title="导入选项">
  <Space direction="vertical" style={{ width: '100%' }}>
    <Checkbox
      checked={importOptions.skipErrors}
      onChange={(e) => setImportOptions({ ...importOptions, skipErrors: e.target.checked })}
    >
      跳过错误数据，继续导入
    </Checkbox>
    
    <Checkbox
      checked={importOptions.updateExisting}
      onChange={(e) => setImportOptions({ ...importOptions, updateExisting: e.target.checked })}
    >
      如果项目已存在，更新项目信息
    </Checkbox>
    
    <Checkbox
      checked={importOptions.createTickets}
      onChange={(e) => setImportOptions({ ...importOptions, createTickets: e.target.checked })}
    >
      自动创建关联工单
    </Checkbox>
  </Space>
</Card>
```

#### 1.5 导入按钮和结果
```tsx
<Space>
  <Button
    type="primary"
    icon={<ImportOutlined />}
    onClick={handleImport}
    loading={importing}
    disabled={!previewData}
  >
    开始导入
  </Button>
  
  <Button onClick={handleReset}>
    重置
  </Button>
</Space>

{importResult && (
  <Card title="导入结果" style={{ marginTop: 16 }}>
    <Result
      status={importResult.success ? "success" : "warning"}
      title={importResult.success ? "导入成功" : "导入完成（有错误）"}
      subTitle={
        <>
          <div>成功：{importResult.successCount} 条</div>
          <div>失败：{importResult.failureCount} 条</div>
          {importResult.errors.length > 0 && (
            <div>
              <Text type="danger">错误详情：</Text>
              <ul>
                {importResult.errors.map((error, index) => (
                  <li key={index}>{error}</li>
                ))}
              </ul>
            </div>
          )}
        </>
      }
    />
  </Card>
)}
```

### 2. 项目-问题管理页面

**文件**：`web-admin/src/pages/projects/ProjectList.tsx`

**功能模块**：

#### 2.1 项目列表
```tsx
<Table
  columns={[
    {
      title: '项目号',
      dataIndex: 'projectNo',
      key: 'projectNo',
      width: 150,
    },
    {
      title: '项目名称',
      dataIndex: 'projectName',
      key: 'projectName',
      width: 200,
    },
    {
      title: '客户名称',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 150,
    },
    {
      title: '问题数',
      dataIndex: 'problemCount',
      key: 'problemCount',
      width: 100,
      render: (count, record) => (
        <Badge count={count} showZero>
          <Button
            type="link"
            onClick={() => navigate(`/projects/${record.projectId}/problems`)}
          >
            查看问题
          </Button>
        </Badge>
      ),
    },
    {
      title: '项目状态',
      dataIndex: 'projectStatus',
      key: 'projectStatus',
      width: 120,
      render: (status) => <Tag color={getStatusColor(status)}>{status}</Tag>,
    },
    {
      title: '交货延期',
      dataIndex: 'deliveryDelayDays',
      key: 'deliveryDelayDays',
      width: 120,
      render: (days) => days > 0 ? <Text type="danger">{days} 天</Text> : <Text type="success">按时</Text>,
    },
    {
      title: '操作',
      key: 'action',
      width: 200,
      render: (_, record) => (
        <Space>
          <Button
            type="link"
            onClick={() => navigate(`/projects/${record.projectId}`)}
          >
            详情
          </Button>
          <Button
            type="link"
            onClick={() => handleEdit(record)}
          >
            编辑
          </Button>
        </Space>
      ),
    },
  ]}
  dataSource={projects}
  loading={loading}
  pagination={{
    current: page,
    pageSize: pageSize,
    total: total,
    onChange: (p, s) => {
      setPage(p);
      setPageSize(s);
    },
  }}
/>
```

### 3. 项目详情页面

**文件**：`web-admin/src/pages/projects/ProjectDetail.tsx`

**功能模块**：

#### 3.1 项目基本信息
```tsx
<Card title="项目基本信息">
  <Descriptions column={2}>
    <Descriptions.Item label="项目号">{project.projectNo}</Descriptions.Item>
    <Descriptions.Item label="项目名称">{project.projectName}</Descriptions.Item>
    <Descriptions.Item label="客户名称">{project.customerName}</Descriptions.Item>
    <Descriptions.Item label="设备类型">{project.deviceType}</Descriptions.Item>
    <Descriptions.Item label="下单日期">{formatDate(project.orderDate)}</Descriptions.Item>
    <Descriptions.Item label="要求交货日期">{formatDate(project.requiredDeliveryDate)}</Descriptions.Item>
    <Descriptions.Item label="实际交货日期">{formatDate(project.actualDeliveryDate)}</Descriptions.Item>
    <Descriptions.Item label="交货延期">
      {project.deliveryDelayDays > 0 ? (
        <Text type="danger">{project.deliveryDelayDays} 天</Text>
      ) : (
        <Text type="success">按时</Text>
      )}
    </Descriptions.Item>
  </Descriptions>
</Card>
```

#### 3.2 项目健康度评估
```tsx
<Card title="项目健康度评估">
  <Row gutter={16}>
    <Col span={6}>
      <Statistic
        title="问题总数"
        value={health.totalProblems}
      />
    </Col>
    <Col span={6}>
      <Statistic
        title="已完成"
        value={health.completedProblems}
        suffix={`/ ${health.totalProblems}`}
      />
    </Col>
    <Col span={6}>
      <Statistic
        title="平均处理周期"
        value={health.averageProcessingDays}
        suffix="天"
      />
    </Col>
    <Col span={6}>
      <Statistic
        title="平均满意度"
        value={health.averageSatisfactionScore}
        suffix="/ 5"
        precision={1}
      />
    </Col>
  </Row>
  
  <Progress
    percent={health.completionRate}
    status={health.completionRate === 100 ? "success" : "active"}
    style={{ marginTop: 16 }}
  />
</Card>
```

#### 3.3 问题列表
```tsx
<Card title="问题列表">
  <Table
    columns={[
      {
        title: '序号',
        dataIndex: 'problemSequence',
        key: 'problemSequence',
        width: 80,
      },
      {
        title: '问题分类',
        dataIndex: 'problemCategory',
        key: 'problemCategory',
        width: 120,
        render: (category) => <Tag>{category}</Tag>,
      },
      {
        title: '问题描述',
        dataIndex: 'problemDescription',
        key: 'problemDescription',
        ellipsis: true,
      },
      {
        title: '处理状态',
        dataIndex: 'status',
        key: 'status',
        width: 120,
        render: (status) => <Tag color={getStatusColor(status)}>{status}</Tag>,
      },
      {
        title: '负责人',
        dataIndex: 'primaryResponsible',
        key: 'primaryResponsible',
        width: 120,
      },
      {
        title: '处理周期',
        dataIndex: 'processingDays',
        key: 'processingDays',
        width: 100,
        render: (days) => days ? `${days} 天` : '-',
      },
      {
        title: '满意度',
        dataIndex: 'satisfactionScore',
        key: 'satisfactionScore',
        width: 100,
        render: (score) => score ? <Rate disabled value={score} /> : '-',
      },
      {
        title: '操作',
        key: 'action',
        width: 200,
        render: (_, record) => (
          <Space>
            <Button
              type="link"
              onClick={() => handleViewProblem(record)}
            >
              详情
            </Button>
            <Button
              type="link"
              onClick={() => handleEditProblem(record)}
            >
              编辑
            </Button>
            <Button
              type="link"
              onClick={() => handleRootCauseAnalysis(record)}
            >
              根本原因
            </Button>
          </Space>
        ),
      },
    ]}
    dataSource={problems}
    rowKey="problemId"
  />
</Card>
```

### 4. 根本原因分析页面

**文件**：`web-admin/src/pages/projects/RootCauseAnalysis.tsx`

**功能模块**：

#### 4.1 5Why分析表单
```tsx
<Form
  form={form}
  layout="vertical"
  onFinish={handleSubmit}
>
  <Form.Item
    label="问题描述"
    name="problemDescription"
  >
    <Input.TextArea rows={3} disabled />
  </Form.Item>
  
  <Card title="5Why分析" style={{ marginBottom: 16 }}>
    <Form.Item
      label="Why 1: 为什么会出现这个问题？"
      name="why1"
      rules={[{ required: true, message: '请输入Why1' }]}
    >
      <Input.TextArea rows={2} />
    </Form.Item>
    
    <Form.Item
      label="Why 2: 为什么会有这个原因？"
      name="why2"
      rules={[{ required: true, message: '请输入Why2' }]}
    >
      <Input.TextArea rows={2} />
    </Form.Item>
    
    <Form.Item
      label="Why 3: 为什么？"
      name="why3"
    >
      <Input.TextArea rows={2} />
    </Form.Item>
    
    <Form.Item
      label="Why 4: 为什么？"
      name="why4"
    >
      <Input.TextArea rows={2} />
    </Form.Item>
    
    <Form.Item
      label="Why 5: 为什么？"
      name="why5"
    >
      <Input.TextArea rows={2} />
    </Form.Item>
  </Card>
  
  <Card title="根本原因和预防措施" style={{ marginBottom: 16 }}>
    <Form.Item
      label="根本原因"
      name="rootCause"
      rules={[{ required: true, message: '请输入根本原因' }]}
    >
      <Input.TextArea rows={3} />
    </Form.Item>
    
    <Form.Item
      label="根本原因分类"
      name="rootCauseCategory"
    >
      <Select>
        <Option value="设计">设计</Option>
        <Option value="工艺">工艺</Option>
        <Option value="管理">管理</Option>
        <Option value="其他">其他</Option>
      </Select>
    </Form.Item>
    
    <Form.Item
      label="预防措施"
      name="preventiveMeasures"
      rules={[{ required: true, message: '请输入预防措施' }]}
    >
      <Input.TextArea rows={4} />
    </Form.Item>
    
    <Form.Item
      label="验证方法"
      name="verificationMethod"
    >
      <Input.TextArea rows={2} />
    </Form.Item>
  </Card>
  
  <Form.Item>
    <Button type="primary" htmlType="submit">
      保存分析
    </Button>
  </Form.Item>
</Form>
```

---

## 字段映射配置设计

### 1. 字段映射配置结构

```typescript
interface ExcelFieldMapping {
  excelColumn: string;        // Excel列名（支持多个别名）
  systemField: string;        // 系统字段名
  fieldType: FieldType;      // 字段类型
  required: boolean;          // 是否必填
  defaultValue?: any;         // 默认值
  converter?: string;         // 转换函数名
  validationRules?: ValidationRule[]; // 验证规则
  enumValues?: string[];      // 枚举值（如果是枚举类型）
}

enum FieldType {
  String = 'string',
  Number = 'number',
  Date = 'date',
  Boolean = 'boolean',
  Enum = 'enum',
  Reference = 'reference', // 引用类型（如客户ID、用户ID）
}

interface ValidationRule {
  type: 'required' | 'format' | 'range' | 'custom';
  message: string;
  validator?: (value: any) => boolean;
}
```

### 2. 默认字段映射配置

**文件**：`backend/src/FieldTicket.Infrastructure/Config/DefaultExcelMappings.json`

```json
{
  "projectSheet": {
    "sheetName": "项目主表",
    "alternateNames": ["项目", "Project", "项目信息"],
    "startRow": 2,
    "mappings": [
      {
        "excelColumn": "项目号",
        "alternateNames": ["项目号(PJ)", "项目编号", "ProjectNo"],
        "systemField": "ProjectNo",
        "fieldType": "string",
        "required": true
      },
      {
        "excelColumn": "项目名称",
        "alternateNames": ["项目名", "ProjectName"],
        "systemField": "ProjectName",
        "fieldType": "string",
        "required": true
      },
      {
        "excelColumn": "客户名称",
        "alternateNames": ["客户", "CustomerName"],
        "systemField": "CustomerName",
        "fieldType": "string",
        "required": true
      },
      {
        "excelColumn": "下单日期",
        "alternateNames": ["下单日", "OrderDate"],
        "systemField": "OrderDate",
        "fieldType": "date",
        "required": false,
        "dateFormats": ["yyyy-MM-dd", "yyyy.M.d", "yyyy/M/d", "yyyy.MM.dd", "yyyy/MM/dd"]
      },
      {
        "excelColumn": "要求交货日期",
        "alternateNames": ["要求交货日", "RequiredDeliveryDate"],
        "systemField": "RequiredDeliveryDate",
        "fieldType": "date",
        "required": false,
        "dateFormats": ["yyyy-MM-dd", "yyyy.M.d", "yyyy/M/d"]
      },
      {
        "excelColumn": "实际交货日期",
        "alternateNames": ["实际交货日", "ActualDeliveryDate"],
        "systemField": "ActualDeliveryDate",
        "fieldType": "date",
        "required": false,
        "dateFormats": ["yyyy-MM-dd", "yyyy.M.d", "yyyy/M/d"]
      },
      {
        "excelColumn": "销售金额",
        "alternateNames": ["金额", "SalesAmount"],
        "systemField": "SalesAmount",
        "fieldType": "number",
        "required": false,
        "converter": "parseDecimal"
      },
      {
        "excelColumn": "设备类型",
        "alternateNames": ["设备", "DeviceType"],
        "systemField": "DeviceType",
        "fieldType": "enum",
        "required": false,
        "enumValues": ["线体", "单机", "其他"]
      }
    ]
  },
  "problemSheet": {
    "sheetName": "现场问题表",
    "alternateNames": ["问题", "Problem", "问题信息"],
    "startRow": 2,
    "mappings": [
      {
        "excelColumn": "项目号",
        "alternateNames": ["项目号(PJ)", "ProjectNo"],
        "systemField": "ProjectNo",
        "fieldType": "reference",
        "required": true,
        "referenceType": "Project"
      },
      {
        "excelColumn": "问题序号",
        "alternateNames": ["序号", "ProblemSequence"],
        "systemField": "ProblemSequence",
        "fieldType": "number",
        "required": true
      },
      {
        "excelColumn": "问题分类",
        "alternateNames": ["分类", "ProblemCategory"],
        "systemField": "ProblemCategory",
        "fieldType": "enum",
        "required": true,
        "enumValues": ["硬件故障", "硬件缺失", "软件缺陷", "工艺问题", "设计问题", "安装问题", "配置问题", "需求变更", "其他"],
        "converter": "normalizeProblemCategory"
      },
      {
        "excelColumn": "问题描述",
        "alternateNames": ["描述", "ProblemDescription", "现场异常描述"],
        "systemField": "ProblemDescription",
        "fieldType": "string",
        "required": true
      },
      {
        "excelColumn": "发现日期",
        "alternateNames": ["发现日", "FoundDate", "跟进起始日期"],
        "systemField": "FoundDate",
        "fieldType": "date",
        "required": true,
        "dateFormats": ["yyyy-MM-dd", "yyyy.M.d", "yyyy/M/d"]
      },
      {
        "excelColumn": "处理状态",
        "alternateNames": ["状态", "Status"],
        "systemField": "Status",
        "fieldType": "enum",
        "required": false,
        "enumValues": ["待分配", "处理中", "待验证", "验证中", "已验证", "验证失败", "已关闭"],
        "defaultValue": "待分配"
      },
      {
        "excelColumn": "主负责部门",
        "alternateNames": ["部门", "PrimaryDepartment", "部门归属"],
        "systemField": "PrimaryDepartment",
        "fieldType": "string",
        "required": true
      },
      {
        "excelColumn": "主负责人",
        "alternateNames": ["负责人", "PrimaryResponsible", "责任人"],
        "systemField": "PrimaryResponsible",
        "fieldType": "string",
        "required": true
      },
      {
        "excelColumn": "处理方案",
        "alternateNames": ["方案", "Solution", "处理效果及图片"],
        "systemField": "Solution",
        "fieldType": "string",
        "required": false
      },
      {
        "excelColumn": "处理完成日期",
        "alternateNames": ["完成日期", "CompletedDate", "跟进截止日期"],
        "systemField": "CompletedDate",
        "fieldType": "date",
        "required": false,
        "dateFormats": ["yyyy-MM-dd", "yyyy.M.d", "yyyy/M/d"]
      },
      {
        "excelColumn": "验证状态",
        "alternateNames": ["验证", "VerificationStatus"],
        "systemField": "VerificationStatus",
        "fieldType": "enum",
        "required": false,
        "enumValues": ["未验证", "验证通过", "验证失败"],
        "defaultValue": "未验证"
      },
      {
        "excelColumn": "客户反馈",
        "alternateNames": ["反馈", "CustomerFeedback"],
        "systemField": "CustomerFeedback",
        "fieldType": "string",
        "required": false
      },
      {
        "excelColumn": "满意度评分",
        "alternateNames": ["满意度", "SatisfactionScore"],
        "systemField": "SatisfactionScore",
        "fieldType": "number",
        "required": false,
        "validationRules": [
          {
            "type": "range",
            "min": 1,
            "max": 5,
            "message": "满意度评分必须在1-5之间"
          }
        ]
      }
    ]
  }
}
```

### 3. 字段转换函数

**文件**：`backend/src/FieldTicket.Infrastructure/Services/ExcelFieldConverters.cs`

```csharp
public static class ExcelFieldConverters
{
    /// <summary>
    /// 解析日期（支持多种格式）
    /// </summary>
    public static DateTime? ParseDate(string? value, string[] formats)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        // 尝试各种格式
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, null, DateTimeStyles.None, out var date))
                return date;
        }
        
        // 尝试通用解析
        if (DateTime.TryParse(value, out var parsedDate))
            return parsedDate;
        
        return null;
    }
    
    /// <summary>
    /// 规范化问题分类
    /// </summary>
    public static string NormalizeProblemCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "其他";
        
        var mapping = new Dictionary<string, string>
        {
            { "硬件故障", "硬件故障" },
            { "硬件缺失", "硬件缺失" },
            { "软件缺陷", "软件缺陷" },
            { "软件问题", "软件缺陷" },
            { "工艺问题", "工艺问题" },
            { "设计问题", "设计问题" },
            { "安装问题", "安装问题" },
            { "配置问题", "配置问题" },
            { "需求变更", "需求变更" },
            { "其他", "其他" }
        };
        
        return mapping.TryGetValue(value, out var normalized) 
            ? normalized 
            : "其他";
    }
    
    /// <summary>
    /// 解析小数
    /// </summary>
    public static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        // 移除货币符号和千位分隔符
        var cleaned = value.Replace("¥", "").Replace("$", "").Replace(",", "");
        
        if (decimal.TryParse(cleaned, out var result))
            return result;
        
        return null;
    }
    
    /// <summary>
    /// 规范化处理状态
    /// </summary>
    public static string NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "待分配";
        
        var mapping = new Dictionary<string, string>
        {
            { "待分配", "待分配" },
            { "处理中", "处理中" },
            { "待验证", "待验证" },
            { "验证中", "验证中" },
            { "已验证", "已验证" },
            { "验证失败", "验证失败" },
            { "已关闭", "已关闭" },
            { "已完成", "已验证" },
            { "完成", "已验证" }
        };
        
        return mapping.TryGetValue(value, out var normalized) 
            ? normalized 
            : "待分配";
    }
}
```

---

## 数据验证规则设计

### 1. 验证规则类型

```csharp
public enum ValidationRuleType
{
    Required,           // 必填验证
    Format,             // 格式验证
    Range,              // 范围验证
    Reference,          // 引用验证（如客户ID、用户ID是否存在）
    Business,           // 业务规则验证
    Custom              // 自定义验证
}

public class ValidationRule
{
    public ValidationRuleType Type { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public Func<object, bool>? CustomValidator { get; set; }
}
```

### 2. 项目数据验证规则

```csharp
public static class ProjectValidationRules
{
    public static List<ValidationRule> GetRules()
    {
        return new List<ValidationRule>
        {
            // 必填字段
            new ValidationRule
            {
                Type = ValidationRuleType.Required,
                Field = "ProjectNo",
                Message = "项目号不能为空"
            },
            new ValidationRule
            {
                Type = ValidationRuleType.Required,
                Field = "ProjectName",
                Message = "项目名称不能为空"
            },
            new ValidationRule
            {
                Type = ValidationRuleType.Required,
                Field = "CustomerName",
                Message = "客户名称不能为空"
            },
            
            // 格式验证
            new ValidationRule
            {
                Type = ValidationRuleType.Format,
                Field = "ProjectNo",
                Message = "项目号格式不正确（应以PJ开头）",
                CustomValidator = (value) => 
                {
                    var projectNo = value?.ToString() ?? "";
                    return projectNo.StartsWith("PJ") && projectNo.Length >= 10;
                }
            },
            
            // 业务规则验证
            new ValidationRule
            {
                Type = ValidationRuleType.Business,
                Field = "ActualDeliveryDate",
                Message = "实际交货日期不能早于下单日期",
                CustomValidator = (value) =>
                {
                    // 需要与OrderDate比较
                    return true; // 简化示例
                }
            },
            
            // 引用验证
            new ValidationRule
            {
                Type = ValidationRuleType.Reference,
                Field = "CustomerName",
                Message = "客户不存在，请先创建客户",
                Parameters = new Dictionary<string, object>
                {
                    { "ReferenceType", "Customer" },
                    { "ReferenceField", "CustomerName" }
                }
            }
        };
    }
}
```

### 3. 问题数据验证规则

```csharp
public static class ProblemValidationRules
{
    public static List<ValidationRule> GetRules()
    {
        return new List<ValidationRule>
        {
            // 必填字段
            new ValidationRule
            {
                Type = ValidationRuleType.Required,
                Field = "ProjectNo",
                Message = "项目号不能为空"
            },
            new ValidationRule
            {
                Type = ValidationRuleType.Required,
                Field = "ProblemDescription",
                Message = "问题描述不能为空"
            },
            new ValidationRule
            {
                Type = ValidationRuleType.Required,
                Field = "FoundDate",
                Message = "发现日期不能为空"
            },
            
            // 范围验证
            new ValidationRule
            {
                Type = ValidationRuleType.Range,
                Field = "SatisfactionScore",
                Message = "满意度评分必须在1-5之间",
                Parameters = new Dictionary<string, object>
                {
                    { "Min", 1 },
                    { "Max", 5 }
                }
            },
            
            // 业务规则验证
            new ValidationRule
            {
                Type = ValidationRuleType.Business,
                Field = "CompletedDate",
                Message = "处理完成日期不能早于发现日期",
                CustomValidator = (value) =>
                {
                    // 需要与FoundDate比较
                    return true; // 简化示例
                }
            },
            
            // 引用验证
            new ValidationRule
            {
                Type = ValidationRuleType.Reference,
                Field = "ProjectNo",
                Message = "项目不存在，请先创建项目",
                Parameters = new Dictionary<string, object>
                {
                    { "ReferenceType", "Project" },
                    { "ReferenceField", "ProjectNo" }
                }
            }
        };
    }
}
```

---

## 错误处理机制设计

### 1. 错误类型定义

```csharp
public enum ExcelImportErrorType
{
    FileError,          // 文件错误（格式不支持、文件损坏等）
    FormatError,        // 格式错误（日期格式不对、数字格式不对等）
    DataError,          // 数据错误（必填字段缺失、数据不符合规则等）
    BusinessError,      // 业务错误（项目不存在、客户不存在等）
    ReferenceError,     // 引用错误（外键关联失败等）
    SystemError         // 系统错误（数据库错误、网络错误等）
}

public class ExcelImportError
{
    public ExcelImportErrorType ErrorType { get; set; }
    public int? RowNumber { get; set; }      // 行号（如果是数据行错误）
    public string? SheetName { get; set; }  // Sheet名称
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? InvalidValue { get; set; }
    public string? Suggestion { get; set; }  // 修复建议
}
```

### 2. 错误处理流程

```csharp
public class ExcelImportErrorHandler
{
    public List<ExcelImportError> Errors { get; } = new();
    public List<ExcelImportWarning> Warnings { get; } = new();
    
    public void AddError(ExcelImportError error)
    {
        Errors.Add(error);
    }
    
    public void AddWarning(ExcelImportWarning warning)
    {
        Warnings.Add(warning);
    }
    
    public bool HasErrors => Errors.Any();
    public bool HasWarnings => Warnings.Any();
    
    public ExcelValidationResult GetResult()
    {
        return new ExcelValidationResult
        {
            Errors = Errors,
            Warnings = Warnings,
            IsValid = !HasErrors,
            ErrorCount = Errors.Count,
            WarningCount = Warnings.Count
        };
    }
}
```

### 3. 错误报告格式

```csharp
public class ExcelImportResult
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<ExcelImportError> Errors { get; set; } = new();
    public List<ExcelImportWarning> Warnings { get; set; } = new();
    public Dictionary<string, int> ImportedCounts { get; set; } = new(); // 导入数量统计
    public string? ImportLogId { get; set; } // 导入日志ID（用于后续查询）
}
```

---

## 导入流程设计

### 1. 导入流程图

```
开始
  ↓
上传Excel文件
  ↓
解析Excel文件（读取所有Sheet和行）
  ↓
识别Sheet类型（项目表/问题表）
  ↓
应用字段映射配置
  ↓
数据转换（日期格式、枚举值等）
  ↓
数据验证
  ↓
[有错误？]
  ├─ 是 → 生成错误报告 → 返回预览结果
  └─ 否 → 继续
  ↓
生成预览数据
  ↓
[用户确认导入？]
  ├─ 否 → 结束
  └─ 是 → 继续
  ↓
开始事务
  ↓
导入项目数据
  ├─ 创建或更新项目
  └─ 记录导入日志
  ↓
导入问题数据
  ├─ 创建问题
  ├─ 关联项目
  └─ 记录导入日志
  ↓
[创建关联工单？]
  ├─ 是 → 为每个问题创建工单
  └─ 否 → 跳过
  ↓
提交事务
  ↓
生成导入结果报告
  ↓
结束
```

### 2. 导入服务实现伪代码

```csharp
public class ExcelImportService : IExcelImportService
{
    public async Task<ExcelPreviewResult> PreviewExcelAsync(Stream excelStream, Guid? templateId = null)
    {
        // 1. 加载模板配置
        var template = templateId.HasValue 
            ? await GetTemplateAsync(templateId.Value)
            : await GetDefaultTemplateAsync();
        
        // 2. 解析Excel文件
        var workbook = LoadWorkbook(excelStream);
        var sheets = workbook.Worksheets;
        
        // 3. 识别Sheet类型
        var projectSheet = IdentifySheet(sheets, template.ProjectSheetConfig);
        var problemSheet = IdentifySheet(sheets, template.ProblemSheetConfig);
        
        // 4. 解析数据
        var projects = ParseProjects(projectSheet, template);
        var problems = ParseProblems(problemSheet, template);
        
        // 5. 数据验证
        var validationResult = ValidateData(projects, problems);
        
        // 6. 返回预览结果
        return new ExcelPreviewResult
        {
            Projects = projects,
            Problems = problems,
            ValidationErrors = validationResult.Errors,
            ValidationWarnings = validationResult.Warnings,
            TotalProjects = projects.Count,
            TotalProblems = problems.Count
        };
    }
    
    public async Task<ExcelImportResult> ImportExcelAsync(Stream excelStream, ExcelImportOptions options)
    {
        // 1. 预览数据
        var preview = await PreviewExcelAsync(excelStream, options.TemplateId);
        
        // 2. 检查是否有错误
        if (preview.ValidationErrors.Any(e => e.ErrorType == ExcelImportErrorType.DataError) 
            && !options.SkipErrors)
        {
            return new ExcelImportResult
            {
                Success = false,
                Errors = preview.ValidationErrors,
                Message = "数据验证失败，请修复错误后重试"
            };
        }
        
        // 3. 开始导入
        var result = new ExcelImportResult
        {
            TotalRows = preview.TotalProjects + preview.TotalProblems
        };
        
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 4. 导入项目
            foreach (var projectData in preview.Projects)
            {
                try
                {
                    var project = await CreateOrUpdateProjectAsync(projectData, options);
                    result.ImportedCounts["Projects"] = result.ImportedCounts.GetValueOrDefault("Projects", 0) + 1;
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new ExcelImportError
                    {
                        ErrorType = ExcelImportErrorType.SystemError,
                        Message = $"导入项目失败: {ex.Message}",
                        Field = "Project",
                        InvalidValue = projectData.ProjectNo
                    });
                    
                    if (!options.SkipErrors)
                        throw;
                }
            }
            
            // 5. 导入问题
            foreach (var problemData in preview.Problems)
            {
                try
                {
                    var problem = await CreateProblemAsync(problemData, options);
                    result.ImportedCounts["Problems"] = result.ImportedCounts.GetValueOrDefault("Problems", 0) + 1;
                    result.SuccessCount++;
                    
                    // 6. 创建关联工单（如果选项启用）
                    if (options.CreateTickets)
                    {
                        await CreateRelatedTicketAsync(problem);
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new ExcelImportError
                    {
                        ErrorType = ExcelImportErrorType.SystemError,
                        Message = $"导入问题失败: {ex.Message}",
                        Field = "Problem",
                        InvalidValue = problemData.ProblemDescription
                    });
                    
                    if (!options.SkipErrors)
                        throw;
                }
            }
            
            // 7. 提交事务
            await transaction.CommitAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.Errors.Add(new ExcelImportError
            {
                ErrorType = ExcelImportErrorType.SystemError,
                Message = $"导入失败: {ex.Message}"
            });
        }
        
        return result;
    }
}
```

---

## 数据库设计

### 1. 数据库表结构

#### projects 表

```sql
CREATE TABLE projects (
    project_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_no VARCHAR(50) UNIQUE NOT NULL,
    project_name VARCHAR(200) NOT NULL,
    customer_id UUID,
    customer_name VARCHAR(200),
    device_type VARCHAR(50),
    industry_type VARCHAR(50),
    sales_amount DECIMAL(18, 2),
    quantity INTEGER DEFAULT 1,
    order_date DATE,
    required_delivery_date DATE,
    actual_delivery_date DATE,
    delivery_delay_days INTEGER,
    project_status VARCHAR(50) DEFAULT '进行中',
    project_manager_id UUID,
    project_manager_name VARCHAR(100),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID,
    updated_by UUID
);

CREATE INDEX idx_projects_project_no ON projects(project_no);
CREATE INDEX idx_projects_customer_id ON projects(customer_id);
CREATE INDEX idx_projects_project_status ON projects(project_status);
```

#### field_problems 表

```sql
CREATE TABLE field_problems (
    problem_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id UUID NOT NULL REFERENCES projects(project_id) ON DELETE CASCADE,
    problem_sequence INTEGER NOT NULL,
    problem_category VARCHAR(50) NOT NULL,
    problem_description TEXT NOT NULL,
    priority VARCHAR(10),
    found_date DATE NOT NULL,
    completed_date DATE,
    processing_days INTEGER,
    primary_department VARCHAR(100) NOT NULL,
    primary_responsible VARCHAR(100) NOT NULL,
    primary_responsible_id UUID,
    collaborating_department VARCHAR(100),
    collaborating_person VARCHAR(100),
    collaborating_person_id UUID,
    status VARCHAR(50) DEFAULT '待分配',
    solution TEXT,
    solution_details TEXT,
    verification_status VARCHAR(50),
    customer_feedback TEXT,
    satisfaction_score INTEGER CHECK (satisfaction_score >= 1 AND satisfaction_score <= 5),
    verified_at TIMESTAMP,
    verified_by VARCHAR(100),
    related_ticket_id UUID,
    related_ticket_no VARCHAR(50),
    knowledge_base_id VARCHAR(100),
    is_repeat_problem BOOLEAN DEFAULT FALSE,
    related_history_problem_id UUID,
    notes TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID,
    updated_by UUID,
    UNIQUE(project_id, problem_sequence)
);

CREATE INDEX idx_field_problems_project_id ON field_problems(project_id);
CREATE INDEX idx_field_problems_status ON field_problems(status);
CREATE INDEX idx_field_problems_category ON field_problems(problem_category);
CREATE INDEX idx_field_problems_primary_responsible_id ON field_problems(primary_responsible_id);
```

#### root_cause_analyses 表

```sql
CREATE TABLE root_cause_analyses (
    analysis_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    problem_id UUID NOT NULL REFERENCES field_problems(problem_id) ON DELETE CASCADE,
    why1 TEXT,
    why2 TEXT,
    why3 TEXT,
    why4 TEXT,
    why5 TEXT,
    root_cause TEXT NOT NULL,
    root_cause_category VARCHAR(50),
    preventive_measures TEXT,
    verification_method TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    analyzed_by UUID,
    analyzed_at TIMESTAMP,
    UNIQUE(problem_id)
);

CREATE INDEX idx_root_cause_analyses_problem_id ON root_cause_analyses(problem_id);
```

#### excel_import_templates 表

```sql
CREATE TABLE excel_import_templates (
    template_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_name VARCHAR(200) NOT NULL,
    description TEXT,
    template_type VARCHAR(50) DEFAULT 'ProjectProblem',
    field_mappings JSONB NOT NULL,
    validation_rules JSONB,
    is_default BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID
);

CREATE INDEX idx_excel_import_templates_template_type ON excel_import_templates(template_type);
CREATE INDEX idx_excel_import_templates_is_default ON excel_import_templates(is_default);
```

#### excel_import_logs 表

```sql
CREATE TABLE excel_import_logs (
    log_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_id UUID REFERENCES excel_import_templates(template_id),
    file_name VARCHAR(500),
    file_size BIGINT,
    total_rows INTEGER,
    success_count INTEGER,
    failure_count INTEGER,
    import_result JSONB,
    imported_by UUID,
    imported_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_excel_import_logs_imported_by ON excel_import_logs(imported_by);
CREATE INDEX idx_excel_import_logs_imported_at ON excel_import_logs(imported_at);
```

### 2. Entity Framework 配置

**文件**：`backend/src/FieldTicket.Infrastructure/Data/ApplicationDbContext.cs`

```csharp
// Project 实体配置
modelBuilder.Entity<Project>(entity =>
{
    entity.ToTable("projects");
    entity.HasKey(e => e.ProjectId);
    entity.Property(e => e.ProjectId).HasColumnName("project_id");
    entity.Property(e => e.ProjectNo).HasColumnName("project_no").HasMaxLength(50).IsRequired();
    entity.Property(e => e.ProjectName).HasColumnName("project_name").HasMaxLength(200).IsRequired();
    entity.Property(e => e.CustomerId).HasColumnName("customer_id");
    entity.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(200);
    entity.Property(e => e.DeviceType).HasColumnName("device_type").HasMaxLength(50);
    entity.Property(e => e.IndustryType).HasColumnName("industry_type").HasMaxLength(50);
    entity.Property(e => e.SalesAmount).HasColumnName("sales_amount").HasColumnType("decimal(18,2)");
    entity.Property(e => e.Quantity).HasColumnName("quantity").HasDefaultValue(1);
    entity.Property(e => e.OrderDate).HasColumnName("order_date");
    entity.Property(e => e.RequiredDeliveryDate).HasColumnName("required_delivery_date");
    entity.Property(e => e.ActualDeliveryDate).HasColumnName("actual_delivery_date");
    entity.Property(e => e.DeliveryDelayDays).HasColumnName("delivery_delay_days");
    entity.Property(e => e.ProjectStatus).HasColumnName("project_status").HasMaxLength(50).HasDefaultValue("进行中");
    entity.Property(e => e.ProjectManagerId).HasColumnName("project_manager_id");
    entity.Property(e => e.ProjectManagerName).HasColumnName("project_manager_name").HasMaxLength(100);
    entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
    entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
    
    entity.HasIndex(e => e.ProjectNo).IsUnique();
    entity.HasIndex(e => e.CustomerId);
    entity.HasIndex(e => e.ProjectStatus);
});

// FieldProblem 实体配置
modelBuilder.Entity<FieldProblem>(entity =>
{
    entity.ToTable("field_problems");
    entity.HasKey(e => e.ProblemId);
    entity.Property(e => e.ProblemId).HasColumnName("problem_id");
    entity.Property(e => e.ProjectId).HasColumnName("project_id").IsRequired();
    entity.Property(e => e.ProblemSequence).HasColumnName("problem_sequence").IsRequired();
    entity.Property(e => e.ProblemCategory).HasColumnName("problem_category").HasMaxLength(50).IsRequired();
    entity.Property(e => e.ProblemDescription).HasColumnName("problem_description").IsRequired();
    entity.Property(e => e.Priority).HasColumnName("priority").HasMaxLength(10);
    entity.Property(e => e.FoundDate).HasColumnName("found_date").IsRequired();
    entity.Property(e => e.CompletedDate).HasColumnName("completed_date");
    entity.Property(e => e.ProcessingDays).HasColumnName("processing_days");
    entity.Property(e => e.PrimaryDepartment).HasColumnName("primary_department").HasMaxLength(100).IsRequired();
    entity.Property(e => e.PrimaryResponsible).HasColumnName("primary_responsible").HasMaxLength(100).IsRequired();
    entity.Property(e => e.PrimaryResponsibleId).HasColumnName("primary_responsible_id");
    entity.Property(e => e.CollaboratingDepartment).HasColumnName("collaborating_department").HasMaxLength(100);
    entity.Property(e => e.CollaboratingPerson).HasColumnName("collaborating_person").HasMaxLength(100);
    entity.Property(e => e.CollaboratingPersonId).HasColumnName("collaborating_person_id");
    entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("待分配");
    entity.Property(e => e.Solution).HasColumnName("solution");
    entity.Property(e => e.SolutionDetails).HasColumnName("solution_details");
    entity.Property(e => e.VerificationStatus).HasColumnName("verification_status").HasMaxLength(50);
    entity.Property(e => e.CustomerFeedback).HasColumnName("customer_feedback");
    entity.Property(e => e.SatisfactionScore).HasColumnName("satisfaction_score");
    entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
    entity.Property(e => e.VerifiedBy).HasColumnName("verified_by").HasMaxLength(100);
    entity.Property(e => e.RelatedTicketId).HasColumnName("related_ticket_id");
    entity.Property(e => e.RelatedTicketNo).HasColumnName("related_ticket_no").HasMaxLength(50);
    entity.Property(e => e.KnowledgeBaseId).HasColumnName("knowledge_base_id").HasMaxLength(100);
    entity.Property(e => e.IsRepeatProblem).HasColumnName("is_repeat_problem").HasDefaultValue(false);
    entity.Property(e => e.RelatedHistoryProblemId).HasColumnName("related_history_problem_id");
    entity.Property(e => e.Notes).HasColumnName("notes");
    entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
    entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
    
    entity.HasOne(e => e.Project)
        .WithMany(p => p.Problems)
        .HasForeignKey(e => e.ProjectId)
        .OnDelete(DeleteBehavior.Cascade);
    
    entity.HasIndex(e => e.ProjectId);
    entity.HasIndex(e => new { e.ProjectId, e.ProblemSequence }).IsUnique();
    entity.HasIndex(e => e.Status);
    entity.HasIndex(e => e.ProblemCategory);
});
```

---

## 实施优先级和时间表

### Phase 1: 核心功能（2周）
- [ ] 数据库表创建和迁移
- [ ] 实体类和EF配置
- [ ] Excel导入服务基础框架
- [ ] 字段映射配置系统
- [ ] 基础数据验证

### Phase 2: 导入功能完善（2周）
- [ ] 日期格式自动识别
- [ ] 枚举值规范化
- [ ] 错误处理和报告
- [ ] 导入预览功能
- [ ] 批量导入功能

### Phase 3: 项目-问题管理（2周）
- [ ] 项目CRUD接口
- [ ] 问题CRUD接口
- [ ] 项目健康度评估
- [ ] 项目-问题关联查询

### Phase 4: 根本原因分析（1周）
- [ ] 根本原因分析实体和服务
- [ ] 5Why分析模板
- [ ] 根本原因统计

### Phase 5: 前端界面（2周）
- [ ] Excel导入页面
- [ ] 项目列表页面
- [ ] 项目详情页面
- [ ] 根本原因分析页面

### Phase 6: 测试和优化（1周）
- [ ] 单元测试
- [ ] 集成测试
- [ ] 性能优化
- [ ] 文档完善

**总工作量**：约10周（50-60人天）

---

## 总结

本详细设计文档涵盖了：

1. ✅ **完整的数据结构设计**：Project、FieldProblem、RootCauseAnalysis 实体
2. ✅ **详细的API接口设计**：导入、项目、问题、根本原因分析接口
3. ✅ **完整的前端界面设计**：Excel导入、项目列表、项目详情、根本原因分析页面
4. ✅ **字段映射配置设计**：支持多种Excel格式的自动识别和映射
5. ✅ **数据验证规则设计**：必填、格式、范围、业务规则验证
6. ✅ **错误处理机制设计**：错误分类、错误报告、修复建议
7. ✅ **导入流程设计**：完整的导入流程图和实现逻辑
8. ✅ **数据库设计**：完整的表结构和EF配置

该设计可以直接用于开发实施。









