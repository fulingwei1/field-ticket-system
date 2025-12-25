#!/bin/bash
# 运行测试脚本

set -e

echo "=========================================="
echo "运行 Field Ticket 测试套件"
echo "=========================================="
echo ""

# 颜色定义
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 检查是否安装了 dotnet
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}错误: 未找到 dotnet 命令${NC}"
    exit 1
fi

echo -e "${YELLOW}1. 恢复依赖...${NC}"
dotnet restore

echo ""
echo -e "${YELLOW}2. 构建项目...${NC}"
dotnet build --no-restore

echo ""
echo -e "${YELLOW}3. 运行所有测试...${NC}"
dotnet test --no-build --verbosity normal

echo ""
echo -e "${YELLOW}4. 生成代码覆盖率报告...${NC}"
dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory:./TestResults

echo ""
echo -e "${GREEN}=========================================="
echo "测试完成！"
echo "==========================================${NC}"

