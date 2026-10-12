using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Users;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    // soft-deleted users are never returned in this query.
    private IQueryable<User> ActiveUsers => _context.Users.Where(u => !u.IsDeleted);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ActiveUsers.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        ActiveUsers.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        ActiveUsers
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash), cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await ActiveUsers.AsNoTracking().OrderBy(u => u.CreatedAt).ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        ActiveUsers.AnyAsync(u => u.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        ActiveUsers.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<bool> AnyAdminAsync(CancellationToken cancellationToken = default) =>
        ActiveUsers.AnyAsync(u => u.Role == Role.Admin, cancellationToken);

    public void Add(User user) => _context.Users.Add(user);
}
