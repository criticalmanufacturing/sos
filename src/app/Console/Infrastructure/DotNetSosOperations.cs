using Cmf.Cli.Plugin.Sos.Orchestration;
using Cmf.Cli.Plugin.Sos.Abstractions;
using Cmf.Cli.Plugin.Sos.Utilities;
using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Infrastructure;

/// <summary>
/// .NET runtime operations
/// </summary>
public sealed class DotNetSosOperations : ISosOperations
{
    private readonly DotnetDumpOrchestrator _dumpOrchestrator;
    private readonly RuntimeMetricsOrchestrator _runtimeMetricsOrchestrator;
    private readonly DotnetRemoteDebugOrchestrator _remoteDebugOrchestrator;

    public DotNetSosOperations(KubeCliRunner kube)
    {
        _dumpOrchestrator = new DotnetDumpOrchestrator(kube);
        _runtimeMetricsOrchestrator = new RuntimeMetricsOrchestrator(kube);
        _remoteDebugOrchestrator = new DotnetRemoteDebugOrchestrator(kube);
    }

    public void Dump(string pod, string output, string pid, string? container, string? ns, string image)
    {
        _dumpOrchestrator.Execute(pod, output, pid, container, ns, image);
    }

    public void RuntimeMetrics(string pod, string output, string pid, string format, int duration, string counters, string? container, string? ns, string image)
    {
        _runtimeMetricsOrchestrator.Execute(pod, output, pid, container, ns, image, format, duration, counters);
    }

    public void RemoteDebug(string pod, string pid, string? container, string? ns, string image, string? pdbPath = null, string? sourceCodePath = null)
    {
        if (string.IsNullOrWhiteSpace(pdbPath) || string.IsNullOrWhiteSpace(sourceCodePath))
        {
            throw new CliException("PDB and Source Code paths are required for .NET remote debugging. Please provide them via UI or command line arguments.");
        }

        _remoteDebugOrchestrator.Execute(pod, pid, container, ns, pdbPath, sourceCodePath);
    }
}
