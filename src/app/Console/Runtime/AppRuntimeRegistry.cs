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
