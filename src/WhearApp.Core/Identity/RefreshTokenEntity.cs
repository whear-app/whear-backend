using System.ComponentModel.DataAnnotations.Schema;

namespace WhearApp.Core.Identity;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevokedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;

    public virtual UserEntity User { get; set; } = null!;
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;
}