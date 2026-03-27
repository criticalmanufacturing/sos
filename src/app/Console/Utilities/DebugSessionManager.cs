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
    /// It constructs the appropriate kubectl command with the provided parameters and executes it.
    /// The function captures the output to determine the name of the debug container created by Kubernetes, which is essential for subsequent operations.
    /// </summary>
    public string Start(string pod, string targetContainer, string image, string? ns)
    {

        var args = new List<string>();
        if (!string.IsNullOrEmpty(ns)) { args.Add("-n"); args.Add(ns); }

        args.Add("debug");
        args.Add(pod);
        args.Add($"--image={image}");
        args.Add("--share-processes"); // As requested
        args.Add($"--target={targetContainer}"); // As requested
        args.Add("--attach=false"); // Essential for automation (prevents hanging)
        
        args.Add("--");
        args.Add("sleep"); 
        args.Add("3600"); // Keep it alive so we can exec into it

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
        
        // Quick verify it's up before proceeding (1 retry)
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
                if (ns != null) { args.Add("-n"); args.Add(ns); }
                args.Add("exec"); args.Add(pod); args.Add("-c"); args.Add(container); 
                args.Add("--"); args.Add("true");
                _kube.Run(args);
                return; // Ready!
            }
            catch { Thread.Sleep(5000); }
        }
    }

    /// <summary>
    /// This function closes the debug session by killing the debug container.
    /// If it fails it will throw an exception with instructions for manual cleanup.
    /// </summary>
    public void Close()
    {
        if (_debugContainerName == null || _pod == null) return;
        try
        {
            var args = new List<string>();
            if (_ns != null) { args.Add("-n"); args.Add(_ns); }
            args.Add("exec"); args.Add(_pod); args.Add("-c"); args.Add(_debugContainerName);
            args.Add("--"); args.Add("pkill"); args.Add("sleep"); // Kill sleep
            _kube.RunAllowFailure(args);
        }
        catch (Exception ex) { 
            throw new CliException("Failed to clean up debug container. Please check manually and remove the 'debugger-*' container if it exists. " + ex.Message);
         }
    }
}