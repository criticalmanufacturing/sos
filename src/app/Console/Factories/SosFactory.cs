using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Abstractions;
using Cmf.Cli.Plugin.Sos.Infrastructure;
using Cmf.Cli.Plugin.Sos.Utilities;
using Cmf.Cli.Plugin.Sos.Runtime;

namespace Cmf.Cli.Plugin.Sos.Factories;

/// <summary>
/// Creates the appropriate runtime-specific SoS operations (dump, counters, etc.)
/// based on pod discovery (app.kubernetes.io/name -> runtime).
/// Used by all SoS commands (dump, dotnetCounters, etc.).
/// </summary>
public sealed class SosFactory
{
    private readonly KubeCliRunner _kube;
    private readonly PodInspector _inspector;

    /// <summary>
    /// The runtime resolved for the current pod.
    /// </summary>
    public AppRuntime CurrentRuntime { get; private set; }

    public SosFactory(KubeCliRunner kube)
    {
        _kube = kube;
        _inspector = new PodInspector(kube);
    }

    /// <summary>
    /// Resolves runtime from pod (labels) and returns the matching operations object.
    /// </summary>
    public ISosOperations CreateForPod(string pod, string? ns, string operation)
    {
        var appName = _inspector.GetAppName(pod, ns);
        var runtime = AppRuntimeRegistry.GetRuntime(appName);

        if(runtime.Equals(AppRuntime.Unknown)) 
        {
            throw new InvalidOperationException($"Application {appName} is not a valid application for {operation} operation");
        }

        Log.Information($"Pod app label: {appName ?? "(none)"} -> runtime: {runtime}");
        CurrentRuntime = runtime;

        return Create(runtime);
    }

    /// <summary>
    /// This will decise if the operations to be executed will be done in what framework.
    /// </summary>
    public ISosOperations Create(AppRuntime runtime)
    {
        return runtime switch
        {
            AppRuntime.Dotnet => new DotNetSosOperations(_kube),
            AppRuntime.NodeJs => new NodeJsSosOperations(_kube),
            _ => new DotNetSosOperations(_kube)
        };
    }
}
