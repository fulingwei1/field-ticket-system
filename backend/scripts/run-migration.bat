@echo off
REM ====================================================
REM 数据库迁移脚本 (Windows版本)
REM 用于执行员工管理相关的数据库结构更新
REM ====================================================

echo ========================================
echo   现场工单系统 - 数据库迁移
echo ========================================
echo.

REM 默认数据库配置
set DEFAULT_HOST=localhost
set DEFAULT_PORT=5432
set DEFAULT_DB=fieldticket
set DEFAULT_USER=app

REM 读取配置
set /p DB_HOST="数据库主机地址 [默认: %DEFAULT_HOST%]: "
if "%DB_HOST%"=="" set DB_HOST=%DEFAULT_HOST%

set /p DB_PORT="数据库端口 [默认: %DEFAULT_PORT%]: "
if "%DB_PORT%"=="" set DB_PORT=%DEFAULT_PORT%

set /p DB_NAME="数据库名称 [默认: %DEFAULT_DB%]: "
if "%DB_NAME%"=="" set DB_NAME=%DEFAULT_DB%

set /p DB_USER="数据库用户名 [默认: %DEFAULT_USER%]: "
if "%DB_USER%"=="" set DB_USER=%DEFAULT_USER%

set /p DB_PASSWORD="数据库密码: "

REM 确认信息
echo.
echo ========================================
echo   数据库连接信息
echo ========================================
echo 主机: %DB_HOST%
echo 端口: %DB_PORT%
echo 数据库: %DB_NAME%
echo 用户: %DB_USER%
echo ========================================
echo.

set /p CONFIRM="确认执行迁移？ (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo 已取消迁移
    exit /b 0
)

REM 设置环境变量
set PGPASSWORD=%DB_PASSWORD%

REM 执行迁移脚本
echo.
echo 开始执行迁移...
echo.

set MIGRATION_FILE=003_add_employee_fields.sql

if not exist "%MIGRATION_FILE%" (
    echo [错误] 找不到迁移文件 %MIGRATION_FILE%
    exit /b 1
)

echo 执行迁移: %MIGRATION_FILE%

REM 执行SQL文件
psql -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -f %MIGRATION_FILE%

if %ERRORLEVEL% equ 0 (
    echo [成功] 迁移执行成功！
    echo.
    echo ========================================
    echo   迁移完成
    echo ========================================
    echo.
    echo 已添加以下字段到 Users 表：
    echo   • DeptName - 部门名称
    echo   • SupervisorId - 上级用户ID
    echo   • SupervisorName - 上级姓名
    echo   • IdCardLastFour - 身份证后4位
    echo   • IsActivated - 账户开通状态
    echo.
    echo 已创建索引和外键约束
    echo.
    echo [成功] 现在可以使用员工批量导入功能了！
) else (
    echo [失败] 迁移执行失败！
    echo 请检查错误信息并重试
    exit /b 1
)

REM 清除密码环境变量
set PGPASSWORD=

echo.
echo 提示：如果迁移已经执行过，可能会看到 'already exists' 错误，这是正常的。
echo.

pause
