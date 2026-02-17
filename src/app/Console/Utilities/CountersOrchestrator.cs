using Cmf.CLI.Core;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public class CountersOrchestrator
{
    private readonly KubeCliRunner _kube;
    public CountersOrchestrator(KubeCliRunner kube) => _kube = kube;

    public void Execute(string pod, string output, string? container, string? ns, string image, string format, int duration, string counters)
    {
        var inspector = new PodInspector(_kube);
        var session = new DebugSessionManager(_kube);
        var finder = new ProcessFinder(_kube);

        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(container)
                ? inspector.ResolveTargetContainer(pod, ns)
                : container;

            var debugContainer = session.Start(pod, targetContainer, image, ns);

            var pid = finder.FindDotnetPid(pod, debugContainer, ns);
            Log.Information($"Target PID: {pid}");

            string targetPath = $"/tmp/output.{format.ToLower()}";

            Log.Information($"Collecting dotnet-counters for {duration} seconds...");

            var countersArgs = new List<string>();
            if (ns != null) { countersArgs.Add("-n"); countersArgs.Add(ns); }
            countersArgs.Add("exec"); countersArgs.Add(pod); countersArgs.Add("-c"); countersArgs.Add(debugContainer);
            countersArgs.Add("--"); countersArgs.Add("sh"); countersArgs.Add("-c");

            countersArgs.Add($@"
                export DOTNET_CLI_HOME=/tmp
                export DOTNET_NOLOGO=true
                export TMPDIR=/proc/{pid}/root/tmp

                # Pipe 'q' after duration to stop session
                timeout --signal=INT {duration} dotnet-counters collect -p {pid} -o {targetPath} --format {format} --counters ""{counters}""

                sleep 2
            ");

            _kube.Run(countersArgs);

            Log.Information($"Downloading to {output}...");

            var cpArgs = new List<string>();
            if (ns != null) { cpArgs.Add("-n"); cpArgs.Add(ns); }
            cpArgs.Add("cp");
            cpArgs.Add($"{pod}:{targetPath}");
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