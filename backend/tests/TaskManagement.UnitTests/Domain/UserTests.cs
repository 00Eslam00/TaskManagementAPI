using TaskManagement.Domain.Common;
using TaskManagement.Domain.Users;

namespace TaskManagement.UnitTests.Domain;

public class UserTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private static User NewUser(string email = "jane@example.com") =>
        User.Create("Jane", email, "hashed-password", Role.User, Now);

    [Fact]
    public void Create_normalizes_email_and_trims_name()
    {
        var user = User.Create("  Jane  ", "  Jane@Example.COM ", "hashed-password", Role.User, Now);

        Assert.Equal("Jane", user.Name);
        Assert.Equal("jane@example.com", user.Email);
        Assert.Equal(Role.User, user.Role);
        Assert.Equal(Now, user.CreatedAt);
        Assert.False(user.IsDeleted);
        Assert.False(user.IsAdmin);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("jane@")]
    [InlineData("jane@@example.com")]
    [InlineData("jane doe@example.com")]
    public void Create_rejects_invalid_email(string email)
    {
        Assert.Throws<DomainException>(() => NewUser(email));
    }

    [Fact]
    public void Create_rejects_empty_name()
    {
        Assert.Throws<DomainException>(() => User.Create(" ", "jane@example.com", "hash", Role.User, Now));
    }

    [Fact]
    public void Refresh_token_can_be_used_only_once()
    {
        var user = NewUser();
        user.IssueRefreshToken("hash-1", Now, Now.AddDays(7));

        user.UseRefreshToken("hash-1", Now.AddMinutes(1));

        Assert.Throws<DomainException>(() => user.UseRefreshToken("hash-1", Now.AddMinutes(2)));
    }

    [Fact]
    public void Expired_refresh_token_is_rejected()
    {
        var user = NewUser();
        user.IssueRefreshToken("hash-1", Now, Now.AddDays(7));

        Assert.Throws<DomainException>(() => user.UseRefreshToken("hash-1", Now.AddDays(8)));
    }

    [Fact]
    public void Unknown_refresh_token_is_rejected()
    {
        Assert.Throws<DomainException>(() => NewUser().UseRefreshToken("missing", Now));
    }

    [Fact]
    public void Delete_marks_user_deleted_and_revokes_refresh_tokens()
    {
        var user = NewUser();
        var token = user.IssueRefreshToken("hash-1", Now, Now.AddDays(7));

        user.Delete(Now.AddHours(1));

        Assert.True(user.IsDeleted);
        Assert.Equal(Now.AddHours(1), user.DeletedAt);
        Assert.False(token.IsActive(Now.AddHours(2)));
    }

    [Fact]
    public void Delete_twice_is_rejected()
    {
        var user = NewUser();
        user.Delete(Now);

        Assert.Throws<DomainException>(() => user.Delete(Now));
    }
}
