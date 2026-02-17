using System.Text.RegularExpressions;
using Cmf.CLI.Core;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public class DebugSessionManager
{
    private readonly KubeCliRunner _kube;
    private string? _debugContainerName;
    private string? _pod;
    private string? _ns;
    private string? _context;

    public DebugSessionManager(KubeCliRunner kube) => _kube = kube;

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

    private string? ExtractContainerName(string text)
    {
        // Matches "Defaulting debug container name to debugger-abcde"
        var match = Regex.Match(text, @"name to (?<name>[a-z0-9\-]+)");
        return match.Success ? match.Groups["name"].Value : null;
    }

    private void WaitForReady(string pod, string container, string? ns)
    {
        // Simple "Door Kicker": Try to echo, if it fails, sleep 1s and try again.
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
            catch { Thread.Sleep(1000); }
        }
    }

    public void Close()
    {
        if (_debugContainerName == null || _pod == null) return;
        try
        {
            var args = new List<string>();
            if (_ns != null) { args.Add("-n"); args.Add(_ns); }
            if (_context != null) { args.Add("--context"); args.Add(_context); }
            args.Add("exec"); args.Add(_pod); args.Add("-c"); args.Add(_debugContainerName);
            args.Add("--"); args.Add("kill"); args.Add("1"); // Kill sleep
            _kube.RunAllowFailure(args);
        }
        catch { /* Ignore */ }
    }
}