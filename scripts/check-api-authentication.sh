#!/bin/bash

# API 请求认证检查脚本
# 用于检查所有服务文件是否正确使用认证头

echo "🔍 检查 API 请求认证规则..."
echo ""

ERRORS=0
WARNINGS=0

# 检查所有 service 文件
for file in web-admin/src/services/*.ts; do
    if [ ! -f "$file" ]; then
        continue
    fi
    
    filename=$(basename "$file")
    
    # 跳过 authService.ts（认证服务本身）
    if [ "$filename" = "authService.ts" ]; then
        continue
    fi
    
    # 检查是否包含 fetch 调用
    if ! grep -q "fetch.*API_BASE_URL\|fetch.*api/" "$file"; then
        continue
    fi
    
    echo "检查: $filename"
    
    # 检查 1: 是否使用了 authService.getAuthHeaders() 或 authService.getToken()
    if grep -q "authService\.getAuthHeaders\|authService\.getToken" "$file"; then
        echo "  ✅ 使用了 authService 获取认证信息"
    else
        # 检查是否直接使用 localStorage.getItem('token') 但至少包含了 Authorization
        if grep -q "localStorage.getItem.*token" "$file" && grep -q "Authorization.*Bearer" "$file"; then
            echo "  ⚠️  直接使用 localStorage，建议改用 authService.getAuthHeaders()"
            WARNINGS=$((WARNINGS + 1))
        else
            echo "  ❌ 未使用 authService 获取认证信息"
            ERRORS=$((ERRORS + 1))
        fi
    fi
    
    # 检查 2: 是否直接使用 localStorage.getItem('token')
    if grep -q "localStorage.getItem('token')\|localStorage.getItem(\"token\")" "$file"; then
        echo "  ⚠️  直接使用 localStorage.getItem('token')，应使用 authService.getToken()"
        WARNINGS=$((WARNINGS + 1))
    fi
    
    # 检查 3: 是否包含 Authorization 头
    if grep -q "Authorization.*Bearer\|getAuthHeaders" "$file"; then
        echo "  ✅ 包含 Authorization 头"
    else
        # 检查是否有硬编码的 headers 但没有 Authorization
        if grep -q "headers.*Content-Type.*application/json" "$file" && ! grep -q "Authorization" "$file"; then
            echo "  ❌ 有 headers 但缺少 Authorization 头"
            ERRORS=$((ERRORS + 1))
        fi
    fi
    
    # 检查 4: API_BASE_URL 是否正确
    if grep -q "localhost:5000" "$file"; then
        echo "  ⚠️  API_BASE_URL 使用了错误的端口 5000，应为 5001"
        WARNINGS=$((WARNINGS + 1))
    fi
    
    echo ""
done

echo "=========================================="
echo "检查完成"
echo "错误: $ERRORS"
echo "警告: $WARNINGS"
echo "=========================================="

if [ $ERRORS -gt 0 ]; then
    echo "❌ 发现 $ERRORS 个错误，请修复后再提交代码"
    exit 1
elif [ $WARNINGS -gt 0 ]; then
    echo "⚠️  发现 $WARNINGS 个警告，建议修复"
    exit 0
else
    echo "✅ 所有检查通过"
    exit 0
fi

