using System.Text.Json.Serialization;
using Serilog;
using TaskManagement.Api.Infrastructure;
using TaskManagement.Application;
using TaskManagement.Application.Abstractions;
using TaskManagement.Infrastructure;

// Minimal logger so that failures during startup, before the real logger is configured, are still recorded.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Sinks, levels and the output format are all defined in the "Serilog" section of appsettings.json.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    builder.Services.AddAuthorization();

    builder.Services
        .AddControllers()
        // Enums travel as their names ("Admin", "Pending") rather than numbers.
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddSwaggerWithJwt();

    builder.Services.AddFrontendCors(builder.Configuration);

    var app = builder.Build();

    await app.Services.InitializeDatabaseAsync();

    // One summary line per request: method, path, status code and duration.
    app.UseSerilogRequestLogging();

    // Before the exception handler and authentication, so error responses such as 401 and 409
    // also carry the CORS headers and the browser lets the frontend read their messages.
    app.UseCors(CorsExtensions.FrontendPolicy);

    app.UseExceptionHandler();
    // Gives responses without a body, such as 401 and 403, a problem-details body too.
    app.UseStatusCodePages();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
// HostAbortedException is how the EF Core command-line tools stop the host after reading the model.
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "The application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
