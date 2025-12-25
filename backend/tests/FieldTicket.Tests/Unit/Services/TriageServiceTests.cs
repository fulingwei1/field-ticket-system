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
/// TriageService 单元测试
/// </summary>
public class TriageServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<TriageService>> _loggerMock;
    private readonly TriageService _triageService;

    public TriageServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<TriageService>>();

        _triageService = new TriageService(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task TriageTicketAsync_ShouldCreateTriageNote()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var jcCode = "JC-001";
        
        var ticket = new Ticket
        {
            TicketId = ticketId,
            CustomerId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            Status = "Submitted",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var judgementCard = new JudgementCard
        {
            JcCode = jcCode,
            Title = "测试判断卡",
            Description = "测试描述",
            Domain = "A",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);
        _dbContext.JudgementCards.Add(judgementCard);
        await _dbContext.SaveChangesAsync();

        var request = new TriageTicketRequest
        {
            JcCode = jcCode,
            Confidence = 4,
            CurrentHypothesis = "测试假设",
            NextAction = "测试动作"
        };

        // Act
        var result = await _triageService.TriageTicketAsync(ticketId, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.JcCode.Should().Be(jcCode);
        result.Confidence.Should().Be(4);

        var triageNote = await _dbContext.TriageNotes
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);
        triageNote.Should().NotBeNull();
        triageNote!.JcCode.Should().Be(jcCode);

        var updatedTicket = await _dbContext.Tickets.FindAsync(ticketId);
        updatedTicket!.Status.Should().Be("Triage");
        updatedTicket.CurrentJcCode.Should().Be(jcCode);
    }

    [Fact]
    public async Task TriageTicketAsync_WhenLowConfidence_ShouldSetEscalationFlag()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var jcCode = "JC-001";
        
        var ticket = new Ticket
        {
            TicketId = ticketId,
            CustomerId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            Status = "Submitted",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var judgementCard = new JudgementCard
        {
            JcCode = jcCode,
            Title = "测试判断卡",
            Domain = "A",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);
        _dbContext.JudgementCards.Add(judgementCard);
        await _dbContext.SaveChangesAsync();

        var request = new TriageTicketRequest
        {
            JcCode = jcCode,
            Confidence = 2, // 低置信度
            CurrentHypothesis = "测试假设"
        };

        // Act
        var result = await _triageService.TriageTicketAsync(ticketId, request, userId);

        // Assert
        result.EscalationRequired.Should().BeTrue(); // HR-002: 低置信度自动升级
    }

    [Fact]
    public async Task TriageTicketAsync_WhenTicketNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new TriageTicketRequest
        {
            JcCode = "JC-001",
            Confidence = 4
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _triageService.TriageTicketAsync(ticketId, request, userId));
    }

    [Fact]
    public async Task TriageTicketAsync_WhenJudgementCardNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var ticket = new Ticket
        {
            TicketId = ticketId,
            CustomerId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            Status = "Submitted",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        var request = new TriageTicketRequest
        {
            JcCode = "NONEXISTENT",
            Confidence = 4
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _triageService.TriageTicketAsync(ticketId, request, userId));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

