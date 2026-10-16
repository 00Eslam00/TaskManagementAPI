using TaskManagement.Domain.Users;

namespace TaskManagement.Application.Abstractions;

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}

public record AccessToken(string Value, DateTime ExpiresAt);
