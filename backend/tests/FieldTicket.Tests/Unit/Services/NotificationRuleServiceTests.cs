using FluentAssertions;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Services;
using FieldTicket.Infrastructure.WeCom;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace FieldTicket.Tests.Unit.Services;

/// <summary>
/// NotificationRuleService 单元测试
/// </summary>
public class NotificationRuleServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IWeComNotificationService> _notificationServiceMock;
    private readonly Mock<WeComUserService> _weComUserServiceMock;
    private readonly Mock<ILogger<NotificationRuleService>> _loggerMock;
    private readonly NotificationRuleService _notificationRuleService;

    public NotificationRuleServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _notificationServiceMock = new Mock<IWeComNotificationService>();
        _weComUserServiceMock = new Mock<WeComUserService>(
            Mock.Of<Microsoft.Extensions.Options.IOptions<WeComOptions>>(),
            _dbContext,
            Mock.Of<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(),
            Mock.Of<ILogger<WeComUserService>>(),
            Mock.Of<IHttpClientFactory>());
        _loggerMock = new Mock<ILogger<NotificationRuleService>>();

        _notificationRuleService = new NotificationRuleService(
            _dbContext,
            _notificationServiceMock.Object,
            _weComUserServiceMock.Object,
            _loggerMock.Object,
            contactService: null);
    }

    [Fact]
    public async Task SaveNotificationRuleAsync_ShouldCreateRule()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new SaveNotificationRuleRequest
        {
            RuleLevel = "customer",
            CustomerId = Guid.NewGuid(),
            TriggerEvent = "ticket_submitted",
            RecipientsConfig = JsonDocument.Parse("{\"userIds\":[\"user1\",\"user2\"]}"),
            IsActive = true
        };

        // Act
        var result = await _notificationRuleService.SaveNotificationRuleAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.RuleLevel.Should().Be("customer");
        result.TriggerEvent.Should().Be("ticket_submitted");
        result.IsActive.Should().BeTrue();

        var rule = await _dbContext.NotificationRules
            .FirstOrDefaultAsync(r => r.RuleId == result.RuleId);
        rule.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNotificationRulesAsync_ShouldReturnFilteredRules()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var rules = new[]
        {
            new NotificationRule
            {
                RuleId = Guid.NewGuid(),
                RuleLevel = "customer",
                CustomerId = customerId,
                TriggerEvent = "ticket_submitted",
                RecipientsConfig = JsonDocument.Parse("{\"userIds\":[\"user1\"]}"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationRule
            {
                RuleId = Guid.NewGuid(),
                RuleLevel = "customer",
                CustomerId = customerId,
                TriggerEvent = "solution_published",
                RecipientsConfig = JsonDocument.Parse("{\"userIds\":[\"user2\"]}"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
        _dbContext.NotificationRules.AddRange(rules);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationRuleService.GetNotificationRulesAsync(
            customerId: customerId,
            triggerEvent: "ticket_submitted");

        // Assert
        result.Should().HaveCount(1);
        result.First().TriggerEvent.Should().Be("ticket_submitted");
    }

    [Fact]
    public async Task ExecuteNotificationAsync_ShouldSendNotification()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var ticket = new Ticket
        {
            TicketId = ticketId,
            CustomerId = customerId,
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            Status = "Submitted",
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var rule = new NotificationRule
        {
            RuleId = Guid.NewGuid(),
            RuleLevel = "customer",
            CustomerId = customerId,
            TriggerEvent = "ticket_submitted",
            RecipientsConfig = JsonDocument.Parse("{\"userIds\":[\"user1\",\"user2\"]}"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);
        _dbContext.NotificationRules.Add(rule);
        await _dbContext.SaveChangesAsync();

        _notificationServiceMock.Setup(n => n.SendNotificationAsync(
            It.IsAny<string[]>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationRuleService.ExecuteNotificationAsync(
            ticketId,
            "ticket_submitted");

        // Assert
        result.Success.Should().BeTrue();
        result.SentCount.Should().BeGreaterThan(0);

        var log = await _dbContext.NotificationLogs
            .FirstOrDefaultAsync(l => l.TicketId == ticketId);
        log.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteNotificationRuleAsync_ShouldDeleteRule()
    {
        // Arrange
        var rule = new NotificationRule
        {
            RuleId = Guid.NewGuid(),
            RuleLevel = "customer",
            CustomerId = Guid.NewGuid(),
            TriggerEvent = "ticket_submitted",
            RecipientsConfig = JsonDocument.Parse("{\"userIds\":[\"user1\"]}"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.NotificationRules.Add(rule);
        await _dbContext.SaveChangesAsync();

        // Act
        await _notificationRuleService.DeleteNotificationRuleAsync(rule.RuleId);

        // Assert
        var deleted = await _dbContext.NotificationRules.FindAsync(rule.RuleId);
        deleted.Should().BeNull();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

