using Microsoft.Extensions.Logging;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Users;

namespace TaskManagement.Application.Users;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUserService _userService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IUserService userService,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        ILogger<AuthService> logger)
    {
        _users = users;
        _userService = userService;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    public Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        return _userService.CreateAsync(
            new CreateUserRequest
            {
                Name = request.Name,
                Email = request.Email,
                Password = request.Password,
                Role = Role.User
            },
            cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindByEmailAsync(request.Email, cancellationToken);

        // Same error for "unknown email" and "wrong password", so the API does not reveal which emails exist.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt");
            throw new UnauthorizedException("Invalid email or password.");
        }

        var token = _tokenGenerator.Generate(user);

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return new AuthResponse(token.Value, token.ExpiresAt, user.ToResponse());
    }

    private async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            return await _users.GetByEmailAsync(User.NormalizeEmail(email), cancellationToken);
        }
        catch (DomainException)
        {
            // A malformed email cannot belong to any user.
            return null;
        }
    }
}
