using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Users;

/// <summary>
/// A refresh token owned by a <see cref="User"/>. Only a hash of the token is stored.
/// </summary>
public class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    internal RefreshToken(Guid userId, string tokenHash, DateTime createdAt, DateTime expiresAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && utcNow < ExpiresAt;

    internal void Revoke(DateTime utcNow) => RevokedAt ??= utcNow;
}
