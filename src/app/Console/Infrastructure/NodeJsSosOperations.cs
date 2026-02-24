using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Abstractions;
using Cmf.Cli.Plugin.Sos.Utilities;

namespace Cmf.Cli.Plugin.Sos.Infrastructure;

/// <summary>
/// Node.js runtime operations (dump, counters, etc).
/// </summary>
public sealed class NodeJsSosOperations : ISosOperations
{
    private readonly KubeCliRunner _kube;

    public NodeJsSosOperations(KubeCliRunner kube)
    {
        _kube = kube;
    }

    public void Dump(string pod, string output, string pid, string? container, string? ns, string image)
    {
        Log.Warning("Node.js dump is not yet implemented. Use runtime-specific tooling inside the pod if needed.");
        throw new NotImplementedException("Node.js dump is not yet implemented. This pod is mapped as NodeJs; extend NodeJsSosOperations.Dump to add support.");
    }

    public void DotnetCounters(string pod, string output, string pid, string format, int duration, string counters, string? container, string? ns, string image)
    {
        Log.Warning($"dotnetCounters cannot be executed on a Node.js pod. Pod={pod}, namespace={ns ?? "(default)"}.");
    }
}
