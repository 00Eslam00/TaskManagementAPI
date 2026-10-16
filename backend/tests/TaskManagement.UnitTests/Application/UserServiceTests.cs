using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Users;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Users;

namespace TaskManagement.UnitTests.Application;

public class UserServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(password => $"hashed:{password}");

        _service = new UserService(
            _users.Object,
            _unitOfWork.Object,
            _passwordHasher.Object,
            new FixedTimeProvider(Now),
            NullLogger<UserService>.Instance);
    }

    private User ExistingUser()
    {
        var user = User.Create("Jane", "jane@example.com", "hash", Role.User, Now);
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task Create_stores_a_hashed_password_and_a_normalized_email()
    {
        User? saved = null;
        _users.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(user => saved = user);

        var response = await _service.CreateAsync(new CreateUserRequest
        {
            Name = "Jane",
            Email = "Jane@Example.com",
            Password = "Passw0rd!",
            Role = Role.Admin
        });

        Assert.NotNull(saved);
        Assert.Equal("hashed:Passw0rd!", saved.PasswordHash);
        Assert.Equal("jane@example.com", response.Email);
        Assert.Equal(Role.Admin, response.Role);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_rejects_an_email_that_is_already_used()
    {
        _users.Setup(r => r.EmailExistsAsync("jane@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(new CreateUserRequest
        {
            Name = "Jane",
            Email = "JANE@example.com",
            Password = "Passw0rd!"
        }));

        _users.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Delete_soft_deletes_the_user()
    {
        var user = ExistingUser();

        await _service.DeleteAsync(user.Id, deletedByUserId: Guid.NewGuid());

        Assert.True(user.IsDeleted);
        Assert.Equal(Now, user.DeletedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_rejects_deleting_your_own_account()
    {
        var user = ExistingUser();

        await Assert.ThrowsAsync<DomainException>(() => _service.DeleteAsync(user.Id, deletedByUserId: user.Id));

        Assert.False(user.IsDeleted);
    }

    [Fact]
    public async Task Delete_reports_an_unknown_user_as_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetProfile_reports_an_unknown_user_as_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetProfileAsync(Guid.NewGuid()));
    }
}
