using Cmf.CLI.Core;
using Cmf.CLI.Core.Attributes;
using Cmf.CLI.Core.Commands;
using Cmf.Cli.Plugin.Sos.Factories;
using Cmf.Cli.Plugin.Sos.Utilities;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Cmf.Cli.Plugin.Sos.Commands;

[CmfCommand("dotnetCounters", Id = "dotnetCounters", ParentId = "sos", Description = "Collect .NET counters from a .NET pod (no effect on Node.js pods)")]
public sealed class DotnetCountersCommand : BaseCommand
{
    public override void Configure(Command cmd)
    {
        var podArg = new Argument<string>("pod", "The name of the target Pod");
        var outputOpt = new Option<string>(new[] { "--output", "-o" }, "Local path to save the counters") { IsRequired = true };
        var pidOption = new Option<string>(new[] { "--pid", "-pid" }, "Process ID of the target process") { IsRequired = true };
        var formatOpt = new Option<string>(new[] { "--format" }, () => "json", "The format of the counters file (json or csv)");
        var durationOpt = new Option<int>(new[] { "--duration" }, () => 60, "The duration in seconds for which to collect counters. Defaults to 60s.");
        var countersOpt = new Option<string>(new[] { "--counters" }, () => "System.Runtime", "A space-separated list of counters to collect.");
        var targetContainerOpt = new Option<string>("--container", "The specific container inside the pod");
        var nsOpt = new Option<string>(new[] { "--namespace", "-n" }, "Namespace of the target pod") { IsRequired = true };
        var imageOpt = new Option<string>("--image", () => "dev.criticalmanufacturing.io/platformengineering/sos:latest", "Debug image");

        cmd.AddArgument(podArg);
        cmd.AddOption(outputOpt);
        cmd.AddOption(pidOption);
        cmd.AddOption(formatOpt);
        cmd.AddOption(durationOpt);
        cmd.AddOption(countersOpt);
        cmd.AddOption(targetContainerOpt);
        cmd.AddOption(nsOpt);
        cmd.AddOption(imageOpt);

        cmd.Handler = CommandHandler.Create<string, string, string, string, int, string, string?, string?, string>(Execute);
    }

    public void Execute(string pod, string output, string pid, string format, int duration, string counters, string? container, string? @namespace, string image)
    {
        var kube = new KubeCliRunner();
        var factory = new SosFactory(kube);
        var ops = factory.CreateForPod(pod, @namespace, "dotnetCounters");
        try
        {
            ops.DotnetCounters(pod, output, pid, format, duration, counters, container, @namespace, image);
        }
        catch (Exception ex)
        {
            Log.Error($"Counters operation failed: {ex.Message}");
            throw;
        }
    }
}