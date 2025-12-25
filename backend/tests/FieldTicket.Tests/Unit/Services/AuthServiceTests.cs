using FluentAssertions;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Services;
using FieldTicket.Infrastructure.WeCom;
using FieldTicket.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FieldTicket.Tests.Unit.Services;

/// <summary>
/// AuthService 单元测试 - 关注安全性修复
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<WeComUserService> _weComUserServiceMock;
    private readonly Mock<JwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IOptions<WeComOptions> _weComOptions;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _cacheMock = new Mock<IDistributedCache>();
        _weComUserServiceMock = new Mock<WeComUserService>();
        _jwtTokenServiceMock = new Mock<JwtTokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _weComOptions = Options.Create(new WeComOptions
        {
            CorpId = "test-corp-id",
            AgentId = "1000001",
            Secret = "test-secret",
            RedirectUri = "https://example.com/callback"
        });

        _authService = new AuthService(
            _weComOptions,
            _weComUserServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _dbContext,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// CRITICAL FIX TEST: 验证 CSRF 保护 - state 参数必需
    /// 修复: 原代码中 state 是可选的,存在 CSRF 攻击风险
    /// </summary>
    [Fact]
    public async Task HandleWeComCallbackAsync_WithoutState_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var code = "valid-code";
        string? state = null; // ❌ 攻击者可以省略 state 参数

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.HandleWeComCallbackAsync(code, state));

        exception.Message.Should().Contain("State parameter is required");

        // 验证日志记录了潜在的 CSRF 攻击
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CSRF attack")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// CRITICAL FIX TEST: 验证 CSRF 保护 - 空字符串 state 也应拒绝
    /// </summary>
    [Fact]
    public async Task HandleWeComCallbackAsync_WithEmptyState_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var code = "valid-code";
        var state = ""; // ❌ 空字符串

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.HandleWeComCallbackAsync(code, state));

        exception.Message.Should().Contain("State parameter is required");
    }

    /// <summary>
    /// CRITICAL FIX TEST: 验证 CSRF 保护 - 无效 state 应拒绝
    /// </summary>
    [Fact]
    public async Task HandleWeComCallbackAsync_WithInvalidState_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var code = "valid-code";
        var state = "invalid-state";

        // 模拟缓存中没有此 state (无效或已过期)
        _cacheMock.Setup(x => x.GetStringAsync($"wecom:state:{state}", default))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.HandleWeComCallbackAsync(code, state));

        exception.Message.Should().Contain("Invalid or expired state parameter");

        // 验证日志记录了无效 state
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("invalid state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
