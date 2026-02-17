using System.CommandLine;
using Cmf.CLI.Core.Attributes;
using Cmf.CLI.Core.Commands;

namespace Cmf.Cli.Plugin.Sos.Commands.host;

/// <summary>
/// `cmf sos host ...`
/// </summary>
[CmfCommand("host", Id = "host", ParentId = "sos", Description = "Host/node level SoS operations")]
public sealed class HostCommand : BaseCommand
{
    public override void Configure(Command cmd)
    {
        // Acts as a grouping command. Options can be added later if needed.
    }
}

