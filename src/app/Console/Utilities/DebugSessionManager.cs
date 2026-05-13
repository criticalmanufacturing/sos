using System.Text.RegularExpressions;
using Cmf.CLI.Core;
using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public class DebugSessionManager
{
    private readonly KubeCliRunner _kube;
    private string? _debugContainerName;
    private string? _pod;
    private string? _ns;

    public DebugSessionManager(KubeCliRunner kube) => _kube = kube;

    /// <summary>
    /// This function starts a debug session by creating a new debug container attached to the specified pod and container.
    /// It uses a Sentinel File polling loop to guarantee safe self-termination (TTL 3600s) even if the client crashes.
    /// </summary>
    public string Start(string pod, string targetContainer, string image, string? ns)
    {
        var args = new List<string>();
        if (!string.IsNullOrEmpty(ns)) { args.Add("-n"); args.Add(ns); }

        args.Add("debug");
        args.Add(pod);
        args.Add($"--image={image}");
        args.Add("--share-processes"); 
        args.Add($"--target={targetContainer}"); 
        args.Add("--attach=false"); 
        
        args.Add("--");
        args.Add("sh"); 
        args.Add("-c"); 
        // Loop 1200 times (20 mins). If the file exists, exit. Otherwise sleep 1s and check again.
        args.Add("for i in $(seq 1 1200); do if [ -f /tmp/debug-done ]; then exit 0; fi; sleep 1; done");

        Log.Information($"Execution command: kubectl {string.Join(' ', args)}");

        _pod = pod;
        _ns = ns;

        Log.Information($"Injecting debugger (detached)...");
        
        // This returns immediately with "Defaulting debug container name to debugger-xxxxx"
        var res = _kube.Run(args); 

        // We MUST parse the name because we didn't force one
        _debugContainerName = ExtractContainerName(res.StdOut) ?? ExtractContainerName(res.StdErr);

        if (string.IsNullOrWhiteSpace(_debugContainerName))
            throw new InvalidOperationException($"Could not determine debug container name. Output: {res.StdErr}");

        Log.Information($"Attached to: {_debugContainerName}");
        
        // Verify that the container is ready
        WaitForReady(pod, _debugContainerName, ns);

        return _debugContainerName;
    }

    /// <summary>
    /// This function extracts the debug container name from the output of the kubectl debug command.
    /// </summary>
    public string? ExtractContainerName(string text)
    {
        // Matches "Defaulting debug container name to debugger-abcde"
        var match = Regex.Match(text, @"name to (?<name>[a-z0-9\-]+)");
        return match.Success ? match.Groups["name"].Value : null;
    }

    /// <summary>
    /// This function checks if the debug container is ready by attempting to execute a simple command inside it.
    /// If the command fails, it waits for a short period and retries (currently set to 10 attemps with 1 second delay).
    /// </summary>
    private void WaitForReady(string pod, string container, string? ns)
    {
        // Simple "Door Kicker": Try to echo, if it fails, sleep 5s and try again.
        for(int i=0; i<10; i++) 
        {
            try 
            {
                var args = new List<string>();
                if (ns != null) 
                { 
                    args.Add("-n"); 
                    args.Add(ns); }
                    args.Add("exec"); 
                    args.Add(pod); 
                    args.Add("-c"); 
                    args.Add(container); 
                    args.Add("--"); 
                    args.Add("true");
                    _kube.Run(args);
                    return; // Ready!
            }
            catch 
            { 
                Thread.Sleep(5000); 
            }
        }
    }

    /// <summary>
    /// This function closes the debug session by dropping the sentinel file into the container,
    /// triggering the polling loop to exit gracefully.
    /// </summary>
    public void Close()
    {
        if (_debugContainerName == null || _pod == null) return;
        try
        {
            var args = new List<string>();
            if (_ns != null) { args.Add("-n"); args.Add(_ns); }
            args.Add("exec"); 
            args.Add(_pod); 
            args.Add("-c"); 
            args.Add(_debugContainerName);
            args.Add("--"); 
            args.Add("sh"); 
            args.Add("-c"); 
            args.Add("touch /tmp/debug-done"); 
            
            _kube.RunAllowFailure(args);
            Log.Information("Sent cleanup signal to debug container.");

            EnsureEphemeralPodWasTerminated();
        }
        catch (Exception ex) { 
            throw new CliException("Failed to send cleanup signal to debug container. It will self-terminate when its TTL expires. " + ex.Message);
         }
    }

    /// <summary>
    /// Verifies that the ephemeral debug container has successfully transitioned to a Terminated state.
    /// Uses JSONPath to directly query the container's status without relying on text parsing.
    /// </summary>
    public void EnsureEphemeralPodWasTerminated()
    {
        if (_debugContainerName == null || _pod == null) return;

        try
        {
            Log.Information("Verifying debug container termination...");
            
            // Give the Kubelet a brief moment to process the exit command and update the API state
            Thread.Sleep(2000);

            var args = new List<string>();
            if (_ns != null) 
            {
                args.Add("-n"); 
                args.Add(_ns); 
            }
            args.Add("get");
            args.Add("pod");
            args.Add(_pod);
            
            // Natively extract just the state of our specific ephemeral container
            args.Add("-o");
            args.Add($"jsonpath={{.status.ephemeralContainerStatuses[?(@.name==\"{_debugContainerName}\")].state}}");

            var res = _kube.Run(args);

            // Output will look like {"terminated":{...}} if dead, or {"running":{...}} if alive
            if (res.StdOut.Contains("terminated", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Cleanup Verified: Debug container successfully terminated.");
            }
            else
            {
                Log.Warning($"Cleanup verification failed. Container may still be running. Current state: {res.StdOut}");
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not definitively verify container termination. {ex.Message}");
        }
    }
}