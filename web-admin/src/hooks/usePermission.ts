import { useMemo } from 'react';
import { authService } from '../services/authService';
import {
  hasMenuPermission,
  hasActionPermission,
  getAccessibleMenus,
  getAccessibleActions,
  isAdmin as checkIsAdmin,
  isEngineer as checkIsEngineer,
  isFieldEngineer as checkIsFieldEngineer,
  Action,
} from '../config/permissions';

/**
 * 权限管理 Hook
 * 用于在组件中检查当前用户的权限
 *
 * @example
 * ```tsx
 * const { canAccessMenu, canPerformAction, isAdmin } = usePermission();
 *
 * if (!canAccessMenu('/users')) {
 *   return <Navigate to="/" />;
 * }
 *
 * {canPerformAction(Action.Export) && <Button>导出</Button>}
 * ```
 */
export const usePermission = () => {
  const user = authService.getUser();
  const role = user?.role || '';

  // 使用 useMemo 缓存权限检查结果，避免重复计算
  const permissions = useMemo(() => {
    return {
      /**
       * 检查是否可以访问指定菜单
       * @param menuKey 菜单路径，如 '/users', '/tickets'
       */
      canAccessMenu: (menuKey: string): boolean => {
        return hasMenuPermission(role, menuKey);
      },

      /**
       * 检查是否可以执行指定操作
       * @param action 操作类型
       */
      canPerformAction: (action: Action): boolean => {
        return hasActionPermission(role, action);
      },

      /**
       * 获取所有可访问的菜单列表
       */
      accessibleMenus: getAccessibleMenus(role),

      /**
       * 获取所有可执行的操作列表
       */
      accessibleActions: getAccessibleActions(role),

      /**
       * 检查是否为管理员
       */
      isAdmin: (): boolean => {
        return checkIsAdmin(role);
      },

      /**
       * 检查是否为工程师（包括管理员）
       */
      isEngineer: (): boolean => {
        return checkIsEngineer(role);
      },

      /**
       * 检查是否为现场工程师
       */
      isFieldEngineer: (): boolean => {
        return checkIsFieldEngineer(role);
      },

      /**
       * 获取当前用户角色
       */
      role,

      /**
       * 获取当前用户信息
       */
      user,
    };
  }, [role, user]);

  return permissions;
};

/**
 * 快捷权限检查 Hook
 * 用于快速判断用户角色
 *
 * @example
 * ```tsx
 * const { isAdmin, isEngineer } = useRole();
 * ```
 */
export const useRole = () => {
  const user = authService.getUser();
  const role = user?.role || '';

  return useMemo(() => ({
    isAdmin: checkIsAdmin(role),
    isEngineer: checkIsEngineer(role),
    isFieldEngineer: checkIsFieldEngineer(role),
    role,
  }), [role]);
};
