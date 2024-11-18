using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.AddApplicationInsights(
    configureTelemetryConfiguration: telemetryConfig => telemetryConfig.ConnectionString =
        "InstrumentationKey=47da8b49-b36a-42dc-b9a0-94032f40902e;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/;LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/;ApplicationId=96350192-e66a-48a7-a2c4-d1eeef70184f",
    loggerOptions => 
    {
        loggerOptions.IncludeScopes = true;
    });

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestHeaders.Add("sec-ch-ua");
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
    logging.CombineLogs = true;
});
//builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpLogging();

// Configure the HTTP request pipeline.

app.MapGet("/ping", (ILogger<Program> logger) =>
{
    using var _ = logger.BeginScope(new Dictionary<string, object>
    {
        ["userEmail"] = "test@test.com",
        ["CorrelationId"] = Guid.NewGuid(),
    });

    logger.LogInformation("Hello World! OpenTelemetry Trace: {activityId} {timeNow}",
        Activity.Current?.Id, DateTime.Now);

    return $"Hello World! OpenTelemetry Trace: {Activity.Current?.Id}";
});

app.MapGet("/test", async (ILogger<Program> logger, TelemetryClient telemetryClient) =>
{
    //var dependencyTelemetry = new DependencyTelemetry();
    using var activity = telemetryClient.StartOperation<DependencyTelemetry>("GettingStock");
    activity.Telemetry.Type = "InProc";

    using var client = new HttpClient();
    _ = await client.GetAsync("https://www.bing.com/");

    await Task.Delay(500);

    activity?.Telemetry.Context.GlobalProperties?.Add("ProductId", Random.Shared.Next().ToString());

    telemetryClient.TrackTrace("ProductStockCalculationCompleted");
    // activity?.Telemetry.(new ActivityEvent("ProductStockCalculationCompleted"));

    return TypedResults.Ok("Stock updated");
});

app.MapPost("/validate", async ([FromBody] Person person, TelemetryClient telemetryClient, HttpContext context) =>
{
    // This does not work with appinsight telemetry
    //Activity.Current?.AddTag("email", person.Email);
    //Activity.Current?.AddBaggage("email", person.Email);

    var requestTelemetry = context.Features.Get<RequestTelemetry>();
    requestTelemetry?.Properties.Add("email", person.Email);

    var dependencyTelemetry = new DependencyTelemetry()
    {
        Name = "ValidateCredit",
        Type = "InProc",
        Context =
        {
            GlobalProperties = 
            {
                ["email"] = person.Email
            }
        }
    };
    //dependencyTelemetry.Context.GlobalProperties.Add(new("email", person.Email));
    using (var _ = telemetryClient.StartOperation(dependencyTelemetry))
    {
        if (person.Age < 20)
        {
            dependencyTelemetry.Success = false;
            dependencyTelemetry.Context.GlobalProperties.Add("otel.status_description", "Invalid Age");
            requestTelemetry?.Properties.Add("otel.status_description", "Invalid Age");
            return Results.BadRequest("Invalid Age");
        }

        if (person.Location != "US")
        {
            dependencyTelemetry.Success = false;
            dependencyTelemetry.Context.GlobalProperties.Add("otel.status_description", "Invalid location");
            requestTelemetry?.Properties.Add("otel.status_description", "Invalid location");
            return Results.BadRequest("Invalid location");
        }

        if (person.CreditScore < 75M)
        {
            dependencyTelemetry.Success = false;
            dependencyTelemetry.Context.GlobalProperties.Add("otel.status_description", "Invalid credit score");
            requestTelemetry?.Properties.Add("otel.status_description", "Invalid credit score");
            return Results.BadRequest("Invalid credit score");
        }
    }

    using var client = new HttpClient();
    _ = await client.GetAsync("https://www.bing.com/");
    return Results.Ok("Valid credit");
});

app.Logger.StartingApp();

app.Run();

internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Starting the app...")]
    public static partial void StartingApp(this ILogger logger);
}

public record Person(string Email, int Age, string Location, decimal CreditScore);