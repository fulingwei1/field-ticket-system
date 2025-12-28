#!/bin/bash

#####################################################
# 员工批量导入功能 - 完整性验证脚本
# 用于验证所有组件是否正确安装和配置
#####################################################

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 计数器
TOTAL_CHECKS=0
PASSED_CHECKS=0
FAILED_CHECKS=0

# 检查结果数组
declare -a FAILURES

echo ""
echo "========================================"
echo "  员工批量导入功能 - 完整性验证"
echo "========================================"
echo ""

# 辅助函数：检查项目
check_item() {
    local description="$1"
    local command="$2"
    local expected="$3"

    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
    echo -n "[$TOTAL_CHECKS] $description ... "

    if eval "$command" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ PASS${NC}"
        PASSED_CHECKS=$((PASSED_CHECKS + 1))
    else
        echo -e "${RED}✗ FAIL${NC}"
        FAILED_CHECKS=$((FAILED_CHECKS + 1))
        FAILURES+=("$description")
    fi
}

# 辅助函数：检查文件存在
check_file() {
    local description="$1"
    local file_path="$2"

    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
    echo -n "[$TOTAL_CHECKS] $description ... "

    if [ -f "$file_path" ]; then
        echo -e "${GREEN}✓ 存在${NC}"
        PASSED_CHECKS=$((PASSED_CHECKS + 1))
    else
        echo -e "${RED}✗ 缺失${NC}"
        FAILED_CHECKS=$((FAILED_CHECKS + 1))
        FAILURES+=("$description - 文件不存在: $file_path")
    fi
}

# 辅助函数：检查目录存在
check_directory() {
    local description="$1"
    local dir_path="$2"

    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
    echo -n "[$TOTAL_CHECKS] $description ... "

    if [ -d "$dir_path" ]; then
        echo -e "${GREEN}✓ 存在${NC}"
        PASSED_CHECKS=$((PASSED_CHECKS + 1))
    else
        echo -e "${RED}✗ 缺失${NC}"
        FAILED_CHECKS=$((FAILED_CHECKS + 1))
        FAILURES+=("$description - 目录不存在: $dir_path")
    fi
}

echo ""
echo -e "${BLUE}=== 1. 后端组件检查 ===${NC}"
echo ""

# 检查后端核心文件
check_file "User实体扩展" "backend/src/FieldTicket.Domain/Entities/User.cs"
check_file "员工导入数据模型" "backend/src/FieldTicket.Shared/Models/EmployeeImportModels.cs"
check_file "员工导入API端点" "backend/src/FieldTicket.Api/Endpoints/EmployeeImportEndpoints.cs"
check_file "拼音转换工具类" "backend/src/FieldTicket.Infrastructure/Utils/PinyinHelper.cs"

# 检查数据库迁移
check_file "员工字段迁移SQL" "backend/scripts/003_add_employee_fields.sql"
check_file "管理员初始化脚本" "backend/scripts/init-admin.csx"
check_file "迁移脚本(Linux/Mac)" "backend/scripts/run-migration.sh"
check_file "迁移脚本(Windows)" "backend/scripts/run-migration.bat"
check_file "迁移脚本(Docker)" "backend/scripts/run-migration-docker.sh"

echo ""
echo -e "${BLUE}=== 2. 前端组件检查 ===${NC}"
echo ""

# 检查前端页面
check_file "员工导入页面" "web-admin/src/pages/users/EmployeeImport.tsx"
check_file "账户开通页面" "web-admin/src/pages/users/AccountActivation.tsx"
check_file "用户管理页面" "web-admin/src/pages/users/UserManagement.tsx"

# 检查前端服务
check_file "员工导入服务" "web-admin/src/services/employeeImportService.ts"
check_file "认证服务" "web-admin/src/services/authService.ts"
check_file "用户管理服务" "web-admin/src/services/userManagementService.ts"

# 检查路由和布局
check_file "路由配置" "web-admin/src/routes.tsx"
check_file "应用布局" "web-admin/src/components/AppLayout.tsx"

echo ""
echo -e "${BLUE}=== 3. 测试数据检查 ===${NC}"
echo ""

# 检查测试数据
check_directory "测试数据目录" "backend/scripts/test-data"
check_file "正常数据测试集" "backend/scripts/test-data/test-normal.csv"
check_file "边界情况测试集" "backend/scripts/test-data/test-edge-cases.csv"
check_file "错误场景测试集" "backend/scripts/test-data/test-errors.csv"
check_file "员工导入示例" "backend/scripts/员工导入示例.csv"

echo ""
echo -e "${BLUE}=== 4. 文档检查 ===${NC}"
echo ""

# 检查文档
check_file "完整使用指南" "EMPLOYEE_IMPORT_GUIDE.md"
check_file "快速启动指南" "EMPLOYEE_IMPORT_QUICKSTART.md"
check_file "测试指南" "TESTING_GUIDE.md"
check_file "启动测试检查清单" "STARTUP_AND_TESTING_CHECKLIST.md"
check_file "Web管理端快速启动" "WEB_ADMIN_QUICK_START.md"

