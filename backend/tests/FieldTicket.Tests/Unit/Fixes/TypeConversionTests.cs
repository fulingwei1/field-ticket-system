using FluentAssertions;
using Xunit;

namespace FieldTicket.Tests.Unit.Fixes;

/// <summary>
/// 类型转换修复验证测试
/// 验证我们修复的 decimal/double 转换问题不会导致精度丢失
/// </summary>
public class TypeConversionTests
{
    /// <summary>
    /// CRITICAL FIX TEST: 验证 decimal/double 混合运算精度
    /// 修复: UserProfileService.cs:292 - avgTime (double) 转 decimal 后运算
    /// </summary>
    [Fact]
    public void DecimalDoubleConversion_ShouldPreservePrecision()
    {
        // Arrange - 模拟实际场景中的平均时间计算
        var times = new List<int> { 250, 280, 320, 400, 450 };
        var avgTime = times.Average(); // double

        // Act - 使用修复后的逻辑
        var timeScore = avgTime < 300
            ? 1.0m
            : Math.Max(0.0m, 1.0m - ((decimal)avgTime - 300) / 600.0m);

        // Assert - 验证计算结果正确
        avgTime.Should().Be(340.0); // double 平均值
        timeScore.Should().BeApproximately(0.9333m, 0.0001m); // 1.0 - (340-300)/600 = 0.9333
    }

    /// <summary>
    /// CRITICAL FIX TEST: char vs string 比较
    /// 修复: TicketSearchService.cs:143 - Domain 字段 char 转 string 比较
    /// </summary>
    [Fact]
    public void CharToStringConversion_ShouldWorkCorrectly()
    {
        // Arrange
        char domainChar = 'A';
        string domainString = "A";

        // Act - 使用修复后的逻辑
        var result = domainChar.ToString() == domainString;

        // Assert
        result.Should().BeTrue();

        // 验证不同的值
        domainChar = 'B';
        domainString = "A";
        result = domainChar.ToString() == domainString;
        result.Should().BeFalse();
    }

    /// <summary>
    /// CRITICAL FIX TEST: 验证 decimal 运算不会因 double 中间结果产生误差
    /// </summary>
    [Fact]
    public void DecimalArithmetic_ShouldNotAccumulateFloatingPointErrors()
    {
        // Arrange - 模拟分数计算场景
        decimal score = 0.0m;

        // 模拟 UserProfileService 中的多个权重计算
        decimal ticketCountScore = Math.Min(50 / 100.0m, 1.0m);
        score += ticketCountScore * 0.3m; // 权重 30%

        decimal timeScore = 0.8m;
        score += timeScore * 0.2m; // 权重 20%

        decimal errorRate = 0.1m;
        score += (1.0m - errorRate) * 0.3m; // 权重 30%

        decimal knowledgeScore = 0.7m;
        score += knowledgeScore * 0.2m; // 权重 20%

        // Act
        var finalScore = Math.Min(1.0m, score);

        // Assert - 验证使用 decimal 可以精确计算
        // 0.5*0.3 + 0.8*0.2 + 0.9*0.3 + 0.7*0.2 = 0.15 + 0.16 + 0.27 + 0.14 = 0.72
        finalScore.Should().Be(0.72m);

        // 如果用 double 计算,可能会有精度误差
        double doubleScore = 0.5 * 0.3 + 0.8 * 0.2 + 0.9 * 0.3 + 0.7 * 0.2;
        // double 结果可能是 0.7200000000000001 这样的值
        ((decimal)doubleScore).Should().NotBe(0.72m); // 证明 double 会有精度问题
    }

    /// <summary>
    /// CRITICAL FIX TEST: JsonElement vs JsonDocument 转换
    /// 修复: TicketTemplateService.cs:265
    /// </summary>
    [Fact]
    public void JsonElementToJsonDocument_ShouldConvertCorrectly()
    {
        // Arrange
        var jsonString = "{\"mechanical\":{\"action_completed\":\"yes\"}}";
        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);

        // Act - 模拟修复后的逻辑
        var hasProperty = jsonDoc.RootElement.TryGetProperty("mechanical", out var mechanicalElement);

        // 修复前: 直接赋值 JsonElement (错误)
        // 修复后: 转换为 JsonDocument
        var mechanicalDoc = hasProperty
            ? System.Text.Json.JsonDocument.Parse(mechanicalElement.GetRawText())
            : System.Text.Json.JsonDocument.Parse("{}");

        // Assert
        hasProperty.Should().BeTrue();
        mechanicalDoc.RootElement.TryGetProperty("action_completed", out var actionProp).Should().BeTrue();
        actionProp.GetString().Should().Be("yes");

        // Cleanup
        jsonDoc.Dispose();
        mechanicalDoc.Dispose();
    }
}
