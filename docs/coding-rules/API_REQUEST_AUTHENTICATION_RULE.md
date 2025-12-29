# API 请求认证规则（铁律）

## 🚨 核心规则

**所有需要认证的 API 请求必须使用 `authService.getAuthHeaders()` 获取请求头，严禁直接构建 headers 对象。**

## 📋 规则详情

### ✅ 正确做法

```typescript
// ✅ 方式 1：使用 authService.getAuthHeaders()（推荐）
import { authService } from './authService';

const headers = authService.getAuthHeaders();
const response = await fetch(`${API_BASE_URL}${endpoint}`, {
  ...options,
  headers: {
    ...headers,  // 自动包含 Authorization: Bearer {token}
    ...(options.headers || {}),
  },
});
```

```typescript
// ✅ 方式 2：如果必须手动构建，确保包含 Authorization
const token = authService.getToken();
if (!token) {
  throw new Error('未登录，请先登录');
}
const response = await fetch(`${API_BASE_URL}${endpoint}`, {
  ...options,
  headers: {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,  // ✅ 必须包含
    ...(options.headers || {}),
  },
});
```

### ❌ 错误做法

```typescript
// ❌ 错误：缺少 Authorization 头
const response = await fetch(`${API_BASE_URL}${endpoint}`, {
  headers: {
    'Content-Type': 'application/json',
    // ❌ 缺少 Authorization: Bearer {token}
  },
});
```

```typescript
// ❌ 错误：硬编码 headers，没有从 authService 获取
const response = await fetch(`${API_BASE_URL}${endpoint}`, {
  headers: {
    'Content-Type': 'application/json',
    // ❌ 没有 Authorization 头
  },
});
```

## 🔍 检查清单

在创建或修改任何 API 请求代码时，必须检查：

- [ ] 是否调用了 `authService.getAuthHeaders()` 或 `authService.getToken()`？
- [ ] 请求头中是否包含 `Authorization: Bearer {token}`？
- [ ] 是否处理了 token 不存在的情况？
- [ ] 是否处理了 401 错误（token 过期）的情况？
- [ ] 是否与 `ticketService` 的实现保持一致？

## 📝 代码审查要点

### 1. 所有 Service 类必须遵循

```typescript
class XxxService {
  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    // ✅ 必须使用 authService.getAuthHeaders()
    const headers = authService.getAuthHeaders();
    
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        ...headers,  // 确保 Authorization 头被包含
        ...(options.headers || {}),
      },
    });
    
    // 错误处理...
    return response.json();
  }
}
```

### 2. 禁止的模式

```typescript
// ❌ 禁止：只设置 Content-Type，缺少 Authorization
headers: {
  'Content-Type': 'application/json',
}

// ❌ 禁止：忘记添加 Authorization
const response = await fetch(url, {
  headers: {
    'Content-Type': 'application/json',
  },
});
```

## 🛡️ 防护措施

### 1. 统一使用 authService.getAuthHeaders()

**原因：**
- `authService.getAuthHeaders()` 会自动检查 token 是否存在
- 如果 token 不存在，不会添加 Authorization 头（避免无效请求）
- 统一管理认证逻辑，便于维护

### 2. 错误处理

所有 API 请求必须处理以下情况：
- **401 Unauthorized**: Token 过期或无效，应尝试刷新 token
- **Token 不存在**: 应提示用户重新登录
- **网络错误**: 应显示友好的错误提示

### 3. 代码审查检查点

在代码审查时，必须检查：
1. 所有 `/api/` 开头的请求是否包含 Authorization 头
2. 是否使用了 `authService.getAuthHeaders()` 或正确处理了 token
3. Network 标签页中是否能看到 Authorization 头

## 📊 问题影响

### 问题症状
- API 请求返回 401 Unauthorized
- 页面显示 "No data" 或加载失败
- 用户已登录但无法获取数据

### 根本原因
- 请求头中缺少 `Authorization: Bearer {token}`
- 后端认证中间件拒绝未认证的请求

## 🔧 修复步骤

如果发现某个 Service 存在此问题：

1. **立即修复**
   ```typescript
   // 修改前
   headers: {
     'Content-Type': 'application/json',
   }
   
   // 修改后
   const headers = authService.getAuthHeaders();
   // 或
   headers: {
     'Content-Type': 'application/json',
     Authorization: `Bearer ${authService.getToken()}`,
   }
   ```

2. **验证修复**
   - 清除浏览器缓存
   - 检查 Network 标签页中的 Request Headers
   - 确认包含 `Authorization: Bearer {token}`
   - 确认 API 返回 200 而不是 401

3. **重新构建**
   ```bash
   docker compose build --no-cache web
   docker compose up -d web
   ```

## 📚 参考实现

### 标准实现（推荐）

参考 `ticketService.ts` 和修复后的 `customerService.ts`：

```typescript
import { authService } from './authService';

class XxxService {
  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const headers = authService.getAuthHeaders();
    
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        ...headers,
        ...(options.headers || {}),
      },
    });
    
    if (!response.ok) {
      if (response.status === 401) {
        // 处理 token 过期
        try {
          await authService.refreshToken();
          const retryHeaders = authService.getAuthHeaders();
          const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, {
            ...options,
            headers: {
              ...retryHeaders,
              ...(options.headers || {}),
            },
          });
          if (!retryResponse.ok) {
            throw new Error('认证失败，请重新登录');
          }
          return retryResponse.json();
        } catch {
          authService.clearAuth();
          window.location.href = '/login';
          throw new Error('认证失败，请重新登录');
        }
      }
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    return response.json();
  }
}
```

## ⚠️ 重要提醒

1. **永远不要**在需要认证的 API 请求中省略 Authorization 头
2. **永远不要**硬编码 headers，必须使用 `authService.getAuthHeaders()`
3. **永远不要**假设 token 一定存在，必须检查和处理
4. **永远要**在 Network 标签页中验证 Authorization 头是否被正确发送

## 📅 规则生效日期

2025-12-28

## 🔄 规则维护

- 此规则应纳入代码审查清单
- 新创建的 Service 必须遵循此规则
- 修改现有 Service 时必须检查是否符合此规则

## 🔧 自动化检查

### 使用检查脚本

```bash
# 运行检查脚本
bash scripts/check-api-authentication.sh
```

脚本会检查：
- ✅ 是否使用了 `authService.getAuthHeaders()` 或 `authService.getToken()`
- ✅ 是否包含 `Authorization` 头
- ⚠️  是否直接使用 `localStorage.getItem('token')`
- ⚠️  API_BASE_URL 端口是否正确（应为 5001）

### 在 CI/CD 中集成

建议在代码提交前运行此脚本，确保所有 API 请求都符合规则。

## 📦 统一 HTTP Client（推荐）

为了彻底避免此问题，建议所有新代码使用统一的 HTTP Client：

```typescript
// 使用统一的 httpClient
import { httpClient } from '../utils/httpClient';

// GET 请求
const customers = await httpClient.get<CustomerDto[]>('/api/customers');

// POST 请求
const result = await httpClient.post('/api/tickets', { ... });
```

`httpClient` 已经内置了：
- ✅ 自动添加 Authorization 头
- ✅ Token 刷新机制
- ✅ 统一的错误处理
- ✅ 401 错误自动处理

## 📊 当前状态

- ✅ `customerService.ts` - 已修复
- ✅ `ticketService.ts` - 正确实现
- ✅ `deviceService.ts` - 正确实现
- ✅ `excelImportService.ts` - 已修复
- ✅ 所有其他服务 - 已检查并确认正确
