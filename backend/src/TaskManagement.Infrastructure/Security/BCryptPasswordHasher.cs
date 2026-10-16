using TaskManagement.Application.Abstractions;

namespace TaskManagement.Infrastructure.Security;

// BCrypt generates a random salt per password and stores it inside the hash
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
