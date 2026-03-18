using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Runtime;
using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public class ProcessInspector
{
    private readonly KubeCliRunner _kube;

    public ProcessInspector(KubeCliRunner kube)
    {
        _kube = kube;
    }

    /// <summary>
    /// This function will automatically detect the PID of the process we want to target based on the runtime.
    /// This can be used by multiple operations.
    /// </summary>
    public string ResolvePid(string pod, string? container, string? ns, AppRuntime runtime)
    {
        var processName = runtime switch
        {
            AppRuntime.Dotnet => "dotnet",
            AppRuntime.NodeJs => "node",
            _ => null
        };

        if (processName is null)
        {
            throw new CliException("Runtime provided is null, are you sure this is a pod supported for SOS?");
        }

        var shellCommand = $"pgrep -x {processName}";

        var args = container is null
            ? new[] { "exec", pod, "-n", ns!, "--", "sh", "-c", shellCommand }
            : new[] { "exec", pod, "-n", ns!, "-c", container, "--", "sh", "-c", shellCommand };

        var result = _kube.Run(args);

        var pid = result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(pid))
        {
            Log.Debug($"Auto-resolved PID {pid} for {runtime}.");
            return pid.Trim();
        }

        Log.Warning($"Could not find {processName}. Falling back to PID 1.");
        return "1";
    }
}