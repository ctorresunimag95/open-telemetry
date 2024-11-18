using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenTelemetry.Logging.Processors;

public class ActivityProcessor(IHostEnvironment hostEnvironment) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        base.OnEnd(data);


        var definedAttributes = new List<KeyValuePair<string, object>>
        {
            new("ApplicationName", Constants.AppName),
            new("Environment", hostEnvironment.EnvironmentName),
            new("ProcessID", Environment.ProcessId),
            new("DotnetFramework", RuntimeInformation.FrameworkDescription),
            new("Runtime", RuntimeInformation.RuntimeIdentifier),
        };

        foreach ( var attribute in definedAttributes)
        {
            data?.AddTag(attribute.Key, attribute.Value);
        }
    }
}

//public class MetricProcessor(IHostEnvironment hostEnvironment) : BaseProcessor<Metric>
//{
//    public override void OnEnd(Metric data)
//    {
//        base.OnEnd(data);


//        var definedAttributes = new List<KeyValuePair<string, object>>
//        {
//            new("ApplicationName", Constants.AppName),
//            new("Environment", hostEnvironment.EnvironmentName),
//            new("ProcessID", Environment.ProcessId),
//            new("DotnetFramework", RuntimeInformation.FrameworkDescription),
//            new("Runtime", RuntimeInformation.RuntimeIdentifier),
//        };

//        foreach (var attribute in definedAttributes)
//        {
//            data?.(attribute.Key, attribute.Value);
//        }
//    }
//}