echo ""
echo -e "${BLUE}=== 5. 关键代码片段验证 ===${NC}"
echo ""

# 检查关键代码是否包含必要的字段和逻辑
TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
echo -n "[$TOTAL_CHECKS] User实体包含IsActivated字段 ... "
if grep -q "IsActivated" backend/src/FieldTicket.Domain/Entities/User.cs 2>/dev/null; then
    echo -e "${GREEN}✓ 存在${NC}"
    PASSED_CHECKS=$((PASSED_CHECKS + 1))
else
    echo -e "${RED}✗ 缺失${NC}"
    FAILED_CHECKS=$((FAILED_CHECKS + 1))
    FAILURES+=("User实体缺少IsActivated字段")
fi

TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
echo -n "[$TOTAL_CHECKS] User实体包含SupervisorId字段 ... "
if grep -q "SupervisorId" backend/src/FieldTicket.Domain/Entities/User.cs 2>/dev/null; then
    echo -e "${GREEN}✓ 存在${NC}"
    PASSED_CHECKS=$((PASSED_CHECKS + 1))
else
    echo -e "${RED}✗ 缺失${NC}"
    FAILED_CHECKS=$((FAILED_CHECKS + 1))
    FAILURES+=("User实体缺少SupervisorId字段")
fi

TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
echo -n "[$TOTAL_CHECKS] PinyinHelper包含GeneratePassword方法 ... "
if grep -q "GeneratePassword" backend/src/FieldTicket.Infrastructure/Utils/PinyinHelper.cs 2>/dev/null; then
    echo -e "${GREEN}✓ 存在${NC}"
    PASSED_CHECKS=$((PASSED_CHECKS + 1))
else
    echo -e "${RED}✗ 缺失${NC}"
    FAILED_CHECKS=$((FAILED_CHECKS + 1))
    FAILURES+=("PinyinHelper缺少GeneratePassword方法")
fi

TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
echo -n "[$TOTAL_CHECKS] EmployeeImportEndpoints包含激活端点 ... "
if grep -q "activateAccount\|ActivateAccount" backend/src/FieldTicket.Api/Endpoints/EmployeeImportEndpoints.cs 2>/dev/null; then
    echo -e "${GREEN}✓ 存在${NC}"
    PASSED_CHECKS=$((PASSED_CHECKS + 1))
else
    echo -e "${RED}✗ 缺失${NC}"
    FAILED_CHECKS=$((FAILED_CHECKS + 1))
    FAILURES+=("EmployeeImportEndpoints缺少激活账户端点")
fi

TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
echo -n "[$TOTAL_CHECKS] 前端包含权限检查(isAdmin) ... "
if grep -q "isAdmin" web-admin/src/pages/users/EmployeeImport.tsx 2>/dev/null; then
    echo -e "${GREEN}✓ 存在${NC}"
    PASSED_CHECKS=$((PASSED_CHECKS + 1))
else
    echo -e "${RED}✗ 缺失${NC}"
    FAILED_CHECKS=$((FAILED_CHECKS + 1))
    FAILURES+=("员工导入页面缺少权限检查")
fi

echo ""
echo "========================================"
echo "  验证结果汇总"
echo "========================================"
echo ""
echo "总检查项: $TOTAL_CHECKS"
echo -e "${GREEN}通过: $PASSED_CHECKS${NC}"
echo -e "${RED}失败: $FAILED_CHECKS${NC}"
echo ""

if [ $FAILED_CHECKS -eq 0 ]; then
    echo -e "${GREEN}✅ 所有检查通过！员工批量导入功能组件完整。${NC}"
    echo ""
    echo -e "${YELLOW}下一步操作：${NC}"
    echo "1. 启动数据库服务（PostgreSQL）"
    echo "2. 运行数据库迁移脚本: cd backend/scripts && ./run-migration.sh"
    echo "3. 初始化管理员账户: cd backend/scripts && dotnet script init-admin.csx"
    echo "4. 启动后端服务: cd backend && dotnet run --project src/FieldTicket.Api"
    echo "5. 安装前端依赖: cd web-admin && npm install"
    echo "6. 启动前端服务: cd web-admin && npm run dev"
    echo "7. 访问 http://localhost:5173 开始测试"
    echo ""
    echo "详细说明请参考："
    echo "  - STARTUP_AND_TESTING_CHECKLIST.md - 完整启动测试清单"
    echo "  - TESTING_GUIDE.md - 详细测试指南"
    echo ""
    exit 0
else
    echo -e "${RED}❌ 发现 $FAILED_CHECKS 个问题：${NC}"
    echo ""
    for failure in "${FAILURES[@]}"; do
        echo -e "${RED}  ✗ $failure${NC}"
    done
    echo ""
    echo "请检查并修复以上问题后重新运行验证。"
    echo ""
    exit 1
fi
