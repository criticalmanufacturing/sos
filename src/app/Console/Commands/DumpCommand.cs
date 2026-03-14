using Cmf.CLI.Core;
using Cmf.CLI.Core.Attributes;
using Cmf.CLI.Core.Commands;
using Cmf.Cli.Plugin.Sos.Factories;
using Cmf.Cli.Plugin.Sos.Utilities;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Commands;

[CmfCommand("dump", Id = "dump", ParentId = "sos", Description = "Dump a pod (auto-detects .NET vs Node.js from app.kubernetes.io/name)")]
public sealed class DumpCommand : BaseCommand
{
    public override void Configure(Command cmd)
    {
        var podArg = new Argument<string>("pod", "The name of the target Pod");
        var outputOpt = new Option<string>(new[] { "--output", "-o" }, "Local path to save the dump") { IsRequired = true };
        var pidOption = new Option<string>(new[] { "--pid", "-pid" }, "Process ID of the target process") { IsRequired = true };
        var targetContainerOpt = new Option<string>("--container", "The specific container inside the pod");
        var nsOpt = new Option<string>(new[] { "--namespace", "-n" }, "Namespace of the target pod") { IsRequired = true };
        var imageOpt = new Option<string>("--image", () => "dev.criticalmanufacturing.io/platformengineering/sos:latest", "Debug image");

        cmd.AddArgument(podArg);
        cmd.AddOption(outputOpt);
        cmd.AddOption(pidOption);
        cmd.AddOption(targetContainerOpt);
        cmd.AddOption(nsOpt);
        cmd.AddOption(imageOpt);

        cmd.Handler = CommandHandler.Create<string, string, string, string?, string?, string>(Execute);
    }

    public void Execute(string pod, string output, string pid, string? container, string? @namespace, string image)
    {
        // The following conditions are only used when the user uses the SOS UI. In this case since we call directly execute() we need some way to use default values
        if(image.IsNullOrEmpty()) 
        {
            image = "dev.criticalmanufacturing.io/platformengineering/sos:latest";
        }
        
        var kube = new KubeCliRunner();
        var factory = new SosFactory(kube);
        var ops = factory.CreateForPod(pod, @namespace, "Dump");

        // Auto-resolve PID in case the user doesn't specify it
        if (pid.Equals("-1"))
        {
            var inspector = new ProcessInspector(kube);
            // Since CreateForPod() was already executed we have access to factory.CurrentRuntime
            pid = inspector.ResolvePid(pod, container, @namespace, factory.CurrentRuntime);
            Log.Warning($"PID not provided. Auto-resolved target PID to: {pid}");
        }
        
        try
        {
            ops.Dump(pod, output, pid, container, @namespace, image);
        }
        catch (Exception ex)
        {
            throw new CliException($"Dump operation failed: {ex.Message}");
        }
    }
}
