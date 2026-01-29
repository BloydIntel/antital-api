using Antital.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Antital.Test.Infrastructure.Services;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly Mock<IOptions<EmailSettings>> _optionsMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmailService>>();
        _optionsMock = new Mock<IOptions<EmailSettings>>();
        _optionsMock.Setup(x => x.Value).Returns(new EmailSettings
        {
            BaseUrl = "https://antital.com",
            FromEmail = "test@antital.com",
            FromName = "Antital"
        });
        _emailService = new EmailService(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_LogsEmailContent()
    {
        // Arrange
        var email = "user@example.com";
        var token = "verification_token_12345";

        // Act
        await _emailService.SendVerificationEmailAsync(email, token, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Verification Email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_EmailContainsCorrectVerificationLink()
    {
        // Arrange
        var email = "user@example.com";
        var token = "verification_token_12345";
        var capturedLogMessage = string.Empty;

        _loggerMock
            .Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Delegate>((level, eventId, state, exception, formatter) =>
            {
                capturedLogMessage = state.ToString() ?? string.Empty;
            });

        // Act
        await _emailService.SendVerificationEmailAsync(email, token, CancellationToken.None);

        // Assert
        capturedLogMessage.Should().Contain("https://antital.com/verify-email");
        capturedLogMessage.Should().Contain($"email={Uri.EscapeDataString(email)}");
        capturedLogMessage.Should().Contain($"token={Uri.EscapeDataString(token)}");
    }

    [Fact]
    public async Task SendVerificationEmailAsync_EmailContainsUserEmailAndToken()
    {
        // Arrange
        var email = "test.user@example.com";
        var token = "test_token_abc123";
        var capturedLogMessage = string.Empty;

        _loggerMock
            .Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Delegate>((level, eventId, state, exception, formatter) =>
            {
                capturedLogMessage = state.ToString() ?? string.Empty;
            });

        // Act
        await _emailService.SendVerificationEmailAsync(email, token, CancellationToken.None);

        // Assert
        capturedLogMessage.Should().Contain($"To: {email}");
        capturedLogMessage.Should().Contain("Verify Your Email Address");
        capturedLogMessage.Should().Contain("This link will expire in 24 hours");
    }

    [Fact]
    public async Task SendVerificationEmailAsync_CompletesSuccessfully()
    {
        // Arrange
        var email = "user@example.com";
        var token = "token123";

        // Act
        var task = _emailService.SendVerificationEmailAsync(email, token, CancellationToken.None);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }
}
