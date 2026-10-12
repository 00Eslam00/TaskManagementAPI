using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Tasks;
using TaskManagement.Application.Users;

namespace TaskManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITaskService, TaskService>();

        return services;
    }
}
