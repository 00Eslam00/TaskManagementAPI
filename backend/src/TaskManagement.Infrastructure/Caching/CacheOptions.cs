namespace TaskManagement.Infrastructure.Caching;

public class CacheOptions
{
    public const string SectionName = "Cache";

    public int TaskExpirationMinutes { get; set; } = 10;

    public int RetryAfterFailureSeconds { get; set; } = 30;
}
