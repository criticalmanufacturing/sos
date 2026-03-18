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
    /// 3. It will start a debug session (debug container) attached to the target container.
    /// 4. It will execute a Node.js script inside the debug container that triggers the V8 heap snapshot via the Inspector Protocol, targeting the PID and collecting the output in the debug container's filesystem.
    /// 5. It will copy the output file from the debug container to the local machine.
    /// 6. It will handle cleanup of the debug session and provide informative logging throughout the process.
    /// </summary>
    public void Execute(string pod, string output, string pid, string? container, string? ns, string image)
    {
        var inspector = new PodInspector(_kube);
        var session = new DebugSessionManager(_kube);

        // Enforce correct output path and extension for NodeJS (.heapdump)
        output = OutputChecker.ResolveOutputPath(output, pod, ".heapdump");

        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container) 
                ? inspector.ResolveTargetContainer(pod, ns) 
                : container;
            
            var debugContainer = session.Start(pod, targetContainer, image, ns);

            Log.Information($"Target Node.js PID: {pid}");

            string debuggerStagingPath = "/tmp/node_dump.heapsnapshot";
            string containerScriptPath = "/tmp/extract.js";
            
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "NodeHeapExtractor.js");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Could not find the Node extraction script at {scriptPath}");
            }

            // STEP 1: Push the JS script into the debug container
            Log.Information("Pushing extraction script to debug container...");
            var pushArgs = new List<string>();
            if (ns != null) 
            { 
                pushArgs.Add("-n"); pushArgs.Add(ns); 
            }
            pushArgs.Add("cp");
            pushArgs.Add("--retries=-1");
            pushArgs.Add(scriptPath);
            pushArgs.Add($"{pod}:{containerScriptPath}");
            pushArgs.Add("-c"); 
            pushArgs.Add(debugContainer);

            _kube.Run(pushArgs);

            // STEP 2: Execute the script
            Log.Information("Triggering V8 Heap Snapshot via Inspector Protocol...");
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
            
            // Much safer single-line execution
            dumpArgs.Add($"kill -USR1 {pid} && sleep 2 && export DUMP_PATH={debuggerStagingPath} && node --experimental-websocket {containerScriptPath}");

            _kube.Run(dumpArgs);

            // STEP 3: Pull the dump back to the local environment
            Log.Information($"Downloading to {output} ...");
            var cpArgs = new List<string>();
            if (ns != null) { cpArgs.Add("-n"); cpArgs.Add(ns); }
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