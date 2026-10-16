namespace TaskManagement.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
}
