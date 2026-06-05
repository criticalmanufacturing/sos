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
    /// 3. It will start a troubleshooting session (troubleshooting container) attached to the target container.
    /// 4. It will execute the dotnet-dump command inside the troubleshooting container, targeting the PID and collecting the output in the troubleshooting container's filesystem.
    /// 5. It will copy the output file from the troubleshooting container to the local machine.
    /// 6. It will handle cleanup of the troubleshooting session and provide informative logging throughout the process.
    /// </summary>
    public void Execute(string pod, string output, string pid, string? container, string ns, string image, int sessionDuration = 20)
    {
        var inspector = new PodInspector(_kube);
        var session = new TroubleshootingSessionManager(_kube);

        // Enforce correct output path and extension for .NET (.dmp)
        output = OutputChecker.ResolveOutputPath(output, pod, ".dmp");

        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container) 
                ? inspector.ResolveTargetContainer(pod, ns) 
                : container;
            
            // Start Session
            var troubleshootingContainer = session.Start(pod, targetContainer, image, ns, sessionDuration);

            // Find PID
            Log.Information($"Target PID: {pid}");

            // Collect Dump
            // targetPath: where the APP writes it (Target Container FS)
            // troubleshootingStagingPath: where we move it so kubectl can see it (Troubleshooting Container FS)
            string targetPath = "/tmp/output.dmp"; 
            string troubleshootingStagingPath = "/tmp/final_dump.dmp";
            
            Log.Information("Collecting dump...");
            
            var dumpArgs = new List<string>();

            dumpArgs.Add("-n"); 
            dumpArgs.Add(ns); 
            
            dumpArgs.Add("exec"); 
            dumpArgs.Add(pod); 
            dumpArgs.Add("-c"); 
            dumpArgs.Add(troubleshootingContainer);
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

                PRODUCTIVE_DUMP=/proc/{pid}/root{targetPath}
                TROUBLESHOOTING_STAGING={troubleshootingStagingPath}

                dotnet-dump collect -p {pid} -o ""$PRODUCTIVE_DUMP""

                cp ""$PRODUCTIVE_DUMP"" ""$TROUBLESHOOTING_STAGING""
            ");

            _kube.Run(dumpArgs);

            // Since the dump file is now on the troubleshooting container filesystem, we can delete it from productive pod
            var productivePodFileManager = new ProductivePodFileManager(_kube);
            productivePodFileManager.DeleteFileFromProductivePod(pod, ns, targetPath);

            // Download from the troubleshooting container's local filesystem
            KubeFileTransfer.Download(_kube, pod, ns, troubleshootingContainer, troubleshootingStagingPath, output);
            Log.Information("SUCCESS.");
        }
        finally
        {
            session.Close();
        }
    }
}