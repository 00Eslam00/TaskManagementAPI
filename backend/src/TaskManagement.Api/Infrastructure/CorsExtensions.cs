namespace TaskManagement.Api.Infrastructure;

public static class CorsExtensions
{
    public const string FrontendPolicy = "Frontend";

    private const string AllowedOriginsKey = "Cors:AllowedOrigins";

    /// <summary>
    /// Lets the React frontend call the API from the browser. Only the origins listed in
    /// "Cors:AllowedOrigins" are allowed; every other site is still blocked by the browser.
    /// </summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(FrontendPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            // The frontend sends the token in the Authorization header, not in a cookie,
            // so credentials are not needed.
            .WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PATCH", "DELETE")));

        return services;
    }
}
