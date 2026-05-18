using Cmf.Cli.Plugin.Sos.Orchestration;
using Cmf.Cli.Plugin.Sos.Abstractions;
using Cmf.Cli.Plugin.Sos.Utilities;
using Cmf.Cli.Plugin.Sos.Commands;
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
    private readonly InteractiveShellCommand interactiveShellCommand;

    public DotNetSosOperations(KubeCliRunner kube)
    {
        _dumpOrchestrator = new DotnetDumpOrchestrator(kube);
        _runtimeMetricsOrchestrator = new RuntimeMetricsOrchestrator(kube);
        _remoteDebugOrchestrator = new DotnetRemoteDebugOrchestrator(kube);
        interactiveShellCommand = new();
    }

    public void Dump(string pod, string output, string pid, string? container, string ns, string image, int sessionDuration = 20)
    {
        _dumpOrchestrator.Execute(pod, output, pid, container, ns, image, sessionDuration);
    }

    public void RuntimeMetrics(string pod, string output, string pid, string format, int duration, string counters, string? container, string? ns, string image, int sessionDuration = 20)
    {
        _runtimeMetricsOrchestrator.Execute(pod, output, pid, container, ns, image, format, duration, counters, sessionDuration);
    }

    public void RemoteDebug(string pod, string pid, string? container, string? ns, string image, string? sourceCodePath = null, int sessionDuration = 20)
    {
        if (string.IsNullOrWhiteSpace(sourceCodePath))
        {
            throw new CliException("Source Code path is required for .NET remote debugging. Please provide it via UI or command line arguments.");
        }

        _remoteDebugOrchestrator.Execute(pod, container, ns, sourceCodePath, sessionDuration);
    }

    public void InteractiveShell(string pod, string ns, string? container, string image, int sessionDuration = 20)
    {
        interactiveShellCommand.Execute(pod, ns, container, image, sessionDuration);
    }
}
