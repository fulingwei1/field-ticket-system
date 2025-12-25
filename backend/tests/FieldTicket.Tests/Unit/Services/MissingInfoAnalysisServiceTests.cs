using FluentAssertions;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Services;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace FieldTicket.Tests.Unit.Services;

/// <summary>
/// MissingInfoAnalysisService 单元测试
/// </summary>
public class MissingInfoAnalysisServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<MissingInfoAnalysisService>> _loggerMock;
    private readonly MissingInfoAnalysisService _service;

    public MissingInfoAnalysisServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<MissingInfoAnalysisService>>();

        _service = new MissingInfoAnalysisService(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task AnalyzeMissingInfoAsync_ShouldReturnMissingInfo()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            TicketId = ticketId,
            CustomerId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            SymptomDetail = "", // 缺失信息
            Status = "Draft",
            FactsJson = JsonDocument.Parse("{\"mechanical\":{}}"), // 空的facts
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var jcCode = "JC-001";
        var judgementCard = new JudgementCard
        {
            JcCode = jcCode,
            Title = "测试判断卡",
            Domain = "A",
            KeyChecksJson = JsonDocument.Parse(@"[
                {
                    ""item"": ""动作完成"",
                    ""domain"": ""mechanical"",
                    ""field"": ""action_completed"",
                    ""required"": true,
                    ""question"": ""动作是否完成？"",
                    ""type"": ""yes_no""
                }
            ]"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);
        _dbContext.JudgementCards.Add(judgementCard);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzeMissingInfoAsync(ticketId, jcCode);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(m => m.Field == "mechanical.action_completed");
        result.Should().Contain(m => m.Required == true);
    }

    [Fact]
    public async Task GenerateQuestionnaireAsync_ShouldGenerateQuestions()
    {
        // Arrange
        var missingInfo = new List<MissingInfoItem>
        {
            new MissingInfoItem
            {
                Field = "mechanical.action_completed",
                Question = "动作是否完成？",
                Type = "yes_no",
                Required = true
            },
            new MissingInfoItem
            {
                Field = "symptomDetail",
                Question = "请详细描述问题现象",
                Type = "text",
                Required = false
            }
        };

        // Act
        var result = await _service.GenerateQuestionnaireAsync(missingInfo);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(q => q.QuestionId == "mechanical.action_completed");
        result.Should().Contain(q => q.Type == "yes_no");
        result.Should().Contain(q => q.Type == "text");
    }

    [Fact]
    public async Task AnalyzeMissingInfoAsync_WhenTicketNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AnalyzeMissingInfoAsync(ticketId));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

