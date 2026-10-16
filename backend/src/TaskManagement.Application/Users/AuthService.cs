using Microsoft.Extensions.Logging;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Users;

namespace TaskManagement.Application.Users;

public class AuthService : IAuthService
{
    private const string InvalidRefreshTokenMessage = "Refresh token is invalid or expired.";

    private readonly IUserRepository _users;
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenFactory _refreshTokenFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IUserService userService,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IRefreshTokenFactory refreshTokenFactory,
        TimeProvider timeProvider,
        ILogger<AuthService> logger)
    {
        _users = users;
        _userService = userService;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _refreshTokenFactory = refreshTokenFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Self-registration always creates a regular user. Only an admin can choose a role.
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

        var response = IssueTokens(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenFactory.Hash(request.RefreshToken);

        var user = await _users.GetByRefreshTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new UnauthorizedException(InvalidRefreshTokenMessage);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            user.UseRefreshToken(tokenHash, now);
        }
        catch (DomainException)
        {
            user.RevokeAllRefreshTokens(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Inactive refresh token presented for user {UserId}; all refresh tokens revoked", user.Id);
            throw new UnauthorizedException(InvalidRefreshTokenMessage);
        }

        var response = IssueTokens(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tokens refreshed for user {UserId}", user.Id);

        return response;
    }

    public async Task LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        user.RevokeAllRefreshTokens(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged out", userId);
    }

    private AuthResponse IssueTokens(User user)
    {
        var accessToken = _tokenGenerator.Generate(user);
        var refreshToken = _refreshTokenFactory.Create();

        user.IssueRefreshToken(refreshToken.Hash, _timeProvider.GetUtcNow().UtcDateTime, refreshToken.ExpiresAt);

        return new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshToken.Value,
            refreshToken.ExpiresAt,
            user.ToResponse());
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
