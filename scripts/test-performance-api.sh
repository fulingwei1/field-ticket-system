#!/bin/bash

# 绩效管理系统 API 测试脚本
# 使用方法: ./scripts/test-performance-api.sh [API_URL] [TOKEN]

set -e

API_URL="${1:-http://localhost:5000}"
TOKEN="${2:-}"

if [ -z "$TOKEN" ]; then
    echo "错误: 请提供 JWT Token"
    echo "使用方法: ./scripts/test-performance-api.sh [API_URL] [TOKEN]"
    echo "示例: ./scripts/test-performance-api.sh http://localhost:5000 eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    exit 1
fi

echo "=========================================="
echo "  绩效管理系统 API 测试"
echo "=========================================="
echo "API 地址: $API_URL"
echo ""

PASSED=0
FAILED=0

# 测试函数
test_api() {
    local name="$1"
    local method="$2"
    local endpoint="$3"
    local data="$4"
    local expected_status="${5:-200}"
    
    echo -n "测试 $name ... "
    
    if [ -n "$data" ]; then
        response=$(curl -s -w "\n%{http_code}" -X "$method" \
            "$API_URL$endpoint" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "$data")
    else
        response=$(curl -s -w "\n%{http_code}" -X "$method" \
            "$API_URL$endpoint" \
            -H "Authorization: Bearer $TOKEN")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" = "$expected_status" ]; then
        echo "✓ 通过 (HTTP $http_code)"
        ((PASSED++))
        return 0
    else
        echo "✗ 失败 (HTTP $http_code, 预期 $expected_status)"
        echo "  响应: $body"
        ((FAILED++))
        return 1
    fi
}

# 获取一个工程师ID（用于测试）
echo "1. 获取用户信息（用于获取工程师ID）"
user_response=$(curl -s -X GET "$API_URL/api/auth/me" \
    -H "Authorization: Bearer $TOKEN")

ENGINEER_ID=$(echo "$user_response" | grep -o '"id":"[^"]*' | head -1 | cut -d'"' -f4)

if [ -z "$ENGINEER_ID" ]; then
    echo "警告: 无法获取工程师ID，部分测试将跳过"
    ENGINEER_ID="00000000-0000-0000-0000-000000000000"
fi

echo "工程师ID: $ENGINEER_ID"
echo ""

# Phase 1: 绩效计算功能测试
echo "=========================================="
echo "  Phase 1: 绩效计算功能"
echo "=========================================="
echo ""

# 测试 1.1: 手动触发绩效计算（需要管理员权限）
echo "1.1 手动触发绩效计算"
PERIOD_START=$(date -u -v-1m +"%Y-%m-01" 2>/dev/null || date -u -d "1 month ago" +"%Y-%m-01" 2>/dev/null || echo "2025-12-01")
PERIOD_END=$(date -u +"%Y-%m-%d" 2>/dev/null || echo "2025-12-31")

test_api \
    "手动触发绩效计算" \
    "POST" \
    "/api/performance/calculate" \
    "{\"engineerId\":\"$ENGINEER_ID\",\"periodType\":\"monthly\",\"periodStart\":\"$PERIOD_START\",\"periodEnd\":\"$PERIOD_END\"}" \
    "200"

echo ""

# 测试 1.2: 获取工程师绩效
echo "1.2 获取工程师绩效"
test_api \
    "获取工程师绩效" \
    "GET" \
    "/api/performance/engineer/$ENGINEER_ID?periodType=monthly&periodStart=$PERIOD_START" \
    "" \
    "200"

echo ""

# 测试 1.3: 获取团队绩效
echo "1.3 获取团队绩效"
test_api \
    "获取团队绩效" \
    "GET" \
    "/api/performance/team?periodType=monthly&periodStart=$PERIOD_START" \
    "" \
    "200"

echo ""

# 测试 1.4: 获取绩效排名
echo "1.4 获取绩效排名"
test_api \
    "获取绩效排名" \
    "GET" \
    "/api/performance/ranking?periodType=monthly&periodStart=$PERIOD_START" \
    "" \
    "200"

echo ""

# 测试 1.5: 获取绩效趋势
echo "1.5 获取绩效趋势"
FROM_DATE=$(date -u -v-3m +"%Y-%m-01" 2>/dev/null || date -u -d "3 months ago" +"%Y-%m-01" 2>/dev/null || echo "2025-09-01")
TO_DATE=$(date -u +"%Y-%m-%d" 2>/dev/null || echo "2025-12-31")

test_api \
    "获取绩效趋势" \
    "GET" \
    "/api/performance/trends?engineerId=$ENGINEER_ID&periodType=monthly&fromDate=$FROM_DATE&toDate=$TO_DATE" \
    "" \
    "200"

echo ""

# Phase 2: AI 分析功能测试
echo "=========================================="
echo "  Phase 2: AI 分析功能"
echo "=========================================="
echo ""

# 测试 2.1: 生成每日工作总结
echo "2.1 生成每日工作总结"
ANALYSIS_DATE=$(date -u +"%Y-%m-%d" 2>/dev/null || echo "2025-12-22")

test_api \
    "生成每日工作总结" \
    "POST" \
    "/api/ai-analysis/daily-summary" \
    "{\"engineerId\":\"$ENGINEER_ID\",\"analysisDate\":\"$ANALYSIS_DATE\"}" \
    "200"

echo ""

# 测试 2.2: 生成每周总结
echo "2.2 生成每周总结"
WEEK_START=$(date -u -v-Monday +"%Y-%m-%d" 2>/dev/null || date -u -d "last monday" +"%Y-%m-%d" 2>/dev/null || echo "2025-12-15")

test_api \
    "生成每周总结" \
    "POST" \
    "/api/ai-analysis/weekly-summary" \
    "{\"engineerId\":\"$ENGINEER_ID\",\"weekStart\":\"$WEEK_START\"}" \
    "200"

echo ""

# 测试 2.3: 生成团队分析
echo "2.3 生成团队分析"
test_api \
    "生成团队分析" \
    "POST" \
    "/api/ai-analysis/team-analysis" \
    "{\"analysisDate\":\"$ANALYSIS_DATE\",\"periodType\":\"monthly\"}" \
    "200"

echo ""

# 测试 2.4: 生成人员安排建议
echo "2.4 生成人员安排建议"
test_api \
    "生成人员安排建议" \
    "POST" \
    "/api/ai-analysis/scheduling-suggestion" \
    "{\"analysisDate\":\"$ANALYSIS_DATE\"}" \
    "200"

echo ""

# 测试 2.5: 获取分析结果列表
echo "2.5 获取分析结果列表"
test_api \
    "获取分析结果列表" \
    "GET" \
    "/api/ai-analysis/results?page=1&pageSize=20" \
    "" \
    "200"

echo ""

# 测试结果汇总
echo "=========================================="
echo "  测试结果"
echo "=========================================="
echo "通过: $PASSED"
echo "失败: $FAILED"
echo ""

if [ $FAILED -eq 0 ]; then
    echo "✓ 所有测试通过！"
    exit 0
else
    echo "✗ 有 $FAILED 个测试失败"
    exit 1
fi


