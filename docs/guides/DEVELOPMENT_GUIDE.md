# 开发指南

本文档为开发人员提供项目开发的基本指南和最佳实践。

## 🚀 快速开始

### 环境要求

- **.NET 8 SDK** - 后端开发
- **Node.js 18+** - 前端开发
- **Flutter 3.x** - 移动端开发
- **Docker & Docker Compose** - 容器化部署
- **PostgreSQL 16** - 数据库（或使用 Docker）
- **Redis 7** - 缓存（或使用 Docker）

### 本地开发环境搭建

#### 1. 克隆仓库

```bash
git clone https://github.com/YOUR_ORG/field-ticket-system.git
cd field-ticket-system
```

#### 2. 配置环境变量

```bash
# 复制环境变量模板
cp .env.example .env

# 编辑环境变量
vim .env
```

**必需配置**：
- 数据库连接字符串
- Redis 连接
- MinIO 配置
- 企业微信配置（CorpId, AgentId, Secret）
- JWT 密钥

#### 3. 启动依赖服务

```bash
# 启动 PostgreSQL, Redis, MinIO
docker-compose up -d postgres redis minio

# 等待服务就绪
sleep 10
```

#### 4. 初始化数据库

```bash
cd backend
dotnet ef database update
```

#### 5. 启动开发服务器

**后端**：
```bash
cd backend/src/FieldTicket.Api
dotnet run
```

**前端**：
```bash
cd web-admin
npm install
npm run dev
```

**移动端**：
```bash
cd mobile-app
flutter pub get
flutter run
```

## 📁 项目结构

```
field-ticket-system/
├── backend/                    # .NET 8 后端
│   ├── src/
│   │   ├── FieldTicket.Api/    # API 层（Minimal API）
│   │   ├── FieldTicket.Core/   # 领域模型、业务逻辑
│   │   ├── FieldTicket.Infrastructure/  # 数据访问、外部服务
│   │   └── FieldTicket.Shared/ # 共享 DTO、常量
│   └── tests/                  # 单元测试
│
├── web-admin/                  # React 管理端
│   ├── src/
│   │   ├── pages/             # 页面组件
│   │   ├── components/        # 通用组件
│   │   ├── services/          # API 服务
│   │   └── stores/           # 状态管理
│   └── public/
│
├── mobile-app/                # Flutter 移动端
│   ├── lib/
│   │   ├── pages/            # 页面
│   │   ├── widgets/          # 组件
│   │   ├── services/          # 服务
│   │   ├── models/           # 数据模型
│   │   └── providers/        # 状态管理
│   └── test/
│
└── docs/                      # 文档
```

## 🏗️ 架构设计

### 后端架构（Clean Architecture）

```
┌─────────────────────────────────────┐
│         FieldTicket.Api            │  ← API 层（Minimal API）
│  - Endpoints                        │
│  - Middleware                       │
│  - Request/Response Models          │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│         FieldTicket.Core            │  ← 业务逻辑层
│  - Entities                          │
│  - Services (Interfaces)             │
│  - Domain Logic                      │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│    FieldTicket.Infrastructure       │  ← 基础设施层
│  - EF Core (Data Access)             │
│  - External Services                 │
│  - Services (Implementations)        │
└─────────────────────────────────────┘
```

### 前端架构

- **状态管理**：Zustand 或 Redux Toolkit
- **路由**：React Router
- **UI 组件库**：Ant Design 5
- **HTTP 客户端**：Axios
- **表单处理**：React Hook Form

### 移动端架构

- **状态管理**：Provider 或 Riverpod
- **路由**：go_router
- **HTTP 客户端**：Dio
- **本地存储**：sqflite (SQLite)
- **网络状态**：connectivity_plus

## 📝 编码规范

### 后端 (.NET)

#### 命名规范

- **类名**：PascalCase（`TicketService`）
- **方法名**：PascalCase（`CreateTicketAsync`）
- **变量名**：camelCase（`ticketId`）
- **常量**：PascalCase（`MaxFileSize`）
- **私有字段**：`_camelCase`（`_logger`）

#### 代码示例

```csharp
// ✅ 好的示例
public class TicketService : ITicketService
{
    private readonly ILogger<TicketService> _logger;
    private readonly ITicketRepository _repository;

    public async Task<TicketDto> CreateTicketAsync(CreateTicketRequest request)
    {
        _logger.LogInformation("Creating ticket for device {DeviceId}", request.DeviceId);
        
        var ticket = new Ticket
        {
            TicketNo = GenerateTicketNo(),
            DeviceId = request.DeviceId,
            Status = TicketStatus.Draft
        };

        await _repository.AddAsync(ticket);
        await _repository.SaveChangesAsync();

        return MapToDto(ticket);
    }
}

// ❌ 不好的示例
public class ticketService  // 类名应该是 PascalCase
{
    public async Task create_ticket()  // 方法名应该是 PascalCase，使用 async/await
    {
        // 缺少日志和错误处理
    }
}
```

