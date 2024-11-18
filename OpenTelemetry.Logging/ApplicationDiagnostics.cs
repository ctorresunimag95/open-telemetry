using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenTelemetry.Logging;

public static class ApplicationDiagnostics
{
    public static readonly ActivitySource ActivitySource = new(Constants.AppName, "1.0.0");

    public static ActivityTagsCollection DefaultTags = new(new List<KeyValuePair<string, object>>
    {
        new("ApplicationName", Constants.AppName),
        new("ProcessID", Environment.ProcessId),
        new("DotnetFramework", RuntimeInformation.FrameworkDescription),
        new("Runtime", RuntimeInformation.RuntimeIdentifier),
    }!);
}
