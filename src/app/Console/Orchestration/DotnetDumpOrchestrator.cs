using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Utilities;

namespace Cmf.Cli.Plugin.Sos.Orchestration;

/// <summary>
/// This class will orchestrate all dump operations
/// </summary>
public class DotnetDumpOrchestrator
{
    private readonly KubeCliRunner _kube;
    public DotnetDumpOrchestrator(KubeCliRunner kube) => _kube = kube;

    /// <summary>
    /// This function will orchestrate the entire flow of dotnet-dump collection:
    /// 1. It will ensure the output path is valid and has the correct extension.
    /// 2. It will resolve the target container if not provided.
    /// 3. It will start a debug session (debug container) attached to the target container.
    /// 4. It will execute the dotnet-dump command inside the debug container, targeting the PID and collecting the output in the debug container's filesystem.
    /// 5. It will copy the output file from the debug container to the local machine.
    /// 6. It will handle cleanup of the debug session and provide informative logging throughout the process.
    /// </summary>
    public void Execute(string pod, string output, string pid, string? container, string? ns, string image)
    {
        var inspector = new PodInspector(_kube);
        var session = new DebugSessionManager(_kube);

        // Enforce correct output path and extension for .NET (.dmp)
        output = OutputChecker.ResolveOutputPath(output, pod, ".dmp");

        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container) 
                ? inspector.ResolveTargetContainer(pod, ns) 
                : container;
            
            // Start Session (Exact command you provided)
            var debugContainer = session.Start(pod, targetContainer, image, ns);

            // Find PID
            Log.Information($"Target PID: {pid}");

            // Collect Dump
            // targetPath: where the APP writes it (Target Container FS)
            // debuggerStagingPath: where we move it so kubectl can see it (Debugger Container FS)
            string targetPath = "/tmp/output.dmp"; 
            string debuggerStagingPath = "/tmp/final_dump.dmp";
            
            Log.Information("Collecting dump...");
            
            var dumpArgs = new List<string>();
            if (ns != null) 
            { 
                dumpArgs.Add("-n"); dumpArgs.Add(ns); 
            }
            dumpArgs.Add("exec"); 
            dumpArgs.Add(pod); 
            dumpArgs.Add("-c"); 
            dumpArgs.Add(debugContainer);
            dumpArgs.Add("--"); 
            dumpArgs.Add("sh"); 
            dumpArgs.Add("-c");
            
            // Important notes:
            // Set environment variables
            // Run dotnet-dump collect
            // PHYSICALLY copy from /proc/{pid}/root/tmp to the debugger's /tmp
            dumpArgs.Add($@"
                export DOTNET_CLI_HOME=/tmp
                export DOTNET_NOLOGO=true
                export TMPDIR=/proc/{pid}/root/tmp
                
                dotnet-dump collect -p {pid} -o {targetPath} && \
                cp /proc/{pid}/root{targetPath} {debuggerStagingPath} && \
                rm /proc/{pid}/root{targetPath}
            ");

            _kube.Run(dumpArgs);

            // Download from the debugger's local filesystem
            Log.Information($"Downloading to {output} ...");
            
            var cpArgs = new List<string>();
            if (ns != null) 
            { 
                cpArgs.Add("-n"); cpArgs.Add(ns); 
            }
            cpArgs.Add("cp");
            cpArgs.Add("--retries=1");
            cpArgs.Add($"{pod}:{debuggerStagingPath}");
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