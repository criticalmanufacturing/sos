using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Utilities;

namespace Cmf.Cli.Plugin.Sos.Orchestration;

/// <summary>
/// This class will orchestrate all dump operations
/// </summary>
public class NodeJsDumpOrchestrator
{
    private readonly KubeCliRunner _kube;
    public NodeJsDumpOrchestrator(KubeCliRunner kube) => _kube = kube;

    /// <summary>
    /// This function will orchestrate the entire flow of Node.js heap dump collection:
    /// 1. It will ensure the output path is valid and has the correct extension.
    /// 2. It will resolve the target container if not provided.
    /// 3. It will start a troubleshooting session (troubleshooting container) attached to the target container.
    /// 4. It will execute a Node.js script inside the troubleshooting container that triggers the V8 heap snapshot via the Inspector Protocol, targeting the PID and collecting the output in the troubleshooting container's filesystem.
    /// 5. It will copy the output file from the troubleshooting container to the local machine.
    /// 6. It will handle cleanup of the troubleshooting session and provide informative logging throughout the process.
    /// </summary>
    public void Execute(string pod, string output, string pid, string? container, string ns, string image, int sessionDuration = 20)
    {
        var inspector = new PodInspector(_kube);
        var session = new TroubleshootingSessionManager(_kube);

        // Enforce correct output path and extension for NodeJS (.heapdump)
        output = OutputChecker.ResolveOutputPath(output, pod, ".heapdump");

        string? scriptPath = null;
        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container) 
                ? inspector.ResolveTargetContainer(pod, ns) 
                : container;
            
            var troubleshootingContainer = session.Start(pod, targetContainer, image, ns, sessionDuration);

            Log.Information($"Target Node.js PID: {pid}");

            string troubleshootingStagingPath = "/tmp/node_dump.heapsnapshot";
            string containerScriptPath = "/tmp/extract.js";
            
            var assembly = typeof(NodeJsDumpOrchestrator).Assembly;
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("NodeHeapExtractor.js"));

            if (resourceName == null)
            {
                throw new FileNotFoundException("Could not find the Node extraction script embedded inside the binary bundle.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream!);
            
            scriptPath = Path.GetTempFileName();
            File.WriteAllText(scriptPath, reader.ReadToEnd());

            // STEP 1: Push the JS script into the troubleshooting container
            Log.Information("Pushing extraction script to troubleshooting container...");
            KubeFileTransfer.Upload(_kube, pod, ns, troubleshootingContainer, scriptPath, containerScriptPath);

            // STEP 2: Execute the script
            Log.Information("Triggering V8 Heap Snapshot via Inspector Protocol...");
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
            
            // Much safer single-line execution
            dumpArgs.Add($"kill -USR1 {pid} && sleep 2 && export DUMP_PATH={troubleshootingStagingPath} && node --experimental-websocket {containerScriptPath}");

            _kube.Run(dumpArgs);

            // STEP 3: Pull the dump back to the local environment
            KubeFileTransfer.Download(_kube, pod, ns, troubleshootingContainer, troubleshootingStagingPath, output);
            
            Log.Information("SUCCESS.");
        }
        finally
        {
            session.Close();
        }
    }
}