using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpLogging;
using OpenTelemetry.Logging;
using OpenTelemetry.Logging.Meters;
using OpenTelemetry.Logging.Middlewares;
using OpenTelemetry.Logging.Processors;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Logging.ClearProviders();

var resourceBuilder = ResourceBuilder
        .CreateDefault()
        .AddService(Constants.AppName,
            serviceVersion: "1.0.0",
            serviceNamespace: "MyLearning.Company")
        .AddTelemetrySdk()
        .AddAttributes(new Dictionary<string, object>
        {
            ["service.division"] = "Avengers",
            ["service.teamName"] = "MyAwesomeTeam",
            ["environment"] =
                builder.Environment.EnvironmentName.ToLowerInvariant()
        })
    ;

builder.Logging
    .AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
    })
    ;

// Logging & Open telemetry
var jaegerEndpoint = new Uri(Environment.GetEnvironmentVariable("Jaeger_Endpoint")!);
var otlpEnpoint = new Uri(Environment.GetEnvironmentVariable("OTLP_Endpoint")!);
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(Constants.AppName))
    .WithLogging(logging =>
    {
        logging.SetResourceBuilder(resourceBuilder)
            //.AddConsoleExporter()
            ;

        logging.AddOtlpExporter(options =>
            options.Endpoint = otlpEnpoint
        );

        logging.AddProcessor<ActivityLogProcessor>()
            .AddProcessor<CorrelationIdLogProcessor>()
            ;
    }, options =>
    {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.ParseStateValues = true;
    })
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder);

        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                // One way to filter requests
                //options.Filter = httpContext =>
                //    !httpContext.Request.Path.Value?.EndsWith("/health") ?? false;
            })
            .AddHttpClientInstrumentation()
            ;
        tracing.AddOtlpExporter(options =>
                options.Endpoint = otlpEnpoint
            )
            // .AddConsoleExporter()
            ;

        tracing
            .AddProcessor<ActivityProcessor>()
            .AddProcessor<CustomErrorActivityProcessor>()
            .AddProcessor<CorrelationIdActivityProcessor>()
            .AddProcessor<HealthCheckActivityFilterProcessors>()
            ;

        tracing.AddSource(Constants.AppName);
    })
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(resourceBuilder);

        metrics
            .AddMeter(EntryMeter.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            ;

        metrics.AddOtlpExporter(options =>
            options.Endpoint = otlpEnpoint
        );

        // metrics.AddPrometheusExporter();
    })
    // .UseAzureMonitor(options =>
    // {
    //     options.ConnectionString = Environment.GetEnvironmentVariable("AppInsight__ConnectionString");
    // })
    ;

builder.Services.AddSingleton(TracerProvider.Default.GetTracer(Constants.AppName));

// Metrics
builder.Services.AddMetrics();
builder.Services.AddSingleton<EntryMeter>();

builder.Services.AddScoped<CorrelationIdMiddleware>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestHeaders.Add("sec-ch-ua");
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
    logging.CombineLogs = true;
});
// builder.Services.AddHttpLoggingInterceptor<HttpLoggingInterceptor>();

// Error managing
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

        // Use TraceIdentifier as it is being set with the same correlation id value on the correlation middleware
        context.ProblemDetails.Extensions.TryAdd("correlationId", context.HttpContext.TraceIdentifier);

        var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

// app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseExceptionHandler();

app.UseHttpLogging();

app.MapEndpoints()
    .MapOrderEndpoints();

app.Logger.StartingApp();

app.Run();

internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Starting the app...")]
    public static partial void StartingApp(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Food `{name}` price changed to `{price}`.")]
    public static partial void FoodPriceChanged(this ILogger logger, string name, double price);
}

public static class Constants
{
    public const string AppName = "Otel.Logging";
}

public record Person(string Email, int Age, string Location, decimal CreditScore);