using System.Text.Json;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public class PodInspector
{
    private readonly KubeCliRunner _kube;
    public PodInspector(KubeCliRunner kube) => _kube = kube;

    public string ResolveTargetContainer(string pod, string? ns)
    {
        var args = new List<string> { "get", "pod", pod, "-o", "json" };
        if (ns != null) { args.Add("-n"); args.Add(ns); }

        var res = _kube.Run(args);
        using var doc = JsonDocument.Parse(res.StdOut);
        
        var containers = doc.RootElement.GetProperty("spec").GetProperty("containers");
        var candidates = new List<string>();

        foreach (var c in containers.EnumerateArray())
        {
            var name = c.GetProperty("name").GetString()!;
            if (!name.Contains("istio") && !name.Contains("proxy")) candidates.Add(name);
        }

        if (candidates.Count == 1) return candidates[0];
        throw new InvalidOperationException($"Ambiguous target ({string.Join(",", candidates)}). Use --container.");
    }
}