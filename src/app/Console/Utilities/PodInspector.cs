using System.Text.Json;

namespace Cmf.Cli.Plugin.Sos.Utilities;

/// <summary>
/// This class provides all the operations in obtaining information about a pod.
/// </summary>
public class PodInspector
{
    private const string AppNameLabel = "app.kubernetes.io/name";

    private readonly KubeCliRunner _kube;
    public PodInspector(KubeCliRunner kube) => _kube = kube;

    /// <summary>
    /// Gets the app name from the pod label app.kubernetes.io/name (e.g. "kafka-ui").
    /// Used to determine runtime (dotnet vs nodejs) for operations.
    /// </summary>
    public string? GetAppName(string pod, string? ns)
    {
        var args = new List<string> { "get", "pod", pod, "-o", "json" };
        if (ns != null) 
        { 
            args.Add("-n"); args.Add(ns); 
        }

        var res = _kube.Run(args);
        using var doc = JsonDocument.Parse(res.StdOut);
        var labels = doc.RootElement.GetProperty("metadata").GetProperty("labels");
        if (labels.TryGetProperty(AppNameLabel, out var nameEl))
        {
            return nameEl.GetString();
        }
        return null;
    }

    /// <summary>
    /// This function will gather the running container name for that specific pod
    /// For example pod <host-12345-abcde> will have the container <host> running.
    /// </summary>
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

        if (candidates.Count == 1) 
        {
            return candidates[0];
        }
        throw new InvalidOperationException($"Ambiguous target ({string.Join(",", candidates)}). Use --container.");
    }
}