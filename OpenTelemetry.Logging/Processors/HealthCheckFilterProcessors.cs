using OpenTelemetry.Logs;
using System.Diagnostics;

namespace OpenTelemetry.Logging.Processors;

public class HealthCheckActivityFilterProcessors : ActivityFilteringProcessor
{
    private const string UrlPath = "url.path";
    private const string HealthEndpoint = "/health";

    protected override Func<Activity, bool> Filter => activity =>
    {
        return !(activity.Kind == ActivityKind.Server &&
                 activity.Tags.Any(tag => tag.Key == UrlPath && tag.Value?.Contains(HealthEndpoint) is true));
    };
}

public class HealthCheckLogFilterProcessors : LogFilteringProcessor
{
    private const string UrlPath = "path";
    private const string HealthEndpoint = "/health";

    protected override Func<LogRecord, bool> Filter => logRecord =>
    {
        return !(logRecord.CategoryName?.Equals("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware",
                     StringComparison.OrdinalIgnoreCase) is true &&
                 logRecord.Attributes?.Any(tag =>
                         tag.Key.Equals(UrlPath, StringComparison.OrdinalIgnoreCase) &&
                         tag.Value?.ToString()?.Contains(HealthEndpoint) is true)
                     is true);
    };
}