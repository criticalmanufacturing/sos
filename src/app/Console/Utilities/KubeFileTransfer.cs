using Cmf.CLI.Core;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public static class KubeFileTransfer
{

    /// <summary>
    /// This function will download a file from the target debugging container to the user local machine.
    /// </summary>
    public static void Download(KubeCliRunner kube, string pod, string? ns, string container, string remotePath, string localPath)
    {
        Log.Information($"Downloading to {localPath} ...");
        
        var cpArgs = new List<string>();
        if (ns != null) 
        { 
            cpArgs.Add("-n"); 
            cpArgs.Add(ns); 
        }
        cpArgs.Add("cp");
        cpArgs.Add("--retries=1");
        cpArgs.Add($"{pod}:{remotePath}");
        cpArgs.Add(localPath);
        cpArgs.Add("-c"); 
        cpArgs.Add(container);

        kube.Run(cpArgs);
    }

    /// <summary>
    /// This function will upload a file from the user local machine to the target debugging container.
    /// </summary>
    public static void Upload(KubeCliRunner kube, string pod, string? ns, string container, string localPath, string remotePath)
    {
        var cpArgs = new List<string>();
        if (ns != null) 
        { 
            cpArgs.Add("-n"); 
            cpArgs.Add(ns); 
        }
        cpArgs.Add("cp");
        cpArgs.Add("--retries=-1");
        cpArgs.Add(localPath);
        cpArgs.Add($"{pod}:{remotePath}");
        cpArgs.Add("-c"); 
        cpArgs.Add(container);

        kube.Run(cpArgs);
    }
}