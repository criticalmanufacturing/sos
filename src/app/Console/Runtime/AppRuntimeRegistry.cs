namespace Cmf.Cli.Plugin.Sos.Runtime;

/// <summary>
/// Maps app.kubernetes.io/name label values to runtime (Dotnet vs NodeJs).
/// Extend this mapping as new apps are onboarded.
/// </summary>
public static class AppRuntimeRegistry
{
    private static readonly HashSet<string> NodeJsApps = new(StringComparer.OrdinalIgnoreCase)
    {
        "clickhouse-ui",
        "connectiot-manager",
        "securityportal",
        // Add other Node.js app labels here.
    };

    private static readonly HashSet<string> DotnetApps = new(StringComparer.OrdinalIgnoreCase)
    {
        "host",
        "data-manager",
        "envmanager",
        "epf-alarm-mng-at",
        "epf-alarm-mng-erh",
        "epf-alarm-mng-mes-eh",
        "housekeeper",
        "housekeeper-cdm-builder",
        "mcad",
        "mes-scheduler",
        "mlplatformagent",
        "mlplatformtraining",
        "traefik-forwardauth",
        // Add other dotnet app labels here.
    };

    /// <summary>
    /// Determines the application runtime by checking if the provided name contains any known runtime identifiers as a substring.
    /// This is a flexible approach suitable for UI scenarios where the full pod name is available.
    /// For example, a pod named 'host-abc-123' will be correctly identified as a .NET runtime because its name contains 'host'.
    /// </summary>
    /// <param name="podName">The full name of the pod.</param>
    /// <returns>The detected AppRuntime, or Unknown if no identifier is matched.</returns>
    public static AppRuntime GetRuntimeFromPodName(string? podName)
    {
        if (string.IsNullOrWhiteSpace(podName))
        {
            return AppRuntime.Unknown;
        }

        if (DotnetApps.Any(id => podName.Contains(id, StringComparison.OrdinalIgnoreCase)))
        {
            return AppRuntime.Dotnet;
        }

        if (NodeJsApps.Any(id => podName.Contains(id, StringComparison.OrdinalIgnoreCase)))
        {
            return AppRuntime.NodeJs;
        }

        return AppRuntime.Unknown;
    }

    /// <summary>
    /// Resolves runtime for the given app name (from label app.kubernetes.io/name).
    /// Unknown apps default to Dotnet.
    /// </summary>
    public static AppRuntime GetRuntime(string? appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            return AppRuntime.Unknown;
        }
        if(NodeJsApps.Contains(appName)) 
        {
            return AppRuntime.NodeJs;
        } 
        else if (DotnetApps.Contains(appName))
        { 
            return AppRuntime.Dotnet;
        }
        return AppRuntime.Unknown;
    }
}
