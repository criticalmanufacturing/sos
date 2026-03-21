using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Orchestration;
using Cmf.Cli.Plugin.Sos.Abstractions;
using Cmf.Cli.Plugin.Sos.Utilities;

namespace Cmf.Cli.Plugin.Sos.Infrastructure;

/// <summary>
/// Node.js runtime operations
/// </summary>
public sealed class NodeJsSosOperations : ISosOperations
{
    private readonly NodeJsDumpOrchestrator _dumpOrchestrator;
    private readonly NodeJsRemoteDebugOrchestrator _remoteDebugOrchestrator;

    public NodeJsSosOperations(KubeCliRunner kube)
    {
        _dumpOrchestrator = new NodeJsDumpOrchestrator(kube);
        _remoteDebugOrchestrator = new NodeJsRemoteDebugOrchestrator(kube);
    }

    public void Dump(string pod, string output, string pid, string? container, string? ns, string image)
    {
        _dumpOrchestrator.Execute(pod, output, pid, container, ns, image);
    }

    public void RuntimeMetrics(string pod, string output, string pid, string format, int duration, string counters, string? container, string? ns, string image)
    {
        Log.Warning($"RuntimeMetrics cannot be executed on a Node.js pod. Pod={pod}, namespace={ns ?? "(default)"}.");
    }

    public void RemoteDebug(string pod, string pid, string? container, string? ns, string image)
    {
        _remoteDebugOrchestrator.Execute(pod, pid, container, ns, image);
    }
}
