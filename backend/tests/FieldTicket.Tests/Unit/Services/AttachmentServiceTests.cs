using FluentAssertions;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Services;
using FieldTicket.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FieldTicket.Tests.Unit.Services;

/// <summary>
/// AttachmentService 单元测试
/// </summary>
public class AttachmentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IMinIOService> _minioMock;
    private readonly Mock<ILogger<AttachmentService>> _loggerMock;
    private readonly AttachmentService _attachmentService;

    public AttachmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _minioMock = new Mock<IMinIOService>();
        _loggerMock = new Mock<ILogger<AttachmentService>>();

        _attachmentService = new AttachmentService(
            _dbContext,
            _minioMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateAttachment()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        var fileSize = 1024L;
        var fileContent = new byte[] { 1, 2, 3, 4 };
        var fileStream = new MemoryStream(fileContent);

        _minioMock.Setup(m => m.UploadFileAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<long>()))
            .ReturnsAsync("attachments/test-key");

        // Act
        var result = await _attachmentService.UploadAsync(
            ticketId,
            fileStream,
            fileName,
            contentType,
            fileSize,
            "photo",
            userId);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.FileSize.Should().Be(fileSize);
        result.FileType.Should().Be("photo");

        var attachment = await _dbContext.Attachments.FindAsync(Guid.Parse(result.AttachmentId));
        attachment.Should().NotBeNull();
        attachment!.TicketId.Should().Be(ticketId);
        attachment.UploadedBy.Should().Be(userId);
    }

    [Fact]
    public async Task UploadAsync_WhenFileTooLarge_ShouldThrowArgumentException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var fileSize = 11 * 1024 * 1024; // 11MB, 超过照片限制
        var fileStream = new MemoryStream(new byte[1024]);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _attachmentService.UploadAsync(
                ticketId,
                fileStream,
                "test.jpg",
                "image/jpeg",
                fileSize,
                "photo",
                userId));
    }

    [Fact]
    public async Task UploadAsync_WhenInvalidFileType_ShouldThrowArgumentException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var fileStream = new MemoryStream(new byte[1024]);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _attachmentService.UploadAsync(
                ticketId,
                fileStream,
                "test.pdf",
                "application/pdf",
                1024,
                "photo",
                userId));
    }

    [Fact]
    public async Task GetTicketAttachmentsAsync_ShouldReturnAttachments()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var attachments = new[]
        {
            new Attachment
            {
                AttachmentId = Guid.NewGuid(),
                TicketId = ticketId,
                UploadedBy = userId,
                FileName = "test1.jpg",
                FileType = "photo",
                FileSize = 1024,
                FileKey = "key1",
                UploadStatus = "completed",
                CreatedAt = DateTime.UtcNow
            },
            new Attachment
            {
                AttachmentId = Guid.NewGuid(),
                TicketId = ticketId,
                UploadedBy = userId,
                FileName = "test2.jpg",
                FileType = "photo",
                FileSize = 2048,
                FileKey = "key2",
                UploadStatus = "completed",
                CreatedAt = DateTime.UtcNow
            }
        };
        _dbContext.Attachments.AddRange(attachments);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _attachmentService.GetTicketAttachmentsAsync(ticketId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(a => a.CreatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteAttachment()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            TicketId = ticketId,
            UploadedBy = userId,
            FileName = "test.jpg",
            FileType = "photo",
            FileSize = 1024,
            FileKey = "test-key",
            UploadStatus = "completed",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Attachments.Add(attachment);
        await _dbContext.SaveChangesAsync();

        _minioMock.Setup(m => m.DeleteFileAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _attachmentService.DeleteAsync(attachment.AttachmentId, userId);

        // Assert
        var deleted = await _dbContext.Attachments.FindAsync(attachment.AttachmentId);
        deleted.Should().BeNull();
        _minioMock.Verify(m => m.DeleteFileAsync("test-key"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenUnauthorized_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            TicketId = ticketId,
            UploadedBy = userId,
            FileName = "test.jpg",
            FileType = "photo",
            FileSize = 1024,
            FileKey = "test-key",
            UploadStatus = "completed",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Attachments.Add(attachment);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _attachmentService.DeleteAsync(attachment.AttachmentId, otherUserId));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

