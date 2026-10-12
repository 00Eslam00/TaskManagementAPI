using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Infrastructure.Security;

public class RefreshTokenFactory : IRefreshTokenFactory
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenFactory(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public GeneratedRefreshToken Create()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(_options.RefreshTokenDays);

        return new GeneratedRefreshToken(value, Hash(value), expiresAt);
    }

    public string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
