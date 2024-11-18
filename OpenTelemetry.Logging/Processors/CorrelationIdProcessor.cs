using OpenTelemetry.Logs;
using System.Diagnostics;

namespace OpenTelemetry.Logging.Processors;

public class CorrelationIdActivityProcessor(IHttpContextAccessor httpContextAccessor) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        base.OnEnd(data);

        var correlationId = httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"];

        if (!string.IsNullOrWhiteSpace(correlationId)
            && !data.Tags.Any(t => t.Key.Equals("CorrelationId")))
        {
            data.AddTag("CorrelationId", correlationId);
        }
    }
}

public class CorrelationIdLogProcessor(IHttpContextAccessor httpContextAccessor) : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        base.OnEnd(data);

        var correlationId = httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"];

        if (string.IsNullOrWhiteSpace(correlationId)
            || data.Attributes?.Any(t => t.Key.Equals("CorrelationId")) is not false) return;

        var correlationData = new List<KeyValuePair<string, object>>()
        {
            new("CorrelationId", correlationId)
        };

        var attributes = data.Attributes is null ? correlationData : data.Attributes!.Concat(correlationData);

        data.Attributes = attributes.ToList().AsReadOnly()!;
    }
}
