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

namespace FieldTicket.Tests.Integration;

/// <summary>
/// 工单工作流集成测试
/// </summary>
public class TicketWorkflowTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TicketService _ticketService;
    private readonly TriageService _triageService;
    private readonly SolutionService _solutionService;
    private readonly VerificationService _verificationService;

    public TicketWorkflowTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<TicketService>>();
        var triageLoggerMock = new Mock<ILogger<TriageService>>();
        var solutionLoggerMock = new Mock<ILogger<SolutionService>>();
        var verificationLoggerMock = new Mock<ILogger<VerificationService>>();

        var validator = new TicketValidator();
        var ticketNumberService = new TicketNumberService(cacheMock.Object);
        var solutionNumberService = new SolutionNumberService(cacheMock.Object);

        _ticketService = new TicketService(
            _dbContext,
            validator,
            ticketNumberService,
            cacheMock.Object,
            loggerMock.Object);

        _triageService = new TriageService(_dbContext, triageLoggerMock.Object);
        _solutionService = new SolutionService(_dbContext, solutionNumberService, solutionLoggerMock.Object);
        _verificationService = new VerificationService(_dbContext, verificationLoggerMock.Object);
    }

    [Fact]
    public async Task CompleteWorkflow_ShouldFlowThroughAllStates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        // Step 1: Create Draft
        var createRequest = new CreateTicketRequest
        {
            CustomerId = customerId,
            ProjectId = projectId,
            DeviceId = deviceId,
            SymptomTitle = "测试问题",
            SymptomDetail = "测试详情",
            Priority = "High",
            Domain = "A",
            FactsJson = JsonDocument.Parse("{\"mechanical\":{\"action_completed\":\"yes\"}}")
        };

        var draft = await _ticketService.CreateDraftAsync(createRequest, userId);
        draft.Status.Should().Be("Draft");

        // Step 2: Submit Ticket
        var submitted = await _ticketService.SubmitTicketAsync(Guid.Parse(draft.TicketId), userId);
        submitted.Status.Should().Be("Submitted");

        // Step 3: Triage Ticket
        var jcCode = "JC-001";
        var judgementCard = new JudgementCard
        {
            JcCode = jcCode,
            Title = "测试判断卡",
            Domain = "A",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.JudgementCards.Add(judgementCard);
        await _dbContext.SaveChangesAsync();

        var triageRequest = new TriageTicketRequest
        {
            JcCode = jcCode,
            Confidence = 4,
            CurrentHypothesis = "测试假设",
            NextAction = "创建解决方案"
        };

        var triaged = await _triageService.TriageTicketAsync(
            Guid.Parse(draft.TicketId),
            triageRequest,
            userId);
        triaged.JcCode.Should().Be(jcCode);

        var ticketAfterTriage = await _ticketService.GetTicketAsync(Guid.Parse(draft.TicketId), userId);
        ticketAfterTriage!.Status.Should().Be("Triage");

        // Step 4: Create Solution
        var solutionRequest = new CreateSolutionRequest
        {
            Title = "测试解决方案",
            Summary = "解决方案摘要",
            Changes = new List<SolutionChangeDto>
            {
                new SolutionChangeDto
                {
                    Type = "code",
                    Location = "test.py",
                    Before = "old",
                    After = "new",
                    Reason = "修复"
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

        var solution = await _solutionService.CreateSolutionAsync(
            Guid.Parse(draft.TicketId),
            solutionRequest,
            userId);
        solution.Status.Should().Be("Draft");

        // Step 5: Publish Solution
        var published = await _solutionService.PublishSolutionAsync(
            Guid.Parse(solution.SolutionId),
            userId);
        published.Status.Should().Be("Published");

        var ticketAfterPublish = await _ticketService.GetTicketAsync(Guid.Parse(draft.TicketId), userId);
        ticketAfterPublish!.Status.Should().Be("SolutionIssued");

        // Step 6: Submit Verification
        var checklistResultJson = JsonDocument.Parse(@"[
            {
                ""itemId"": 0,
                ""passed"": true,
                ""evidence"": ""验证通过""
            }
        ]");
        
        var verificationRequest = new SubmitVerificationRequest
        {
            SolutionId = Guid.Parse(solution.SolutionId),
            RunCount = 3,
            PassCount = 3,
            FailCount = 0,
            ChecklistResultJson = checklistResultJson,
            Note = "验证完成"
        };

        var verification = await _verificationService.SubmitVerificationAsync(
            Guid.Parse(draft.TicketId),
            verificationRequest,
            userId);
        verification.OverallResult.Should().Be("PASS");

        var ticketAfterVerification = await _ticketService.GetTicketAsync(Guid.Parse(draft.TicketId), userId);
        ticketAfterVerification!.Status.Should().Be("Verifying");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

