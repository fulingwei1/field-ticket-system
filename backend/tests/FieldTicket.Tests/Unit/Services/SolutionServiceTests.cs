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
/// SolutionService 单元测试
/// </summary>
public class SolutionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<SolutionService>> _loggerMock;
    private readonly SolutionService _solutionService;
    private readonly SolutionNumberService _solutionNumberService;

    public SolutionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<SolutionService>>();
        var cacheMock = new Mock<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
        _solutionNumberService = new SolutionNumberService(cacheMock.Object);

        _solutionService = new SolutionService(
            _dbContext,
            _solutionNumberService,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateSolutionAsync_ShouldCreateSolution()
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
            Status = "Triage",
            CurrentJcCode = "JC-001",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        var request = new CreateSolutionRequest
        {
            Title = "测试解决方案",
            Summary = "测试摘要",
            Changes = new List<SolutionChangeDto>
            {
                new SolutionChangeDto
                {
                    Type = "code",
                    Location = "test.py",
                    Before = "old code",
                    After = "new code",
                    Reason = "修复bug"
                }
            },
            VerificationChecklist = new List<VerificationChecklistItemDto>
            {
                new VerificationChecklistItemDto
                {
                    Type = "action",
                    Description = "执行动作",
                    ExpectedResult = "预期结果"
                }
            }
        };

        // Act
        var result = await _solutionService.CreateSolutionAsync(ticketId, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("测试解决方案");
        result.Status.Should().Be("Draft");

        var solution = await _dbContext.Solutions
            .FirstOrDefaultAsync(s => s.SolutionId == Guid.Parse(result.SolutionId));
        solution.Should().NotBeNull();
        solution!.TicketId.Should().Be(ticketId);
    }

    [Fact]
    public async Task PublishSolutionAsync_ShouldChangeStatusToPublished()
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
            Status = "Triage",
            CurrentJcCode = "JC-001",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var solution = new Solution
        {
            SolutionId = Guid.NewGuid(),
            TicketId = ticketId,
            Title = "测试解决方案",
            Status = "Draft",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);
        _dbContext.Solutions.Add(solution);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _solutionService.PublishSolutionAsync(solution.SolutionId, userId);

        // Assert
        result.Status.Should().Be("Published");
        result.PublishedAt.Should().NotBeNull();

        var updatedTicket = await _dbContext.Tickets.FindAsync(ticketId);
        updatedTicket!.Status.Should().Be("SolutionIssued");
    }

    [Fact]
    public async Task PublishSolutionAsync_WhenSolutionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var solutionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _solutionService.PublishSolutionAsync(solutionId, userId));
    }

    [Fact]
    public async Task PublishSolutionAsync_WhenTicketNotInTriage_ShouldThrowInvalidOperationException()
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
            Status = "Draft", // 不是 Triage 状态
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var solution = new Solution
        {
            SolutionId = Guid.NewGuid(),
            TicketId = ticketId,
            Title = "测试解决方案",
            Status = "Draft",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);
        _dbContext.Solutions.Add(solution);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _solutionService.PublishSolutionAsync(solution.SolutionId, userId));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

