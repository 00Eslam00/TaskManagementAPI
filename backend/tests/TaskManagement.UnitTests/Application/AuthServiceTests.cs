using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Users;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Users;

namespace TaskManagement.UnitTests.Application;

public class AuthServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();
    private readonly Mock<IRefreshTokenFactory> _refreshTokenFactory = new();
    private readonly FixedTimeProvider _clock = new(Now);
    private readonly AuthService _service;
    private readonly User _user = User.Create("Jane", "jane@example.com", "stored-hash", Role.User, Now);

    private int _issuedRefreshTokens;

    public AuthServiceTests()
    {
        _users.Setup(r => r.GetByEmailAsync("jane@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        // The fake repository finds the user by any refresh token hash the user holds.
        _users.Setup(r => r.GetByRefreshTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string hash, CancellationToken _) => _user.RefreshTokens.Any(t => t.TokenHash == hash) ? _user : null);

        _passwordHasher.Setup(h => h.Verify("Passw0rd!", "stored-hash")).Returns(true);

        _tokenGenerator.Setup(g => g.Generate(_user)).Returns(() => new AccessToken("access-token", _clock.UtcNow.AddMinutes(60)));


        _refreshTokenFactory.Setup(f => f.Hash(It.IsAny<string>())).Returns<string>(token => $"hash:{token}");
        _refreshTokenFactory.Setup(f => f.Create()).Returns(() =>
        {
            var value = $"refresh-{++_issuedRefreshTokens}";
            return new GeneratedRefreshToken(value, $"hash:{value}", _clock.UtcNow.AddDays(7));
        });

        _service = new AuthService(
            _users.Object,
            _userService.Object,
            _unitOfWork.Object,
            _passwordHasher.Object,
            _tokenGenerator.Object,
            _refreshTokenFactory.Object,
            _clock,
            NullLogger<AuthService>.Instance);
    }

    private Task<AuthResponse> LoginAsync() =>
        _service.LoginAsync(new LoginRequest { Email = "Jane@Example.com", Password = "Passw0rd!" });

    private Task<AuthResponse> RefreshAsync(string refreshToken) =>
        _service.RefreshAsync(new RefreshTokenRequest { RefreshToken = refreshToken });

    [Fact]
    public async Task Register_always_creates_a_regular_user()
    {
        await _service.RegisterAsync(new RegisterRequest { Name = "Jane", Email = "jane@example.com", Password = "Passw0rd!" });

        _userService.Verify(s => s.CreateAsync(
            It.Is<CreateUserRequest>(r => r.Role == Role.User && r.Email == "jane@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_returns_tokens_and_stores_only_the_refresh_token_hash()
    {
        var response = await LoginAsync();

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-1", response.RefreshToken);
        Assert.Equal(Now.AddDays(7), response.RefreshTokenExpiresAt);
        Assert.Equal(_user.Id, response.User.Id);

        var stored = Assert.Single(_user.RefreshTokens);
        Assert.Equal("hash:refresh-1", stored.TokenHash);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_rejected()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "wrong" }));

        Assert.Empty(_user.RefreshTokens);
    }

    [Theory]
    [InlineData("nobody@example.com")]
    [InlineData("not-an-email")]
    public async Task Login_with_an_unknown_or_malformed_email_is_rejected_with_the_same_error(string email)
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest { Email = email, Password = "Passw0rd!" }));

        Assert.Equal("Invalid email or password.", exception.Message);
    }

    [Fact]
    public async Task Refresh_replaces_the_refresh_token_with_a_new_one()
    {
        await LoginAsync();

        var response = await RefreshAsync("refresh-1");

        Assert.Equal("refresh-2", response.RefreshToken);
        Assert.False(_user.RefreshTokens.Single(t => t.TokenHash == "hash:refresh-1").IsActive(Now));
        Assert.True(_user.RefreshTokens.Single(t => t.TokenHash == "hash:refresh-2").IsActive(Now));
    }

    [Fact]
    public async Task Refresh_with_an_already_used_token_is_rejected_and_ends_all_sessions()
    {
        await LoginAsync();
        await RefreshAsync("refresh-1");

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync("refresh-1"));

        Assert.All(_user.RefreshTokens, token => Assert.False(token.IsActive(Now)));
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync("refresh-2"));
    }

    [Fact]
    public async Task Refresh_with_an_expired_token_is_rejected()
    {
        await LoginAsync();
        _clock.Advance(TimeSpan.FromDays(8));

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync("refresh-1"));
    }

    [Fact]
    public async Task Refresh_with_an_unknown_token_is_rejected()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync("never-issued"));
    }

    [Fact]
    public async Task Logout_revokes_every_refresh_token()
    {
        await LoginAsync();
        await LoginAsync();
        _users.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);

        await _service.LogoutAsync(_user.Id);

        Assert.Equal(2, _user.RefreshTokens.Count);
        Assert.All(_user.RefreshTokens, token => Assert.False(token.IsActive(Now)));
    }
}
