using Cmf.Cli.Plugin.Sos.Orchestration;
using Cmf.Cli.Plugin.Sos.Abstractions;
using Cmf.Cli.Plugin.Sos.Utilities;
using Cmf.CLI.Core;

namespace Cmf.Cli.Plugin.Sos.Infrastructure;

/// <summary>
/// .NET runtime operations (dump, counters, etc).
/// </summary>
public sealed class DotNetSosOperations : ISosOperations
{
    private readonly DotnetDumpOrchestrator _dumpOrchestrator;
    private readonly DotnetCountersOrchestrator _countersOrchestrator;

    public DotNetSosOperations(KubeCliRunner kube)
    {
        _dumpOrchestrator = new DotnetDumpOrchestrator(kube);
        _countersOrchestrator = new DotnetCountersOrchestrator(kube);
    }

    public void Dump(string pod, string output, string pid, string? container, string? ns, string image)
    {
        _dumpOrchestrator.Execute(pod, output, pid, container, ns, image);
    }

    public void DotnetCounters(string pod, string output, string pid, string format, int duration, string counters, string? container, string? ns, string image)
    {
        _countersOrchestrator.Execute(pod, output, pid, container, ns, image, format, duration, counters);
    }

    public void RemoteDebug(string pod, string pid, string? container, string? ns, string image)
    {
        Log.Warning($"RemoteDebug is currently supported only for Node.js pods. Pod={pod}, namespace={ns ?? "(default)"}.");
    }
}
