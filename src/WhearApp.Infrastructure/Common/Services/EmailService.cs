using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using WhearApp.Application.Common.Exceptions;
using WhearApp.Application.Common.Interfaces;
using WhearApp.Application.Common.Models;

namespace WhearApp.Infrastructure.Common.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        ValidateSettings();
    }

    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var mimeMessage = CreateMimeMessage(message);
            await SendMimeMessageAsync(mimeMessage, cancellationToken);
            
            _logger.LogInformation("Email sent successfully to {To}", message.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", message.To);
            throw new EmailException($"Failed to send email to {message.To}", ex);
        }
    }

    public async Task SendBulkEmailAsync(
        IEnumerable<EmailMessage> messages, 
        CancellationToken cancellationToken = default)
    {
        var messagesList = messages.ToList();
        if (!messagesList.Any())
        {
            _logger.LogWarning("No emails to send in bulk operation");
            return;
        }

        using var client = await CreateSmtpClientAsync(cancellationToken);
        
        var failedEmails = new List<string>();
        var successCount = 0;

        foreach (var message in messagesList)
        {
            try
            {
                var mimeMessage = CreateMimeMessage(message);
                await client.SendAsync(mimeMessage, cancellationToken);
                successCount++;
                
                _logger.LogDebug("Bulk email sent to {To}", message.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk email to {To}", message.To);
                failedEmails.Add(message.To);
            }
        }

        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation(
            "Bulk email operation completed. Success: {Success}, Failed: {Failed}", 
            successCount, 
            failedEmails.Count);

        if (failedEmails.Any())
        {
            throw new EmailException(
                $"Failed to send {failedEmails.Count} email(s). Recipients: {string.Join(", ", failedEmails)}");
        }
    }

    private MimeMessage CreateMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        // From
        var fromEmail = message.From ?? _settings.SenderEmail;
        var fromName = message.FromName ?? _settings.SenderName;
        mimeMessage.From.Add(new MailboxAddress(fromName, fromEmail));

        // To
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));

        // Cc
        if (message.Cc?.Any() == true)
        {
            foreach (var cc in message.Cc)
            {
                mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
            }
        }

        // Bcc
        if (message.Bcc?.Any() == true)
        {
            foreach (var bcc in message.Bcc)
            {
                mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
            }
        }

        // Reply-To
        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            mimeMessage.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
        }

        // Subject
        mimeMessage.Subject = message.Subject;

        // Custom Headers
        if (message.Headers?.Any() == true)
        {
            foreach (var header in message.Headers)
            {
                mimeMessage.Headers.Add(header.Key, header.Value);
            }
        }

        // Body with attachments
        var bodyBuilder = new BodyBuilder();
        
        if (message.IsHtml)
        {
            bodyBuilder.HtmlBody = message.Body;
        }
        else
        {
            bodyBuilder.TextBody = message.Body;
        }

        // Attachments
        if (message.Attachments?.Any() == true)
        {
            foreach (var attachment in message.Attachments)
            {
                bodyBuilder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
            }
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        return mimeMessage;
    }

    private async Task SendMimeMessageAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = await CreateSmtpClientAsync(cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private async Task<SmtpClient> CreateSmtpClientAsync(CancellationToken cancellationToken)
    {
        var client = new SmtpClient
        {
            Timeout = _settings.TimeoutMs
        };

        try
        {
            // Determine security options
            var secureSocketOptions = _settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : _settings.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

            await client.ConnectAsync(
                _settings.SmtpServer,
                _settings.SmtpPort,
                secureSocketOptions,
                cancellationToken);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_settings.Username) && 
                !string.IsNullOrEmpty(_settings.Password))
            {
                await client.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password,
                    cancellationToken);
            }

            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpServer))
            throw new InvalidOperationException("SMTP server is not configured");

        if (_settings.SmtpPort <= 0)
            throw new InvalidOperationException("SMTP port is not configured");

        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
            throw new InvalidOperationException("Sender email is not configured");
    }
}