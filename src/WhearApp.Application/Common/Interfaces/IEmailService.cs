using WhearApp.Application.Common.Models;

namespace WhearApp.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task SendBulkEmailAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
}
