namespace TaskManagement.Application.Abstractions;

public interface IRefreshTokenFactory
{
    GeneratedRefreshToken Create();

    string Hash(string token);
}

public record GeneratedRefreshToken(string Value, string Hash, DateTime ExpiresAt);
