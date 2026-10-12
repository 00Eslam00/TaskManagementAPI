namespace TaskManagement.Application.Users;

public interface IUserService
{
    Task<UserResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, Guid deletedByUserId, CancellationToken cancellationToken = default);
}
