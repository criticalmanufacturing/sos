using Cmf.CLI.Core;
using Cmf.CLI.Core.Attributes;
using Cmf.CLI.Core.Commands;
using Cmf.Cli.Plugin.Sos.Utilities;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Commands;

[CmfCommand("shell", Id = "shell", ParentId = "sos", Description = "Start an interactive shell in a debug container attached to a pod")]
public sealed class InteractiveShellCommand : BaseCommand
{
    public override void Configure(Command cmd)
    {
        var podArg = new Argument<string>("pod", "The name of the target Pod");
        var nsOpt = new Option<string>(new[] { "--namespace", "-n" }, "Namespace of the target pod") { IsRequired = true };
        var targetContainerOpt = new Option<string>("--container", "The specific container inside the pod");
        var imageOpt = new Option<string>("--image", () => "dev.criticalmanufacturing.io/platformengineering/sos:latest", "Debug image");

        cmd.AddArgument(podArg);
        cmd.AddOption(nsOpt);
        cmd.AddOption(targetContainerOpt);
        cmd.AddOption(imageOpt);

        cmd.Handler = CommandHandler.Create<string, string, string?, string>(Execute);
    }

    /// <summary>
    /// This function orchestrates the process of starting an interactive shell in a debug container attached to a specified pod.
    /// Since it's using the debug container it has the same functionalities as using kubectl debug, but with the added benefit of automatic cleanup and better shell integration.
    /// </summary>
    public void Execute(string pod, string @namespace, string? container, string image)
    {
        if(string.IsNullOrWhiteSpace(image))
        {
            image = "dev.criticalmanufacturing.io/platformengineering/sos:latest";
        }

        var kube = new KubeCliRunner();
        var session = new DebugSessionManager(kube);
        
        try
        {
            var podInspector = new PodInspector(kube);
            var targetContainer = string.IsNullOrWhiteSpace(container) 
                ? podInspector.ResolveTargetContainer(pod, @namespace) 
                : container;

            Log.Information("Starting interactive debug session...");
            var debugContainer = session.Start(pod, targetContainer, image, @namespace);

            Log.Information("Entering interactive shell. Type 'exit' to leave.");
            
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "kubectl",
                Arguments = $"exec -it -n {@namespace} {pod} -c {debugContainer} -- bash",
                UseShellExecute = false
            });
            
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            throw new CliException($"Interactive shell failed: {ex.Message}");
        }
        finally
        {
            Log.Information("Closing debug session...");
            session.Close();
        }
    }
}