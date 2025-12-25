# 构建错误修复指南

本文档记录了项目构建过程中常见错误的修复方法，供团队成员参考。

## 快速参考

**修复日期**: 2025-12-25
**修复错误数**: 85 个
**当前状态**: ✅ Infrastructure 项目构建成功（0 错误）

## 常见错误类型及修复方法

### 1. 类型转换错误

#### 问题：decimal 和 double 类型不匹配

```csharp
// ❌ 错误
var timeScore = avgTime < 300 ? 1.0m : Math.Max(0.0m, 1.0m - (avgTime - 300) / 600.0m);

// ✅ 修复
var timeScore = avgTime < 300 ? 1.0m : Math.Max(0.0m, 1.0m - ((decimal)avgTime - 300) / 600.0m);
```

**修复原则**：确保运算符两边类型一致，必要时添加显式类型转换。

#### 问题：char 和 string 类型混用

```csharp
// ❌ 错误
query = query.Where(t => t.Domain == request.Domain);  // Domain 是 char, request.Domain 是 string

// ✅ 修复
query = query.Where(t => t.Domain.ToString() == request.Domain);
```

#### 问题：JsonElement 和 JsonDocument 混用

```csharp
// ❌ 错误
FactsJson = root.TryGetProperty("factsJson", out var factsJsonProp)
    ? factsJsonProp  // JsonElement
    : JsonDocument.Parse("{}");

// ✅ 修复
FactsJson = root.TryGetProperty("factsJson", out var factsJsonProp)
    ? JsonDocument.Parse(factsJsonProp.GetRawText())
    : JsonDocument.Parse("{}");
```

### 2. 实体属性访问错误

#### 问题：访问不存在的属性

```csharp
// ❌ 错误
var userName = user.UserId;  // User 实体使用 Id 而非 UserId

// ✅ 修复
var userName = user.Id;
```

**批量修复命令**：
```bash
find . -name "*.cs" -exec sed -i '' 's/\.UserId ==/\.Id ==/g' {} +
```

#### 问题：通过导航属性访问

```csharp
// ❌ 错误
var name = userProfile.Name;  // UserProfile 没有 Name 属性

// ✅ 修复
var name = userProfile.User!.Name;  // 通过导航属性访问
```

#### 问题：实体缺少直接属性

如果频繁通过关联查询访问，考虑在实体中添加冗余字段：

```csharp
// Ticket.cs
public class Ticket
{
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }  // 添加冗余字段

    public Guid DeviceId { get; set; }
    public string? DeviceSn { get; set; }      // 添加冗余字段
}
```

### 3. DTO 属性缺失

#### 问题：DTO 缺少必需属性

```csharp
// ❌ 错误：访问不存在的属性
question.RelevanceScore = 0.8m;

// ✅ 修复：在 DTO 中添加属性
public class PersonalizedQuestion
{
    // ... 其他属性
    public decimal RelevanceScore { get; set; }  // 新增
    public string? Reason { get; set; }          // 新增
}
```

### 4. 方法与属性混淆

#### 问题：将属性当作方法使用

```csharp
// ❌ 错误
var count = items.Count;  // Count 是方法不是属性

// ✅ 修复
var count = items.Count();
```

### 5. IOptions 依赖注入

#### 问题：错误的 IOptions 使用方式

```csharp
// ❌ 错误
public AuthService(IOptions<WeComOptions> weComOptions)
{
    _weComOptions = weComOptions.Value;  // 存储 Value 而非 IOptions
}

// ✅ 修复
public AuthService(IOptions<WeComOptions> weComOptions)
{
    _weComOptions = weComOptions;  // 存储 IOptions 本身
}

// 使用时
var corpId = _weComOptions.Value.CorpId;
```

### 6. Nullable 操作符误用

#### 问题：在非 nullable 类型上使用 ?

```csharp
// ❌ 错误
DeptId = result.Department?.FirstOrDefault()?.ToString()
// FirstOrDefault() 返回 int，不是 nullable

// ✅ 修复
DeptId = result.Department?.FirstOrDefault().ToString()
```

