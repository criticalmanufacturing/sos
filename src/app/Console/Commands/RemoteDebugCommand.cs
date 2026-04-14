using Cmf.CLI.Core;
using Cmf.CLI.Core.Attributes;
using Cmf.CLI.Core.Commands;
using Cmf.Cli.Plugin.Sos.Factories;
using Cmf.Cli.Plugin.Sos.Utilities;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Commands;

[CmfCommand("remoteDebug", Id = "remoteDebug", ParentId = "sos", Description = "Start a remote debug session for a Node.js pod")]
public sealed class RemoteDebugCommand : BaseCommand
{
    public override void Configure(Command cmd)
    {
        var podArg = new Argument<string>("pod", "The name of the target Pod");
        var pidOption = new Option<string>(new[] { "--pid", "-pid" }, "Process ID of the target process") { IsRequired = true };
        var targetContainerOpt = new Option<string>("--container", "The specific container inside the pod");
        var nsOpt = new Option<string>(new[] { "--namespace", "-n" }, "Namespace of the target pod") { IsRequired = true };
        var imageOpt = new Option<string>("--image", () => "dev.criticalmanufacturing.io/platformengineering/sos:latest", "Debug image");
        var pdbsOpt = new Option<string?>("--pdbs", "Local path to the PDBs directory (Required for .NET)");
        var sourceOpt = new Option<string?>("--source", "Local path to the Product Source Code (Required for .NET)");

        cmd.AddArgument(podArg);
        cmd.AddOption(pidOption);
        cmd.AddOption(targetContainerOpt);
        cmd.AddOption(nsOpt);
        cmd.AddOption(imageOpt);
        cmd.AddOption(pdbsOpt);
        cmd.AddOption(sourceOpt);

        cmd.Handler = CommandHandler.Create<string, string, string?, string?, string, string?, string?>(Execute);
    }

    public void Execute(string pod, string pid, string? container, string? @namespace, string image, string? pdbs, string? source)
    {
        if(string.IsNullOrWhiteSpace(image)) 
        {
            image = "dev.criticalmanufacturing.io/platformengineering/sos:latest";
        }
        
        var kube = new KubeCliRunner();
        var factory = new SosFactory(kube);
        var ops = factory.CreateForPod(pod, @namespace, "remoteDebug");

        // Auto-resolve PID in case the user doesn't specify it
        if (pid.Equals("-1"))
        {
            var inspector = new ProcessInspector(kube);
            pid = inspector.ResolvePid(pod, container, @namespace, factory.CurrentRuntime);
            Log.Warning($"PID not provided. Auto-resolved target PID to: {pid}");
        }
        
        try
        {
            ops.RemoteDebug(pod, pid, container, @namespace, image, pdbs, source);
        }
        catch (Exception ex)
        {
            throw new CliException($"Remote Debug operation failed: {ex.Message}");
        }
    }
}