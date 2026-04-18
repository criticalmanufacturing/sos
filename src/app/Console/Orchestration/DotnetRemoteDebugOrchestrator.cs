using System.Text.RegularExpressions;
using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Utilities;
using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Orchestration;

/// <summary>
/// This class orchestrates the remote debugging of a .NET application inside a Kubernetes pod.
/// </summary>
public class DotnetRemoteDebugOrchestrator
{
    private readonly KubeCliRunner _kube;
    private static readonly string symbolServerUrl = "https://symbolserver.apps.rhos.cm-mes.dev";
    
    public DotnetRemoteDebugOrchestrator(KubeCliRunner kube) => _kube = kube;

    public void Execute(string pod, string? container, string? ns, string sourceCodePath)
    {
        var inspector = new PodInspector(_kube);
        DebugSessionManager? debugSession = null;

        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container) 
                ? inspector.ResolveTargetContainer(pod, ns) 
                : container;
            
            // 1. Understand which UBI we are running
            var osOutput = GatherTargetPodKernelInfo(pod, targetContainer, ns);
            string ubiVersion = "8"; // Fallback to UBI8
            
            var match = Regex.Match(osOutput, @"release\s+(?<version>\d+)");
            if (match.Success)
            {
                ubiVersion = match.Groups["version"].Value;
            }

            string debugImage = $"dev.criticalmanufacturing.io/platformengineering/sos-ubi:latest"; // TODO: this should have ubiVersion (e.g sos-ubi8)
            Log.Information($"Detected OS environment. Using debug image: {debugImage}");

            if (!Directory.Exists(sourceCodePath)) throw new CliException($"Source code path does not exist: {sourceCodePath}");

            // 2. Extract App version to map the correct symbol server
            Log.Information("Extracting application version from pod labels...");
            var describeArgs = new List<string>();
            if (ns != null) { describeArgs.Add("-n"); describeArgs.Add(ns); }
            describeArgs.Add("describe"); describeArgs.Add("pod"); describeArgs.Add(pod);

            var describeOutput = _kube.Run(describeArgs).StdOut;
            var versionMatch = Regex.Match(describeOutput, @"app\.kubernetes\.io/version=(?<version>[^\s]+)");
            string appVersion = versionMatch.Success ? versionMatch.Groups["version"].Value : "latest";
            
            string pdbServerUrl = $"{symbolServerUrl}/{appVersion}";
            Log.Information($"Detected app version: {appVersion}");
            Log.Information($"Using symbol server URL: {pdbServerUrl}");

            // 3. Inject the main .NET debugger container (vsdbg)
            Log.Information("Injecting debug container...");
            debugSession = new DebugSessionManager(_kube);
            var debugContainerName = debugSession.Start(pod, targetContainer, debugImage, ns);

            // 4. Generate the launch.json locally
            string remoteSourcePath = "/__w/1/s"; // TODO: Expose this via CLI options in RemoteDebugCommand OR handle this deterministically
            Log.Information("Generating VS Code launch.json from embedded template...");
            CreateLaunchJson(sourceCodePath, pod, ns, debugContainerName, pdbServerUrl, remoteSourcePath);

            Log.Information("\n========================================================");
            Log.Information("DEBUGGER IS READY!");
            Log.Information($"1. Open VS Code in your product source code folder: {sourceCodePath}");
            Log.Information("2. Go to the 'Run and Debug' view (Ctrl+Shift+D).");
            Log.Information("3. Select 'Attach to Kubernetes' and press F5.");
            Log.Information("Important information: Your source code has to match the version you're currently debugging.");
            Log.Information("========================================================\n");
            
            Log.Information("Press [Enter] to stop debugging and close the sessions...");
            Console.ReadLine();
        }
        finally
        {
            debugSession?.Close();
        }
    }

    private string GatherTargetPodKernelInfo(string pod, string targetContainer, string? ns)
    {
        Log.Information("Detecting target OS...");
        var osArgs = new List<string>();
        if (ns != null) 
        { 
            osArgs.Add("-n"); osArgs.Add(ns); 
        }

        osArgs.Add("exec");
        osArgs.Add(pod); 
        osArgs.Add("-c"); 
        osArgs.Add(targetContainer);
        osArgs.Add("--"); 
        osArgs.Add("cat"); 
        osArgs.Add("/etc/redhat-release");

        return _kube.Run(osArgs).StdOut.ToLower();
    }

    private void CreateLaunchJson(string sourceCodePath, string pod, string? ns, string debugContainerName, string pdbServerUrl, string remoteSourcePath)
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