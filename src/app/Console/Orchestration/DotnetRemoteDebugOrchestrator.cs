using System.Text.RegularExpressions;
using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Utilities;
using Spectre.Console;
using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Orchestration;

/// <summary>
/// This class orchestrates the remote debugging of a .NET application inside a Kubernetes pod.
/// </summary>
public class DotnetRemoteDebugOrchestrator
{
    private readonly KubeCliRunner _kube;
    
    public DotnetRemoteDebugOrchestrator(KubeCliRunner kube) => _kube = kube;

    public void Execute(string pod, string pid, string? container, string? ns, string pdbPath, string sourceCodePath)
    {
        var inspector = new PodInspector(_kube);
        DebugSessionManager? debugSession = null;
        DebugSessionManager? symbolSession = null;

        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container) 
                ? inspector.ResolveTargetContainer(pod, ns) 
                : container;
            
            // 1. Understand which UBI we are running
            Log.Information("Detecting target OS...");
            var osArgs = new List<string>();
            if (ns != null) { osArgs.Add("-n"); osArgs.Add(ns); }
            osArgs.Add("exec"); osArgs.Add(pod); osArgs.Add("-c"); osArgs.Add(targetContainer);
            osArgs.Add("--"); osArgs.Add("cat"); osArgs.Add("/etc/redhat-release");

            var osOutput = _kube.Run(osArgs).StdOut.ToLower();
            string ubiVersion = "8"; // Fallback to UBI8
            
            var match = Regex.Match(osOutput, @"release\s+(?<version>\d+)");
            if (match.Success)
            {
                ubiVersion = match.Groups["version"].Value;
            }

            string debugImage = $"dev.criticalmanufacturing.io/platformengineering/sos-ubi:latest"; // TODO: this should have {ubiVersion}
            Log.Information($"Detected OS environment. Using debug image: {debugImage}");

            if (!Directory.Exists(pdbPath)) throw new CliException($"PDB path does not exist: {pdbPath}");
            if (!Directory.Exists(sourceCodePath)) throw new CliException($"Source code path does not exist: {sourceCodePath}");

            // 3. Inject the main .NET debugger container (vsdbg)
            Log.Information("Injecting debug container...");
            debugSession = new DebugSessionManager(_kube);
            var debugContainerName = debugSession.Start(pod, targetContainer, debugImage, ns);

            // 4. Inject the symbol server ephemeral container
            string symbolServerImage = "dev.criticalmanufacturing.io/platformengineering/sos-symbol-server:latest";
            Log.Information($"Injecting symbol server container using image: {symbolServerImage}...");
            symbolSession = new DebugSessionManager(_kube);
            string symbolServerContainerName = symbolSession.Start(pod, targetContainer, symbolServerImage, ns, useImageCommand: true);

            // 5. Copy the PDBs directly into the symbol server container's filesystem
            string containerPdbPath = "/symbols"; // Has to be /tmp to avoid permission issues.
            Log.Information("Uploading PDBs to the symbol server container...");
            CopyPdbsToContainer(pdbPath, pod, symbolServerContainerName, ns, containerPdbPath);

            Log.Information("Verifying symbol server is reachable from the debug container...");
            var curlArgs = new List<string>();
            if (ns != null) { curlArgs.Add("-n"); curlArgs.Add(ns); }
            curlArgs.Add("exec"); curlArgs.Add(pod); curlArgs.Add("-c"); curlArgs.Add(debugContainerName);
            curlArgs.Add("--"); curlArgs.Add("curl"); curlArgs.Add("-s"); curlArgs.Add("http://127.0.0.1:8081/");
            
            try
            {
                var res = _kube.Run(curlArgs);
                Log.Information("Symbol server is successfully running and reachable on port 8081!");
                Log.Information("Symbol server response (truncated): " + res.StdOut);
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not verify symbol server reachability: {ex.Message}");
            }

            // Ephemeral containers in K8s share the target pod's network namespace, 
            // so our PDB server will be accessible at 127.0.0.1:8081 from the vsdbg container.
            // Note: On Linux, vsdbg requires the srv* cache path syntax to process HTTP symbol servers.
            string pdbServerUrl = "http://localhost:8081";

            // 6. Generate the launch.json locally
            string remoteSourcePath = "/__w/1/s"; // TODO: Expose this via CLI options in RemoteDebugCommand
            Log.Information("Generating VS Code launch.json from embedded template...");
            CreateLaunchJson(sourceCodePath, pod, ns, debugContainerName, pid, pdbServerUrl, remoteSourcePath);

            Log.Information("\n========================================================");
            Log.Information("DEBUGGER IS READY!");
            Log.Information($"1. Open VS Code in your product source code folder: {sourceCodePath}");
            Log.Information("2. Go to the 'Run and Debug' view (Ctrl+Shift+D).");
            Log.Information("3. Select 'Attach to Kubernetes' and press F5.");
            Log.Information("========================================================\n");
            
            Log.Information("Press [Enter] to stop debugging and close the sessions...");
            Console.ReadLine();
        }
        finally
        {
            debugSession?.Close();
            symbolSession?.Close();
        }
    }

    private void CopyPdbsToContainer(string localPdbPath, string pod, string container, string? ns, string targetPath)
    {
        var args = new List<string>();
        if (!string.IsNullOrEmpty(ns)) 
        { 
            args.Add("-n"); args.Add(ns); 
        }

        // Ensure we copy the contents of the directory, not the directory itself.
        // This prevents creating nested directories (like /pdbs/pdbs/) which often causes remote permission issues.
        string sourcePath = localPdbPath.TrimEnd('/', '\\') + "/.";

        args.Add("cp");
        args.Add("--retries=1");
        args.Add(sourcePath);
        args.Add($"{pod}:{targetPath}");
        args.Add("-c");
        args.Add(container);

        _kube.Run(args);
    }

    private void CreateLaunchJson(string sourceCodePath, string pod, string? ns, string debugContainerName, string pid, string pdbServerUrl, string remoteSourcePath)
    {
        var assembly = typeof(DotnetRemoteDebugOrchestrator).Assembly;
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("launch.json"));

        if (resourceName == null)
        {
            throw new CliException("Could not find the launch.json template embedded inside the binary bundle.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        string templateContent = reader.ReadToEnd();

        string finalJson = templateContent
            .Replace("\"${PID}\"", pid) // Strips the quotes so VS Code receives a raw integer (e.g. 27 instead of "27")
            .Replace("${PID}", pid)
            .Replace("${POD}", pod)
            .Replace("${NAMESPACE}", ns ?? "default")
            .Replace("${DEBUG_CONTAINER}", debugContainerName)
            .Replace("${SYMBOL_SERVER_URL}", pdbServerUrl)
            .Replace("${REMOTE_SOURCE_PATH}", remoteSourcePath)
            .Replace("${SOURCE_CODE}", sourceCodePath);

        string vscodeDir = Path.Combine(sourceCodePath, ".vscode");
        Directory.CreateDirectory(vscodeDir);
        string launchJsonPath = Path.Combine(vscodeDir, "launch.json");

        File.WriteAllText(launchJsonPath, finalJson);
        Log.Information($"Successfully created {launchJsonPath}");
    }
}