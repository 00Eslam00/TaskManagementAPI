using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Users;

public class User : Entity
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 256;

    private readonly List<RefreshToken> _refreshTokens = [];

    private User()
    {
    }

    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public Role Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public bool IsAdmin => Role == Role.Admin;

    public static User Create(string name, string email, string passwordHash, Role role, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        name = name.Trim();
        if (name.Length > NameMaxLength)
            throw new DomainException($"Name cannot be longer than {NameMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        if (!Enum.IsDefined(role))
            throw new DomainException("Role is not valid.");

        return new User
        {
            Name = name,
            Email = NormalizeEmail(email),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = utcNow
        };
    }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        email = email.Trim().ToLowerInvariant();

        var at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1 || email.Contains(' '))
            throw new DomainException("Email is not valid.");

        if (email.Length > EmailMaxLength)
            throw new DomainException($"Email cannot be longer than {EmailMaxLength} characters.");

        return email;
    }

    public RefreshToken IssueRefreshToken(string tokenHash, DateTime utcNow, DateTime expiresAt)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Refresh token hash is required.");

        if (expiresAt <= utcNow)
            throw new DomainException("Refresh token expiry must be in the future.");

        var token = new RefreshToken(Id, tokenHash, utcNow, expiresAt);
        _refreshTokens.Add(token);
        return token;
    }

    /// <summary>
    /// Revokes the given refresh token so it can be exchanged only once.
    /// Fails if the token does not belong to this user or is no longer active.
    /// </summary>
    public void UseRefreshToken(string tokenHash, DateTime utcNow)
    {
        EnsureNotDeleted();

        var token = _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (token is null || !token.IsActive(utcNow))
            throw new DomainException("Refresh token is invalid or expired.");

        token.Revoke(utcNow);
    }

    public void RevokeAllRefreshTokens(DateTime utcNow)
    {
        foreach (var token in _refreshTokens)
            token.Revoke(utcNow);
    }

    /// <summary>Soft delete: the row is kept, but the user can no longer log in and is hidden from queries.</summary>
    public void Delete(DateTime utcNow)
    {
        if (IsDeleted)
            throw new DomainException("User is already deleted.");

        IsDeleted = true;
        DeletedAt = utcNow;
        RevokeAllRefreshTokens(utcNow);
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("User is deleted.");
    }
}
