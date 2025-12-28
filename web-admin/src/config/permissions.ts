/**
 * 角色权限配置
 * 定义不同角色可访问的菜单和操作权限
 */

export enum Role {
  Admin = 'Admin',
  Engineer = 'Engineer',
  FieldEngineer = 'FieldEngineer',
}

export enum Action {
  Create = 'create',
  Read = 'read',
  Update = 'update',
  Delete = 'delete',
  Export = 'export',
  Import = 'import',
  Approve = 'approve',
  Assign = 'assign',
}

interface RolePermission {
  menus: string[];
  actions: Action[];
}

/**
 * 角色权限映射
 * 定义每个角色可访问的菜单路径和可执行的操作
 */
export const ROLE_PERMISSIONS: Record<Role, RolePermission> = {
  // 管理员 - 完全权限
  [Role.Admin]: {
    menus: [
      '/',
      '/dashboard',
      '/tickets',
      '/tickets/kanban',
      '/tickets/new',
      '/tickets/templates',
      '/performance',
      '/performance/my',
      '/performance/team',
      '/performance/load-stats',
      '/performance/growth-curve',
      '/statistics',
      '/corrective-actions',
      '/ai-analysis',
      '/judgement-cards',
      '/judgement-cards/quality',
      '/judgement-cards/version',
      '/customers',
      '/devices',
      '/devices/config-snapshot',
      '/devices/qrcode-generator',
      '/projects',
      '/projects/excel-import',
      '/projects/statistics',
      '/knowledge-graph',
      '/notification-rules',
      '/confidence/calibration',
      '/thresholds',
      '/user-profile',
      '/users',
      '/users/import',
      '/users/update',
      '/users/activate',
      '/users/logs',
    ],
    actions: [
      Action.Create,
      Action.Read,
      Action.Update,
      Action.Delete,
      Action.Export,
      Action.Import,
      Action.Approve,
      Action.Assign,
    ],
  },

  // 工程师 - 核心业务权限
  [Role.Engineer]: {
    menus: [
      '/',
      '/dashboard',
      '/tickets',
      '/tickets/kanban',
      '/tickets/new',
      '/tickets/templates',
      '/performance',
      '/performance/my',
      '/performance/team',
      '/statistics',
      '/corrective-actions',
      '/ai-analysis',
      '/judgement-cards',
      '/judgement-cards/quality',
      '/judgement-cards/version',
      '/customers',
      '/devices',
      '/devices/config-snapshot',
      '/devices/qrcode-generator',
      '/projects',
      '/projects/statistics',
      '/knowledge-graph',
    ],
    actions: [
      Action.Create,
      Action.Read,
      Action.Update,
      Action.Export,
      Action.Assign,
    ],
  },

  // 现场工程师 - 仅基础操作权限
  [Role.FieldEngineer]: {
    menus: [
      '/',
      '/dashboard',
      '/tickets',
      '/tickets/new',
      '/performance/my',
      '/devices',
      '/devices/qrcode-generator',
    ],
    actions: [
      Action.Create,
      Action.Read,
    ],
  },
};

/**
 * 检查角色是否有菜单访问权限
 */
export const hasMenuPermission = (role: string, menuKey: string): boolean => {
  const permissions = ROLE_PERMISSIONS[role as Role];
  if (!permissions) return false;

  // 检查精确匹配
  if (permissions.menus.includes(menuKey)) return true;

  // 检查父路径匹配（例如 /tickets/:id 应该匹配 /tickets）
  return permissions.menus.some(allowedPath => {
    if (menuKey.startsWith(allowedPath + '/')) return true;
    return false;
  });
};

/**
 * 检查角色是否有操作权限
 */
export const hasActionPermission = (role: string, action: Action): boolean => {
  const permissions = ROLE_PERMISSIONS[role as Role];
  if (!permissions) return false;
  return permissions.actions.includes(action);
};

/**
 * 获取角色可访问的菜单列表
 */
export const getAccessibleMenus = (role: string): string[] => {
  const permissions = ROLE_PERMISSIONS[role as Role];
  return permissions?.menus || [];
};

/**
 * 获取角色可执行的操作列表
 */
export const getAccessibleActions = (role: string): Action[] => {
  const permissions = ROLE_PERMISSIONS[role as Role];
  return permissions?.actions || [];
};

/**
 * 检查是否为管理员
 */
export const isAdmin = (role: string): boolean => {
  return role === Role.Admin;
};

/**
 * 检查是否为工程师（包括高级工程师）
 */
export const isEngineer = (role: string): boolean => {
  return role === Role.Engineer || role === Role.Admin;
};

/**
 * 检查是否为现场工程师
 */
export const isFieldEngineer = (role: string): boolean => {
  return role === Role.FieldEngineer;
};
