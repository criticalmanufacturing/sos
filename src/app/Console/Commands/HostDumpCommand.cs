using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Cmf.Cli.Plugin.Sos.Utilities;
using Cmf.CLI.Core;
using Cmf.CLI.Core.Attributes;
using Cmf.CLI.Core.Commands;

namespace Cmf.Cli.Plugin.Sos.Commands.host;

[CmfCommand("dotnetDump", Id = "dotnetDump", ParentId = "host", Description = "Inject SOS container to dump a .NET pod")]
public sealed class DotnetDumpCommand : BaseCommand
{
    public override void Configure(Command cmd)
    {
        var podArg = new Argument<string>("pod", "The name of the target Pod");
        var outputOpt = new Option<string>(new[] { "--output", "-o" }, "Local path to save the dump") { IsRequired = true };
        var targetContainerOpt = new Option<string>("--container", "The specific container inside the pod");
        var nsOpt = new Option<string>(new[] { "--namespace", "-n" }, "Namespace of the target pod") { IsRequired = true };
        var cliOpt = new Option<string>("--cli", () => "kubectl", "CLI tool");
        var imageOpt = new Option<string>("--image", () => "dev.criticalmanufacturing.io/platformengineering/sos:latest", "Debug image");
        
        cmd.AddArgument(podArg);
        cmd.AddOption(outputOpt);
        cmd.AddOption(targetContainerOpt);
        cmd.AddOption(nsOpt);
        cmd.AddOption(cliOpt);
        cmd.AddOption(imageOpt);

        cmd.Handler = CommandHandler.Create<string, string, string?, string?, string, string>(Execute);
    }

    public void Execute(string pod, string output, string? container, string? @namespace, string cli, string image)
    {
        var orchestrator = new DumpOrchestrator(new KubeCliRunner());
        try
        {
            orchestrator.Execute(pod, output, container, @namespace, image);
        }
        catch (Exception ex)
        {
            Log.Error($"Dump operation failed: {ex.Message}");
            throw; 
        }
    }
}
