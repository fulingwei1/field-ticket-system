using FluentAssertions;
using FieldTicket.Core.Services;
using FieldTicket.Core.Validators;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Services;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace FieldTicket.Tests.Unit.Services;

/// <summary>
/// TicketService 单元测试
/// </summary>
public class TicketServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<TicketService>> _loggerMock;
    private readonly TicketService _ticketService;
    private readonly TicketValidator _validator;
    private readonly TicketNumberService _ticketNumberService;

    public TicketServiceTests()
    {
        // 使用内存数据库进行测试
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<TicketService>>();
        _validator = new TicketValidator();
        _ticketNumberService = new TicketNumberService(_cacheMock.Object);

        _ticketService = new TicketService(
            _dbContext,
            _validator,
            _ticketNumberService,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateDraftAsync_ShouldCreateDraftTicket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateTicketRequest
        {
            CustomerId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            SymptomDetail = "测试问题详情",
            Priority = "Medium",
            Domain = "A",
            FactsJson = JsonDocument.Parse("{\"mechanical\":{\"action_completed\":\"yes\"}}")
        };

        // Act
        var result = await _ticketService.CreateDraftAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Draft");
        result.SymptomTitle.Should().Be("测试问题");
        result.CreatedByUserId.Should().Be(userId);

        var ticket = await _dbContext.Tickets.FindAsync(Guid.Parse(result.TicketId));
        ticket.Should().NotBeNull();
        ticket!.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task CreateDraftAsync_WithIdempotencyKey_ShouldReturnSameTicket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var idempotencyKey = "test-key-123";
        var request = new CreateTicketRequest
        {
            CustomerId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            SymptomDetail = "测试问题详情",
            Priority = "Medium",
            Domain = "A",
            FactsJson = JsonDocument.Parse("{\"mechanical\":{\"action_completed\":\"yes\"}}")
        };

        // Act
        var result1 = await _ticketService.CreateDraftAsync(request, userId, idempotencyKey);
        var result2 = await _ticketService.CreateDraftAsync(request, userId, idempotencyKey);

        // Assert
        result1.TicketId.Should().Be(result2.TicketId);
    }

    [Fact]
    public async Task SubmitTicketAsync_ShouldChangeStatusToSubmitted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticket = new Ticket
        {
            TicketId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            SymptomDetail = "测试问题详情",
            Priority = "Medium",
            Domain = "A",
            Status = "Draft",
            FactsJson = JsonDocument.Parse("{\"mechanical\":{\"action_completed\":\"yes\"}}"),
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _ticketService.SubmitTicketAsync(ticket.TicketId, userId);

        // Assert
        result.Status.Should().Be("Submitted");
        result.SubmittedAt.Should().NotBeNull();

        var updatedTicket = await _dbContext.Tickets.FindAsync(ticket.TicketId);
        updatedTicket!.Status.Should().Be("Submitted");
        updatedTicket.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitTicketAsync_WhenTicketNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _ticketService.SubmitTicketAsync(ticketId, userId));
    }

    [Fact]
    public async Task SubmitTicketAsync_WhenTicketAlreadySubmitted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticket = new Ticket
        {
            TicketId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            Status = "Submitted",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow
        };
        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ticketService.SubmitTicketAsync(ticket.TicketId, userId));
    }

    [Fact]
    public async Task GetTicketAsync_ShouldReturnTicket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticket = new Ticket
        {
            TicketId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            SymptomTitle = "测试问题",
            Status = "Draft",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _ticketService.GetTicketAsync(ticket.TicketId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.TicketId.Should().Be(ticket.TicketId.ToString());
        result.SymptomTitle.Should().Be("测试问题");
    }

    [Fact]
    public async Task GetTicketAsync_WhenTicketNotFound_ShouldReturnNull()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var result = await _ticketService.GetTicketAsync(ticketId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTicketsAsync_ShouldReturnFilteredTickets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = new[]
        {
            new Ticket
            {
                TicketId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                SymptomTitle = "问题1",
                Status = "Draft",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Ticket
            {
                TicketId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                SymptomTitle = "问题2",
                Status = "Submitted",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };
        _dbContext.Tickets.AddRange(tickets);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _ticketService.GetTicketsAsync(
            userId: userId,
            status: "Draft");

        // Assert
        result.Should().HaveCount(1);
        result.First().SymptomTitle.Should().Be("问题1");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