#### 最佳实践

- ✅ 使用 `async/await` 处理异步操作
- ✅ 使用依赖注入
- ✅ 添加 XML 注释
- ✅ 使用强类型（避免 `any`）
- ✅ 使用 `Result<T>` 模式处理错误
- ❌ 避免使用 `Task.Result` 或 `Task.Wait()`
- ❌ 避免在循环中使用 `async/await`

### 前端 (React/TypeScript)

#### 命名规范

- **组件名**：PascalCase（`TicketList`）
- **函数名**：camelCase（`handleSubmit`）
- **变量名**：camelCase（`ticketId`）
- **常量**：UPPER_SNAKE_CASE（`MAX_FILE_SIZE`）
- **文件名**：PascalCase（`TicketList.tsx`）

#### 代码示例

```typescript
// ✅ 好的示例
interface TicketListProps {
  tickets: Ticket[];
  onTicketClick: (ticketId: string) => void;
}

export const TicketList: React.FC<TicketListProps> = ({ tickets, onTicketClick }) => {
  const handleClick = useCallback((ticketId: string) => {
    onTicketClick(ticketId);
  }, [onTicketClick]);

  return (
    <List
      dataSource={tickets}
      renderItem={(ticket) => (
        <List.Item onClick={() => handleClick(ticket.id)}>
          {ticket.ticketNo}
        </List.Item>
      )}
    />
  );
};

// ❌ 不好的示例
export function ticketList(props: any) {  // 应该是 PascalCase，避免 any
  return <div>{props.tickets.map(t => <div>{t.id}</div>)}</div>;  // 缺少 key
}
```

#### 最佳实践

- ✅ 使用 TypeScript 严格模式
- ✅ 使用函数组件和 Hooks
- ✅ 使用 `useCallback` 和 `useMemo` 优化性能
- ✅ 组件拆分（单一职责）
- ✅ 使用 Error Boundary
- ❌ 避免在渲染中创建对象/函数
- ❌ 避免直接修改 state

### 移动端 (Flutter/Dart)

#### 命名规范

- **类名**：PascalCase（`TicketService`）
- **函数名**：camelCase（`createTicket`）
- **变量名**：camelCase（`ticketId`）
- **常量**：lowerCamelCase（`maxFileSize`）
- **文件名**：snake_case（`ticket_service.dart`）

#### 代码示例

```dart
// ✅ 好的示例
class TicketService {
  final Dio _dio;
  final Logger _logger = Logger('TicketService');

  Future<Ticket> createTicket(CreateTicketRequest request) async {
    _logger.info('Creating ticket for device ${request.deviceId}');
    
    try {
      final response = await _dio.post('/api/tickets', data: request.toJson());
      return Ticket.fromJson(response.data);
    } on DioException catch (e) {
      _logger.error('Failed to create ticket', e);
      rethrow;
    }
  }
}

// ❌ 不好的示例
class ticketService {  // 应该是 PascalCase
  create_ticket() {  // 应该是 camelCase
    // 缺少错误处理和日志
  }
}
```

## 🧪 测试

### 后端测试

```bash
cd backend
dotnet test
```

**测试结构**：
- 单元测试：测试单个方法/类
- 集成测试：测试 API 端点
- 使用 xUnit, Moq, FluentAssertions

### 前端测试

```bash
cd web-admin
npm test
```

**测试框架**：Jest + React Testing Library

### 移动端测试

```bash
cd mobile-app
flutter test
```

## 📦 数据库迁移

### 创建迁移

```bash
cd backend
dotnet ef migrations add MigrationName --project src/FieldTicket.Infrastructure --startup-project src/FieldTicket.Api
```

### 应用迁移

```bash
dotnet ef database update --project src/FieldTicket.Infrastructure --startup-project src/FieldTicket.Api
```

## 🐛 调试

### 后端调试

- 使用 Visual Studio 或 Rider
- 设置断点
- 查看日志（Serilog）

### 前端调试

- 使用 Chrome DevTools
- React DevTools
- Redux DevTools

### 移动端调试

- Flutter DevTools
- 使用 `flutter run --debug`
- 查看日志：`flutter logs`

## 📚 相关资源

- [.NET 8 文档](https://learn.microsoft.com/dotnet/)
- [React 文档](https://react.dev/)
- [Flutter 文档](https://flutter.dev/docs)
- [PostgreSQL 文档](https://www.postgresql.org/docs/)
- [企业微信 API 文档](https://developer.work.weixin.qq.com/document)

## ❓ 常见问题

**Q: 如何添加新的 API 端点？**
A: 在 `FieldTicket.Api/Endpoints/` 目录下创建新的端点文件，使用 Minimal API 语法。

**Q: 如何添加新的数据库表？**
A: 在 `FieldTicket.Core/Entities/` 创建实体类，然后创建迁移。

**Q: 如何添加新的前端页面？**
A: 在 `web-admin/src/pages/` 创建页面组件，在路由中注册。

---

**最后更新**：2025-12-22



