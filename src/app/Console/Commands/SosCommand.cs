using System.CommandLine;
using Cmf.CLI.Core;
using Cmf.CLI.Core.Attributes;
using Cmf.CLI.Core.Commands;

namespace Cmf.Cli.Plugin.Sos.Commands;

/// <summary>
/// Root `sos` command: `cmf sos ...`
/// </summary>
[CmfCommand("sos", Id = "sos", Description = "SoS high-level Kubernetes/OpenShift operations")]
public sealed class SosCommand : BaseCommand
{
    public override void Configure(Command cmd)
    {
        // No extra configuration needed at this level for now.
        // Subcommands such as `host` and `pod` will attach via ParentId.
        Log.Information("Sos called.");
    }
    
    public void Execute() {
        Log.Information("execute called");
    }
}

