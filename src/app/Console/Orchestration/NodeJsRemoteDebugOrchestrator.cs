using System.Diagnostics;
using Cmf.CLI.Core;
using Cmf.Cli.Plugin.Sos.Utilities;

namespace Cmf.Cli.Plugin.Sos.Orchestration;

/// <summary>
/// This class orchestrates the remote debugging of a Node.js application inside a Kubernetes pod.
/// </summary>
public class NodeJsRemoteDebugOrchestrator
{
    private readonly KubeCliRunner _kube;
    
    public NodeJsRemoteDebugOrchestrator(KubeCliRunner kube) => _kube = kube;

    /// <summary>
    /// Orchestrates the Node.js remote debug flow:
    /// 1. Injects a debug container.
    /// 2. Sends the USR1 signal to the target Node process to enable the V8 inspector.
    /// 3. Sets up local port-forwarding to the Node inspector port.
    /// 4. Fetches the active debug session from the Node instance.
    /// 5. Presents the direct Chrome DevTools URL to the user.
    /// </summary>
    public void Execute(string pod, string pid, string? container, string? ns, string image)
    {
        var inspector = new PodInspector(_kube);
        var session = new DebugSessionManager(_kube);

        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container) 
                ? inspector.ResolveTargetContainer(pod, ns) 
                : container;
            
            var debugContainer = session.Start(pod, targetContainer, image, ns);

            Log.Information($"Target Node.js PID: {pid}");
            Log.Information("Sending USR1 signal to enable Node.js inspector...");
            
            var signalArgs = new List<string>();
            if (ns != null) 
            { 
                signalArgs.Add("-n"); signalArgs.Add(ns); 
            }
            signalArgs.Add("exec"); signalArgs.Add(pod); signalArgs.Add("-c"); signalArgs.Add(debugContainer);
            signalArgs.Add("--"); signalArgs.Add("sh"); signalArgs.Add("-c");
            signalArgs.Add($"kill -USR1 {pid}");

            _kube.Run(signalArgs);

            // Find an available local port to prevent clashing if multiple debug sessions run
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            int localPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            Log.Information($"Starting port-forwarding on local port {localPort} to pod port 9229...");
            
            var pfArgs = new List<string>();
            if (ns != null) { pfArgs.Add("-n"); pfArgs.Add(ns); }
            pfArgs.Add("port-forward"); pfArgs.Add(pod);
            pfArgs.Add("--address"); pfArgs.Add("0.0.0.0");
            pfArgs.Add($"{localPort}:9229");

            var psi = new ProcessStartInfo
            {
                FileName = "kubectl",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var a in pfArgs) psi.ArgumentList.Add(a);

            using var pfProcess = new Process { StartInfo = psi };
            
            // IMPORTANT: We must consume the output streams. If we don't, the OS buffer fills up 
            // (kubectl logs every connection), which freezes the port-forwarding and drops the Chrome DevTools connection!
            pfProcess.OutputDataReceived += (_, _) => { };
            pfProcess.ErrorDataReceived += (_, _) => { };
            
            pfProcess.Start();
            pfProcess.BeginOutputReadLine();
            pfProcess.BeginErrorReadLine();

            // Allow some time for port-forwarding to establish
            Thread.Sleep(3000);

            if (pfProcess.HasExited)
            {
                throw new Exception($"Port forwarding failed to start. Error: {pfProcess.StandardError.ReadToEnd()}");
            }

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                bool isReady = false;
                
                // Retry getting the debugger targets a few times because inspector might take time to start listening
                for(int i = 0; i < 5; i++)
                {
                    try
                    {
                        var response = httpClient.GetAsync($"http://127.0.0.1:{localPort}/json/list").Result;
                        if (response.IsSuccessStatusCode)
                        {
                            isReady = true;
                            break;
                        }
                    }
                    catch { /* Ignore and retry */ }
                    Thread.Sleep(1500);
                }

                Log.Information("\n========================================================");
                if (isReady)
                {
                    Log.Information("DEBUGGER IS READY!");
                }
                else
                {
                    Log.Warning("Debugger might still be starting, but port-forwarding is active.");
                }

                Log.Information("\n[HOW TO CONNECT]");
                Log.Information("1. Open Google Chrome or Microsoft Edge and navigate to: chrome://inspect/#devices");
                Log.Information($"2. Click 'Configure...' and ensure 'localhost:{localPort}' is in the list.");
                Log.Information("3. Wait a few seconds until the debugger is ready, then click 'inspect' under the discovered Remote Target.");
                Log.Information("========================================================\n");
                
                Log.Information("Press [Enter] to stop debugging and close the session...");
                Console.ReadLine();
            }
            finally
            {
                if (!pfProcess.HasExited)
                {
                    pfProcess.Kill();
                }
            }
        }
        finally
        {
            session.Close();
        }
    }
}