### 7. MinIO SDK 版本问题

#### 问题：API 不兼容

```csharp
// ❌ 错误（旧版本 4.0.0）
.WithSSL(_options.UseSSL)  // WithSSL(bool) 方法不存在

// ✅ 修复（新版本 6.0.3）
var builder = new MinioClient()
    .WithEndpoint(_options.Endpoint)
    .WithCredentials(_options.AccessKey, _options.SecretKey);

if (_options.UseSSL)
{
    builder = builder.WithSSL();  // 无参数版本
}

_minioClient = builder.Build();
```

**包升级**：
```xml
<!-- 修改 .csproj 文件 -->
<PackageReference Include="Minio" Version="6.0.3" />
```

**添加命名空间**：
```csharp
using Minio.DataModel.Args;
```

### 8. 前端图标兼容性

#### 问题：使用不存在的 Ant Design 图标

```tsx
// ❌ 错误
import { ConfigOutlined } from '@ant-design/icons';

// ✅ 修复
import { ControlOutlined } from '@ant-design/icons';
```

常见替换：
- `ConfigOutlined` → `ControlOutlined`
- `CompareArrowsOutlined` → `SwapOutlined`

## 修复流程建议

### 1. 系统性分类

将错误按类型分组：
- 类型转换
- 实体访问
- DTO 定义
- 依赖注入
- 第三方库

### 2. 批量处理

对于相同模式的错误，使用批量工具：

```bash
# 示例：批量替换 User.UserId
find ./src -name "*.cs" -type f -exec sed -i '' 's/User\.UserId/User.Id/g' {} +

# 示例：批量替换 .Count
find ./src -name "*.cs" -type f -exec sed -i '' 's/\.Count;/\.Count();/g' {} +
```

### 3. 增量验证

每修复一批错误后立即构建验证：

```bash
cd backend/src/FieldTicket.Infrastructure
dotnet build

# 查看剩余错误
dotnet build 2>&1 | grep "error CS"
```

### 4. 错误优先级

1. **阻塞性错误**（编译失败）优先
2. **警告**（可编译但有风险）次之
3. **代码质量问题**最后

## 常用命令

### 构建相关

```bash
# 构建整个解决方案
cd backend/src
dotnet build

# 构建特定项目
cd backend/src/FieldTicket.Infrastructure
dotnet build

# 清理构建
dotnet clean

# 查看错误统计
dotnet build 2>&1 | grep -E "Error\(s\)|Warning\(s\)"
```

### 搜索相关

```bash
# 查找特定错误模式
grep -r "\.UserId" backend/src --include="*.cs"

# 统计某种模式出现次数
grep -r "\.Count[^(]" backend/src --include="*.cs" | wc -l
```

## 预防措施

### 1. 编码规范

- 统一使用明确的类型转换
- 优先使用实体的直接属性
- 避免过度依赖导航属性

### 2. 代码审查

关注点：
- 类型一致性
- 属性存在性
- 方法与属性区分
- Nullable 正确性

### 3. 持续集成

```yaml
# .github/workflows/build.yml
name: Build
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 8.0.x
      - name: Build
        run: dotnet build --configuration Release
```

## 参考资料

- [完整变更日志](../CHANGELOG.md)
- [后端 README](../backend/README.md)
- [.NET 8 文档](https://learn.microsoft.com/zh-cn/dotnet/core/whats-new/dotnet-8)
- [Entity Framework Core 文档](https://learn.microsoft.com/zh-cn/ef/core/)
- [MinIO .NET SDK 文档](https://min.io/docs/minio/linux/developers/dotnet/minio-dotnet.html)

## 问题反馈

如遇到新的构建问题，请：
1. 记录完整的错误信息
2. 尝试本文档中的相关修复方法
3. 如问题仍未解决，提交 Issue 附带详细信息

---

**维护者**: 开发团队
**最后更新**: 2025-12-25
