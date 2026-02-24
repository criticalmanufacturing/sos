using Cmf.CLI.Core;
using Cmf.CLI.Core.Attributes;
using Cmf.CLI.Core.Commands;
using Cmf.Cli.Plugin.Sos.Factories;
using Cmf.Cli.Plugin.Sos.Utilities;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

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
        var cliOpt = new Option<string>("--cli", () => "kubectl", "CLI tool");
        var imageOpt = new Option<string>("--image", () => "dev.criticalmanufacturing.io/platformengineering/sos:latest", "Debug image");

        cmd.AddArgument(podArg);
        cmd.AddOption(outputOpt);
        cmd.AddOption(pidOption);
        cmd.AddOption(targetContainerOpt);
        cmd.AddOption(nsOpt);
        cmd.AddOption(cliOpt);
        cmd.AddOption(imageOpt);

        cmd.Handler = CommandHandler.Create<string, string, string, string?, string?, string, string>(Execute);
    }

    public void Execute(string pod, string output, string pid, string? container, string? @namespace, string cli, string image)
    {
        var kube = new KubeCliRunner();
        var factory = new SosFactory(kube);
        var ops = factory.CreateForPod(pod, @namespace, "Dump");
        try
        {
            ops.Dump(pod, output, pid, container, @namespace, image);
        }
        catch (Exception ex)
        {
            Log.Error($"Dump operation failed: {ex.Message}");
            throw;
        }
    }
}
