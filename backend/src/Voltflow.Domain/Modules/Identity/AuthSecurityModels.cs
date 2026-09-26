using System.Security.Cryptography;
using System.Text;
using Voltflow.Domain.Common;

namespace Voltflow.Domain.Identity;

public sealed class UserSession : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private UserSession() { }

    public UserSession(Guid userId, string token, DateTime expiresAt)
    {
        UserId = userId;
        TokenHash = Hash(token);
        ExpiresAt = expiresAt;
    }

    public void Revoke() => RevokedAt = DateTime.UtcNow;
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    public static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private PasswordResetToken() { }

    public PasswordResetToken(Guid userId, string token, DateTime expiresAt)
    {
        UserId = userId;
        TokenHash = UserSession.Hash(token);
        ExpiresAt = expiresAt;
    }

    public void MarkUsed() => UsedAt = DateTime.UtcNow;
    public bool IsUsable => UsedAt is null && ExpiresAt > DateTime.UtcNow;
}
