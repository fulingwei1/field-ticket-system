#!/bin/bash

#####################################################
# 数据库迁移脚本
# 用于执行员工管理相关的数据库结构更新
#####################################################

set -e  # 遇到错误立即退出

echo "========================================"
echo "  现场工单系统 - 数据库迁移"
echo "========================================"
echo ""

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 默认数据库配置
DEFAULT_HOST="localhost"
DEFAULT_PORT="5432"
DEFAULT_DB="fieldticket"
DEFAULT_USER="app"

# 读取配置
read -p "数据库主机地址 [默认: $DEFAULT_HOST]: " DB_HOST
DB_HOST=${DB_HOST:-$DEFAULT_HOST}

read -p "数据库端口 [默认: $DEFAULT_PORT]: " DB_PORT
DB_PORT=${DB_PORT:-$DEFAULT_PORT}

read -p "数据库名称 [默认: $DEFAULT_DB]: " DB_NAME
DB_NAME=${DB_NAME:-$DEFAULT_DB}

read -p "数据库用户名 [默认: $DEFAULT_USER]: " DB_USER
DB_USER=${DB_USER:-$DEFAULT_USER}

read -sp "数据库密码: " DB_PASSWORD
echo ""

# 确认信息
echo ""
echo "========================================"
echo "  数据库连接信息"
echo "========================================"
echo "主机: $DB_HOST"
echo "端口: $DB_PORT"
echo "数据库: $DB_NAME"
echo "用户: $DB_USER"
echo "========================================"
echo ""

read -p "确认执行迁移？ (y/n): " CONFIRM
if [ "$CONFIRM" != "y" ]; then
    echo "已取消迁移"
    exit 0
fi

# 设置环境变量
export PGPASSWORD=$DB_PASSWORD

# 执行迁移脚本
echo ""
echo "开始执行迁移..."
echo ""

MIGRATION_FILE="003_add_employee_fields.sql"

if [ ! -f "$MIGRATION_FILE" ]; then
    echo -e "${RED}错误: 找不到迁移文件 $MIGRATION_FILE${NC}"
    exit 1
fi

echo -e "${YELLOW}执行迁移: $MIGRATION_FILE${NC}"

# 执行SQL文件
if psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$MIGRATION_FILE"; then
    echo -e "${GREEN}✓ 迁移执行成功！${NC}"
    echo ""
    echo "========================================"
    echo "  迁移完成"
    echo "========================================"
    echo ""
    echo "已添加以下字段到 Users 表："
    echo "  • DeptName - 部门名称"
    echo "  • SupervisorId - 上级用户ID"
    echo "  • SupervisorName - 上级姓名"
    echo "  • IdCardLastFour - 身份证后4位"
    echo "  • IsActivated - 账户开通状态"
    echo ""
    echo "已创建索引和外键约束"
    echo ""
    echo -e "${GREEN}现在可以使用员工批量导入功能了！${NC}"
else
    echo -e "${RED}✗ 迁移执行失败！${NC}"
    echo "请检查错误信息并重试"
    exit 1
fi

# 清除密码环境变量
unset PGPASSWORD

echo ""
echo "提示：如果迁移已经执行过，可能会看到 'already exists' 错误，这是正常的。"
echo ""
