using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Infrastructure.Security;

namespace TaskManagement.Api.Infrastructure;

/// <summary>Reads the current user's id from the validated JWT of the request.</summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var subject = _httpContextAccessor.HttpContext?.User.FindFirst(JwtClaims.UserId)?.Value;

            return Guid.TryParse(subject, out var userId)
                ? userId
                : throw new UnauthorizedException("The request is not authenticated.");
        }
    }
}
