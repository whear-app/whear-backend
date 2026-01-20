using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WhearApp.Application.Common.Models;
using WhearApp.Infrastructure.Common;
using WhearApp.Infrastructure.Common.Services;

namespace WhearApp.UnitTests;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _loggerMock = new();
    private readonly EmailSettings _validSettings = new()
    {
        SmtpServer = "smtp.gmail.com",
        SmtpPort = 587,
        SenderEmail = "test@example.com",
        SenderName = "Test Sender",
        Username = "username",
        Password = "password",
        UseSsl = false,
        UseStartTls = true
    };

    [Fact]
    public void Constructor_WithValidSettings_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_validSettings);

        // Act & Assert
        var exception = Record.Exception(() => new EmailService(options, _loggerMock.Object));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("", 587, "test@example.com")] // Empty SMTP server
    [InlineData("smtp.gmail.com", 0, "test@example.com")] // Invalid port
    [InlineData("smtp.gmail.com", 587, "")] // Empty sender email
    public void Constructor_WithInvalidSettings_ShouldThrowException(
        string smtpServer, 
        int smtpPort, 
        string senderEmail)
    {
        // Arrange
        var invalidSettings = new EmailSettings
        {
            SmtpServer = smtpServer,
            SmtpPort = smtpPort,
            SenderEmail = senderEmail
        };
        var options = Options.Create(invalidSettings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new EmailService(options, _loggerMock.Object));
    }

    [Fact]
    public void CreateMimeMessage_WithBasicMessage_ShouldCreateCorrectMimeMessage()
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new EmailService(options, _loggerMock.Object);
        
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            IsHtml = true
        };

        // Act - Use reflection to test private method for demonstration
        // In real scenario, test through public methods
        var mimeMessage = CreateMimeMessageViaReflection(service, message);

        // Assert
        Assert.Equal("Test Subject", mimeMessage.Subject);
        Assert.Contains("recipient@example.com", mimeMessage.To.ToString());
    }

    [Fact]
    public void CreateMimeMessage_WithCcAndBcc_ShouldIncludeRecipients()
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new EmailService(options, _loggerMock.Object);
        
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Test",
            Body = "Body",
            Cc = new List<string> { "cc1@example.com", "cc2@example.com" },
            Bcc = new List<string> { "bcc@example.com" }
        };

        // Act
        var mimeMessage = CreateMimeMessageViaReflection(service, message);

        // Assert
        Assert.Equal(2, mimeMessage.Cc.Count);
        Assert.Single(mimeMessage.Bcc);
    }

    [Fact]
    public void CreateMimeMessage_WithAttachments_ShouldIncludeAttachments()
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new EmailService(options, _loggerMock.Object);
        
        var attachmentContent = System.Text.Encoding.UTF8.GetBytes("Test content");
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Test",
            Body = "Body",
            Attachments = new List<EmailAttachment>
            {
                new EmailAttachment
                {
                    FileName = "test.txt",
                    Content = attachmentContent,
                    ContentType = "text/plain"
                }
            }
        };

        // Act
        var mimeMessage = CreateMimeMessageViaReflection(service, message);

        // Assert
        Assert.NotNull(mimeMessage.Body);
        var multipart = mimeMessage.Body as MimeKit.Multipart;
        Assert.NotNull(multipart);
        Assert.True(multipart.Count > 1); // Body + attachment
    }

    // Helper method using reflection (for demonstration)
    private static MimeKit.MimeMessage CreateMimeMessageViaReflection(
        EmailService service, 
        EmailMessage message)
    {
        var method = typeof(EmailService)
            .GetMethod("CreateMimeMessage", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
        
        return (MimeKit.MimeMessage)method!.Invoke(service, [message])!;
    }
}