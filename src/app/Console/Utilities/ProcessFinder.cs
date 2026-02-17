namespace Cmf.Cli.Plugin.Sos.Utilities;

public class ProcessFinder
{
    private readonly KubeCliRunner _kube;
    public ProcessFinder(KubeCliRunner kube) => _kube = kube;

    public string FindDotnetPid(string pod, string debugContainer, string? ns)
    {
        var args = new List<string>();
        if (ns != null) { args.Add("-n"); args.Add(ns); }
        
        args.Add("exec"); args.Add(pod); args.Add("-c"); args.Add(debugContainer);
        args.Add("--"); args.Add("ps"); args.Add("aux");

        var res = _kube.Run(args);
        var lines = res.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines.Skip(1)) 
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 11) continue;

            var pid = parts[1];
            var tty = parts[6];
            
            // Index 10 is the command executable (e.g., "dotnet", "/bin/sh", "/usr/bin/dotnet")
            var executable = parts[10]; 
            var fullCmd = string.Join(" ", parts.Skip(10));

            // CRITICAL FIX: 
            // 1. TTY must be '?'
            // 2. The executable ITSELF must be 'dotnet' (or /path/to/dotnet)
            // 3. It must not be our own tools (dotnet-dump, sos)
            bool isDotnetExe = executable == "dotnet" || executable.EndsWith("/dotnet");

            if (tty == "?" && isDotnetExe && !fullCmd.Contains("dotnet-dump") && !fullCmd.Contains("sos"))
            {
                return pid;
            }
        }

        throw new InvalidOperationException("Could not find a running .NET process (TTY=?, Executable=dotnet).");
    }
}