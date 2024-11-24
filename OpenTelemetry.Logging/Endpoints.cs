using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Logging;

public static class Endpoints
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapGet("/food", (ILogger<Program> logger) =>
        {
            logger.FoodPriceChanged("artichoke", 9.99);

            return "Hello from OpenTelemetry Logs!";
        });

        app.MapGet("/health", /*[HttpLogging(HttpLoggingFields.None)]*/ () => "Hello from Health!")
            .WithHttpLogging(HttpLoggingFields.None);

        app.MapGet("/ping", async (ILogger<Program> logger) =>
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

        app.MapGet("/test", async (ILogger<Program> logger) =>
        {
            using (var activity = ApplicationDiagnostics.ActivitySource.StartActivity("GettingStock",
                       ActivityKind.Client))
            {
                using var client = new HttpClient();
                _ = await client.GetAsync("https://www.bing.com/");

                await Task.Delay(500);

                activity?.SetTag("ProductId", Random.Shared.Next());
            }

            Activity.Current?.AddEvent(new ActivityEvent("ProductStockCalculationCompleted",
                tags: ApplicationDiagnostics.DefaultTags));

            return TypedResults.Ok("Stock updated");
        });

        app.MapPost("/validate", async ([FromBody] Person person) =>
        {
            Activity.Current?.AddTag("email", person.Email);

            using (var span = ApplicationDiagnostics.ActivitySource.StartActivity(name: "CheckEligibilityTracer",
                       kind: ActivityKind.Internal,
                       tags: new List<KeyValuePair<string, object>>
                       {
                           new("email", person.Email),
                       }!))
            {
                span?.AddEvent(new ActivityEvent("ValidatingCredit", tags: ApplicationDiagnostics.DefaultTags));

                if (person.Age < 20)
                {
                    span?.SetStatus(ActivityStatusCode.Error, "Invalid Age");
                    // span?.SetStatus(Status.Error.WithDescription("Invalid Age"));
                    return Results.BadRequest("Invalid Age");
                }

                if (person.Location != "US")
                {
                    span?.SetStatus(ActivityStatusCode.Error, "Invalid location");
                    // span?.SetStatus(Status.Error.WithDescription("Invalid location"));
                    return Results.BadRequest("Invalid location");
                }

                if (person.CreditScore < 75M)
                {
                    span?.SetStatus(ActivityStatusCode.Error, "Invalid credit score");
                    // span?.SetStatus(Status.Error.WithDescription("Invalid credit score"));
                    return Results.BadRequest("Invalid credit score");
                }
            }

            using var client = new HttpClient();
            _ = await client.GetAsync("https://www.bing.com/");

            return Results.Ok("Valid credit");
        });

        app.MapPost("/validate-tracer", async ([FromServices] Tracer tracer, [FromBody] Person person) =>
        {
            using (var span = tracer.StartActiveSpan(name: "CheckEligibilityTracer",
                       kind: SpanKind.Internal,
                       initialAttributes: new SpanAttributes(new List<KeyValuePair<string, object>>
                       {
                           new("email", person.Email),
                       }!)))
            {
                if (person.Age < 20)
                {
                    span?.SetStatus(Status.Error.WithDescription("Invalid Age"));
                    return Results.BadRequest("Invalid Age");
                }

                if (person.Location != "US")
                {
                    span?.SetStatus(Status.Error.WithDescription("Invalid location"));
                    return Results.BadRequest("Invalid location");
                }

                if (person.CreditScore < 75M)
                {
                    span?.SetStatus(Status.Error.WithDescription("Invalid credit score"));
                    return Results.BadRequest("Invalid credit score");
                }
            }

            using var client = new HttpClient();
            _ = await client.GetAsync("https://www.bing.com/");

            return Results.Ok("Valid credit");
        });

        app.MapGet("/failed", () => Results.Problem(title: "Invalid data!", statusCode: StatusCodes.Status400BadRequest));

        app.MapGet("/throw", () => 
        {
            var exception = new Exception("Application just crashed!");
            Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
            
            Activity.Current?.AddException(exception);
            
            return Results.Problem(title: exception.Message);
        });

        return app;
    }
}