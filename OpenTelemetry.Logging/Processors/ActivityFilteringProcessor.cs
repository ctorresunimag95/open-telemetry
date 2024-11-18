using OpenTelemetry.Logs;
using System.Diagnostics;

namespace OpenTelemetry.Logging.Processors;

public abstract class ActivityFilteringProcessor : BaseProcessor<Activity>
{
    protected abstract Func<Activity, bool> Filter { get; }

    public override void OnEnd(Activity activity)
    {
        base.OnEnd(activity);

        // Bypass export if the Filter returns false.
        if (!Filter(activity))
        {
            activity.IsAllDataRequested = false;
            activity.ActivityTraceFlags = ActivityTraceFlags.None;
        }
    }
}

// Not working
public abstract class LogFilteringProcessor : BaseProcessor<LogRecord>
{
    protected abstract Func<LogRecord, bool> Filter { get; }

    public override void OnEnd(LogRecord log)
    {
        // Bypass export if the Filter returns false.
        if (!Filter(log))
        {
            log.LogLevel = LogLevel.Debug;
        }
    }
}
