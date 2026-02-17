using Cmf.CLI.Core;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public class DumpOrchestrator
{
    private readonly KubeCliRunner _kube;
    public DumpOrchestrator(KubeCliRunner kube) => _kube = kube;

    public void Execute(string pod, string output, string? container, string? ns,string image)
    {
        var inspector = new PodInspector(_kube);
        var session = new DebugSessionManager(_kube);
        var finder = new ProcessFinder(_kube);

        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container) 
                ? inspector.ResolveTargetContainer(pod, ns) 
                : container;
            
            // 1. Start Session (Exact command you provided)
            var debugContainer = session.Start(pod, targetContainer, image, ns);

            // 2. Find PID
            var pid = finder.FindDotnetPid(pod, debugContainer, ns);
            Log.Information($"Target PID: {pid}");

            // 3. Collect Dump
            // targetPath: where the APP writes it (Target Container FS)
            // debuggerStagingPath: where we move it so kubectl can see it (Debugger Container FS)
            string targetPath = "/tmp/output.dmp"; 
            string debuggerStagingPath = "/tmp/final_dump.dmp";
            
            Log.Information("Collecting dump...");
            
            var dumpArgs = new List<string>();
            if (ns != null) { dumpArgs.Add("-n"); dumpArgs.Add(ns); }
            dumpArgs.Add("exec"); dumpArgs.Add(pod); dumpArgs.Add("-c"); dumpArgs.Add(debugContainer);
            dumpArgs.Add("--"); dumpArgs.Add("sh"); dumpArgs.Add("-c");
            
            // --- THE FIX ---
            // 1. Set environment variables
            // 2. Run dotnet-dump collect
            // 3. PHYSICALLY copy from /proc/{pid}/root/tmp to the debugger's /tmp
            dumpArgs.Add($@"
                export DOTNET_CLI_HOME=/tmp
                export DOTNET_NOLOGO=true
                export TMPDIR=/proc/{pid}/root/tmp
                
                dotnet-dump collect -p {pid} -o {targetPath} && \
                cp /proc/{pid}/root{targetPath} {debuggerStagingPath} && \
                rm /proc/{pid}/root{targetPath}
            ");

            _kube.Run(dumpArgs);

            // 4. Download from the debugger's local filesystem
            Log.Information($"Downloading to {output}...");
            
            var cpArgs = new List<string>();
            if (ns != null) { cpArgs.Add("-n"); cpArgs.Add(ns); }
            cpArgs.Add("cp");
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