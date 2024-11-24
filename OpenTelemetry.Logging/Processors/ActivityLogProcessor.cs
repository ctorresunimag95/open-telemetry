using OpenTelemetry.Logs;
using System.Runtime.InteropServices;

namespace OpenTelemetry.Logging.Processors;

public class ActivityLogProcessor(IHostEnvironment hostEnvironment) : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
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

        var attributes = data.Attributes is null ? definedAttributes : data.Attributes!.Concat(definedAttributes);

        data.Attributes = attributes.ToList().AsReadOnly()!;
    }
}
