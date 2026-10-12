using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Abstractions;
using TaskManagement.Domain.Users;

namespace TaskManagement.Infrastructure.Persistence.Seeding;

public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;
    private readonly AdminSeedOptions _adminOptions;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        AppDbContext context,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider,
        IOptions<AdminSeedOptions> adminOptions,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _users = users;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        if (await _users.AnyAdminAsync(cancellationToken))
        {
            _logger.LogInformation("Admin user already exists, skipping seeding");
            return;
        }

        var admin = User.Create(
            _adminOptions.Name,
            _adminOptions.Email,
            _passwordHasher.Hash(_adminOptions.Password),
            Role.Admin,
            _timeProvider.GetUtcNow().UtcDateTime);

        _users.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded default admin user {Email}", admin.Email);
    }
}
