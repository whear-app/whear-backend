namespace WhearApp.Application.Common.Models;

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<string>? Cc { get; set; }
    public List<string>? Bcc { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
    public string? From { get; set; }
    public string? FromName { get; set; }
    public string? ReplyTo { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}