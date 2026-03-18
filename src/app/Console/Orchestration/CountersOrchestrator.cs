using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Utilities;

namespace Cmf.Cli.Plugin.Sos.Orchestration;

/// <summary>
/// This class will orchestrate DotnetCounters operation.
/// </summary>
public class DotnetCountersOrchestrator
{
    private readonly KubeCliRunner _kube;
    public DotnetCountersOrchestrator(KubeCliRunner kube) => _kube = kube;

    /// <summary>
    /// This function will orchestrate the entire flow of dotnet-counters collection:
    /// 1. It will ensure the output path is valid and has the correct extension.
    /// 2. It will resolve the target container if not provided.
    /// 3. It will start a debug session (debug container) attached to the target container.
    /// 4. It will execute the dotnet-counters command inside the debug container, targeting the PID and collecting the output in the debug container's filesystem.
    /// 5. It will copy the output file from the debug container to the local machine.
    /// 6. It will handle cleanup of the debug session and provide informative logging throughout the process.
    /// </summary>
    public void Execute(string pod, string output, string pid, string? container, string? ns, string image, string format, int duration, string counters)
    {
        var inspector = new PodInspector(_kube);
        var session = new DebugSessionManager(_kube);

        // Enforce correct output path and extension for .NET counters (format argument)
        output = OutputChecker.ResolveOutputPath(output, pod, "."+format );

        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container)
                ? inspector.ResolveTargetContainer(pod, ns)
                : container;

            var debugContainer = session.Start(pod, targetContainer, image, ns);

            Log.Information($"Target PID: {pid}");

            string targetPath = $"/tmp/output.{format.ToLower()}";

            Log.Information($"Collecting dotnet-counters for {duration} seconds...");

            var countersArgs = new List<string>();
            if (ns != null) 
            { 
                countersArgs.Add("-n"); countersArgs.Add(ns); 
            }
            countersArgs.Add("exec"); 
            countersArgs.Add(pod); 
            countersArgs.Add("-c"); 
            countersArgs.Add(debugContainer);
            countersArgs.Add("--"); 
            countersArgs.Add("sh"); 
            countersArgs.Add("-c");

            countersArgs.Add($@"
                export DOTNET_CLI_HOME=/tmp
                export DOTNET_NOLOGO=true
                export TMPDIR=/proc/{pid}/root/tmp

                # Pipe 'q' after duration to stop session
                timeout --signal=INT {duration} dotnet-counters collect -p {pid} -o {targetPath} --format {format} --counters ""{counters}""

                sleep 2
            ");

            _kube.Run(countersArgs);

            Log.Information($"Downloading to {output} ...");

            var cpArgs = new List<string>();
            if (ns != null) 
            { 
                cpArgs.Add("-n"); cpArgs.Add(ns); 
            }
            cpArgs.Add("cp");
            cpArgs.Add("--retries=1");
            cpArgs.Add($"{pod}:{targetPath}");
            cpArgs.Add(output);
            cpArgs.Add("-c");
            cpArgs.Add(debugContainer);

            _kube.Run(cpArgs);
            Log.Information("SUCCESS.");
        }
        finally
        {
            session.Close();
        }
    }
}