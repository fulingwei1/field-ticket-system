using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace FieldTicket.Infrastructure.WeCom;

/// <summary>
/// 角色映射配置
/// 将系统角色映射到企业微信标签和部门
/// </summary>
public class RoleMappingConfig
{
    public Dictionary<string, RoleMapping> RoleMappings { get; set; } = new();

    /// <summary>
    /// 从配置文件加载
    /// </summary>
    public static RoleMappingConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new RoleMappingConfig();
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<RoleMappingConfig>(json) ?? new RoleMappingConfig();
    }

    /// <summary>
    /// 从配置节加载
    /// </summary>
    public static RoleMappingConfig LoadFromConfiguration(IConfiguration configuration)
    {
        var config = new RoleMappingConfig();
        var section = configuration.GetSection("WeCom:RoleMappings");
        
        if (section.Exists())
        {
            section.Bind(config.RoleMappings);
        }

        return config;
    }

    /// <summary>
    /// 获取角色对应的企业微信标签
    /// </summary>
    public List<string> GetWeComTagsForRole(string role)
    {
        if (RoleMappings.TryGetValue(role, out var mapping))
        {
            return mapping.WeComTags ?? new List<string>();
        }
        return new List<string>();
    }

    /// <summary>
    /// 获取角色对应的部门名称
    /// </summary>
    public List<string> GetDepartmentNamesForRole(string role)
    {
        if (RoleMappings.TryGetValue(role, out var mapping))
        {
            return mapping.Departments ?? new List<string>();
        }
        return new List<string>();
    }
}

/// <summary>
/// 角色映射项
/// </summary>
public class RoleMapping
{
    /// <summary>
    /// 企业微信标签列表
    /// </summary>
    public List<string>? WeComTags { get; set; }

    /// <summary>
    /// 部门名称列表
    /// </summary>
    public List<string>? Departments { get; set; }
}

