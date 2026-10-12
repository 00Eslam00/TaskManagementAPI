namespace TaskManagement.Infrastructure.BackgroundProcessing;

public class BackgroundProcessingOptions
{
    public const string SectionName = "BackgroundProcessing";

    public int SimulatedWorkSeconds { get; set; } = 5;
}
