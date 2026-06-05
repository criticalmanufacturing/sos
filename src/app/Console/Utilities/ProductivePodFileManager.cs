

using Cmf.CLI.Core;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public class ProductivePodFileManager
{
    private readonly KubeCliRunner _kube;

    public ProductivePodFileManager(KubeCliRunner kube)
    {
        _kube = kube;
    }

    /// <summary>
    /// This function will delete a specified file from the productive running pod.
    /// Please use this with caution, this function is only intended for cleanup of temporary files during operations and nothing else!!
    /// </summary>
    public void DeleteFileFromProductivePod(string pod, string ns, string filePath)
    {
        // If any of the parameters are null or empty we shall not proceed with the cleanup operation, no exceptions.
        if(string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(pod) || string.IsNullOrWhiteSpace(ns))
        {
            Log.Warning("For this cleanup oepration all parameters below are required.");
            Log.Warning("File path, pod name or namespace is empty. Skipping cleanup of temporary file from productive pod.");
            return;
        }

        var cleanupArgs = new List<string>();

        if (ns != null)
        {
            cleanupArgs.Add("-n");
            cleanupArgs.Add(ns);
        }

        cleanupArgs.Add("exec");
        cleanupArgs.Add(pod);
        cleanupArgs.Add("--");
        cleanupArgs.Add("sh");
        cleanupArgs.Add("-c");

        cleanupArgs.Add($@"
            rm -f {filePath}
        ");


        Log.Information($"Cleaning up temporary file from productive pod ({pod}): {filePath}");
        _kube.Run(cleanupArgs);
    }
}