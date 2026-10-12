using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Users;

namespace TaskManagement.Application.Users;

public class RegisterRequest
{
    [Required, MaxLength(User.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(User.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

// Used by an admin to add a user. Unlike self-registration, the role can be chosen.
public class CreateUserRequest
{
    [Required, MaxLength(User.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(User.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [EnumDataType(typeof(Role))]
    public Role Role { get; set; } = Role.User;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record UserResponse(Guid Id, string Name, string Email, Role Role, DateTime CreatedAt);

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User)
{
    public string TokenType => "Bearer";
}

public static class UserMappings
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.CreatedAt);
}
