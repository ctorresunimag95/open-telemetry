using System.Diagnostics;

namespace OpenTelemetry.Logging.Processors;

public class CustomErrorActivityProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        base.OnEnd(activity);

        var isError = activity.Tags!
                .Any(t => t is { Key: "otel.status_code", Value: "ERROR" });


        if (!isError) return;

        activity.SetStatus(ActivityStatusCode.Error);
    }
}