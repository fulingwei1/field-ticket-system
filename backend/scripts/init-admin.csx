#!/usr/bin/env dotnet script
// 管理员用户初始化脚本
// 用法: dotnet script init-admin.csx [username] [password]
// 示例: dotnet script init-admin.csx admin Admin123!

#r "nuget: BCrypt.Net-Next, 4.0.3"
#r "nuget: Npgsql, 8.0.1"

using BCrypt.Net;
using Npgsql;
using System;

// 默认配置
var defaultUsername = Args.Length > 0 ? Args[0] : "admin";
var defaultPassword = Args.Length > 1 ? Args[1] : "Admin123!";
var defaultName = "系统管理员";
var defaultEmail = "admin@example.com";

// 从环境变量或配置读取数据库连接字符串
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Host=localhost;Database=field_ticket;Username=postgres;Password=postgres";

Console.WriteLine("========================================");
Console.WriteLine("  现场工单系统 - 管理员初始化工具");
Console.WriteLine("========================================");
Console.WriteLine();

try
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    Console.WriteLine($"✅ 已连接到数据库");
    Console.WriteLine();

    // 检查用户是否已存在
    using (var checkCmd = new NpgsqlCommand(
        @"SELECT COUNT(*) FROM ""Users"" WHERE ""Username"" = @username", conn))
    {
        checkCmd.Parameters.AddWithValue("username", defaultUsername);
        var count = (long)(await checkCmd.ExecuteScalarAsync() ?? 0L);

        if (count > 0)
        {
            Console.WriteLine($"⚠️  用户名 '{defaultUsername}' 已存在！");
            Console.WriteLine();
            Console.Write("是否覆盖现有用户？(y/N): ");
            var response = Console.ReadLine()?.Trim().ToLower();

            if (response != "y" && response != "yes")
            {
                Console.WriteLine("❌ 操作已取消");
                return;
            }

            // 删除现有用户
            using var deleteCmd = new NpgsqlCommand(
                @"DELETE FROM ""Users"" WHERE ""Username"" = @username", conn);
            deleteCmd.Parameters.AddWithValue("username", defaultUsername);
            await deleteCmd.ExecuteNonQueryAsync();

            Console.WriteLine($"✅ 已删除现有用户");
        }
    }

    // 生成密码哈希
    Console.WriteLine($"🔐 正在生成密码哈希...");
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
    Console.WriteLine($"✅ 密码哈希已生成");
    Console.WriteLine();

    // 插入新用户
    using (var insertCmd = new NpgsqlCommand(@"
        INSERT INTO ""Users"" (
            ""Id"",
            ""Username"",
            ""Name"",
            ""Email"",
            ""Role"",
            ""LoginType"",
            ""PasswordHash"",
            ""IsActive"",
            ""IsActivated"",
            ""MustChangePassword"",
            ""CreatedAt"",
            ""UpdatedAt""
        ) VALUES (
            @id,
            @username,
            @name,
            @email,
            @role,
            @loginType,
            @passwordHash,
            @isActive,
            @isActivated,
            @mustChangePassword,
            @createdAt,
            @updatedAt
        )", conn))
    {
        var userId = Guid.NewGuid();
        insertCmd.Parameters.AddWithValue("id", userId);
        insertCmd.Parameters.AddWithValue("username", defaultUsername);
        insertCmd.Parameters.AddWithValue("name", defaultName);
        insertCmd.Parameters.AddWithValue("email", defaultEmail);
        insertCmd.Parameters.AddWithValue("role", "Admin");
        insertCmd.Parameters.AddWithValue("loginType", "Password");
        insertCmd.Parameters.AddWithValue("passwordHash", passwordHash);
        insertCmd.Parameters.AddWithValue("isActive", true);
        insertCmd.Parameters.AddWithValue("isActivated", true); // 管理员账户默认已开通
        insertCmd.Parameters.AddWithValue("mustChangePassword", true);
        insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
        insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await insertCmd.ExecuteNonQueryAsync();

        Console.WriteLine("========================================");
        Console.WriteLine("✅ 管理员账户创建成功！");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine($"用户ID: {userId}");
        Console.WriteLine($"用户名: {defaultUsername}");
        Console.WriteLine($"密码:   {defaultPassword}");
        Console.WriteLine($"姓名:   {defaultName}");
        Console.WriteLine($"邮箱:   {defaultEmail}");
        Console.WriteLine($"角色:   Admin");
        Console.WriteLine();
        Console.WriteLine("⚠️  重要提示:");
        Console.WriteLine("  1. 请立即使用上述凭据登录系统");
        Console.WriteLine("  2. 首次登录后会要求修改密码");
        Console.WriteLine("  3. 请妥善保管新密码");
        Console.WriteLine("  4. 建议删除或加密此脚本");
        Console.WriteLine();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 错误: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("详细信息:");
    Console.WriteLine(ex.ToString());
    Environment.Exit(1);
}
