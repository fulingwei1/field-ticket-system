import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { Result, Button } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { authService } from '../services/authService';
import { usePermission } from '../hooks/usePermission';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRoles?: string[];
  requiredMenu?: string;
  fallbackPath?: string;
  showForbidden?: boolean;
}

/**
 * 受保护的路由组件
 * 用于限制特定角色才能访问的页面
 *
 * @example
 * ```tsx
 * // 仅Admin可访问
 * <Route path="/users" element={
 *   <ProtectedRoute requiredRoles={['Admin']}>
 *     <UserManagement />
 *   </ProtectedRoute>
 * } />
 *
 * // 基于菜单权限
 * <Route path="/users" element={
 *   <ProtectedRoute requiredMenu="/users">
 *     <UserManagement />
 *   </ProtectedRoute>
 * } />
 * ```
 */
export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredRoles,
  requiredMenu,
  fallbackPath = '/',
  showForbidden = true,
}) => {
  const location = useLocation();
  const { canAccessMenu, role } = usePermission();

  // 检查是否已登录
  if (!authService.isAuthenticated()) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // 检查角色权限
  if (requiredRoles && requiredRoles.length > 0) {
    const hasRole = authService.hasAnyRole(requiredRoles);
    if (!hasRole) {
      if (showForbidden) {
        return (
          <Result
            status="403"
            title="403"
            subTitle="抱歉，您没有权限访问此页面。"
            icon={<LockOutlined />}
            extra={
              <Button type="primary" onClick={() => window.location.href = fallbackPath}>
                返回首页
              </Button>
            }
          />
        );
      }
      return <Navigate to={fallbackPath} replace />;
    }
  }

  // 检查菜单权限
  if (requiredMenu) {
    const hasAccess = canAccessMenu(requiredMenu);
    if (!hasAccess) {
      if (showForbidden) {
        return (
          <Result
            status="403"
            title="403"
            subTitle={`当前角色 (${role}) 没有权限访问此功能。`}
            icon={<LockOutlined />}
            extra={
              <Button type="primary" onClick={() => window.location.href = fallbackPath}>
                返回首页
              </Button>
            }
          />
        );
      }
      return <Navigate to={fallbackPath} replace />;
    }
  }

  return <>{children}</>;
};

/**
 * 仅管理员可访问的路由
 */
export const AdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  return (
    <ProtectedRoute requiredRoles={['Admin']}>
      {children}
    </ProtectedRoute>
  );
};

/**
 * 工程师及以上可访问的路由
 */
export const EngineerRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Engineer']}>
      {children}
    </ProtectedRoute>
  );
};
