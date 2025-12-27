#!/bin/bash

#####################################################
# Docker环境数据库迁移脚本
# 用于在Docker Compose环境中执行数据库迁移
#####################################################

set -e  # 遇到错误立即退出

echo "========================================"
echo "  Docker环境 - 数据库迁移"
echo "========================================"
echo ""

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Docker配置
CONTAINER_NAME="fieldticket-postgres"
MIGRATION_FILE="003_add_employee_fields.sql"

# 检查Docker容器是否运行
if ! docker ps | grep -q $CONTAINER_NAME; then
    echo -e "${RED}错误: Docker容器 $CONTAINER_NAME 未运行${NC}"
    echo "请先启动数据库容器："
    echo "  docker-compose up -d postgres"
    exit 1
fi

# 检查迁移文件是否存在
if [ ! -f "$MIGRATION_FILE" ]; then
    echo -e "${RED}错误: 找不到迁移文件 $MIGRATION_FILE${NC}"
    exit 1
fi

echo -e "${YELLOW}检测到Docker容器: $CONTAINER_NAME${NC}"
echo ""

# 显示数据库信息
echo "========================================"
echo "  数据库信息"
echo "========================================"
echo "容器名称: $CONTAINER_NAME"
echo "迁移文件: $MIGRATION_FILE"
echo "========================================"
echo ""

read -p "确认执行迁移？ (y/n): " CONFIRM
if [ "$CONFIRM" != "y" ]; then
    echo "已取消迁移"
    exit 0
fi

echo ""
echo "开始执行迁移..."
echo ""

# 复制迁移文件到容器
echo -e "${YELLOW}复制迁移文件到容器...${NC}"
docker cp "$MIGRATION_FILE" "$CONTAINER_NAME:/tmp/$MIGRATION_FILE"

# 执行迁移
echo -e "${YELLOW}执行迁移: $MIGRATION_FILE${NC}"
if docker exec -i $CONTAINER_NAME psql -U app -d fieldticket -f "/tmp/$MIGRATION_FILE"; then
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

# 清理临时文件
echo ""
echo -e "${YELLOW}清理临时文件...${NC}"
docker exec $CONTAINER_NAME rm -f "/tmp/$MIGRATION_FILE"

echo ""
echo "提示：如果迁移已经执行过，可能会看到 'already exists' 错误，这是正常的。"
echo ""
