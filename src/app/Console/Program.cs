using Cmf.CLI.Core.Enums;
using Cmf.CLI.Core.Objects;
using Cmf.CLI.Core;
using Cmf.CLI.Utilities;
using ExecutionContext = Cmf.CLI.Core.Objects.ExecutionContext;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Parsing;
using Sos.UI;

try
{
    // as it's an internal development tool, we keep telemetry on by default
    Environment.SetEnvironmentVariable("cmf_sos_enable_telemetry", "1");
    Environment.SetEnvironmentVariable("cmf_sos_enable_extended_telemetry", "1");

    var registryAddress = Environment.GetEnvironmentVariable("cmf_sos_registry");

    var (rootCommand, parser) = await StartupModule.Configure(
        packageName: "@criticalmanufacturing/sos",
        envVarPrefix: "cmf_sos",
        description: "Plugin to set up VM environments for Critical Manufacturing MES",
        args: args,
        npmClient: new VerdaccioService(new Uri(registryAddress ?? "https://dev.criticalmanufacturing.io/repository/npm-public")));

    using var activity = ExecutionContext.ServiceProvider.GetService<ITelemetryService>()!.StartActivity("Main");

    rootCommand.SetHandler(() =>
    {
        Log.Debug("No subcommand provided. Launching interactive TUI.");
        var menu = new MainMenu();
        menu.Show();
    });

    var result = await parser.InvokeAsync(args);
    activity?.SetTag("execution.success", true);
    return result;
}
catch (CliException e)
{
    Log.Error(e.Message);
    Log.Debug(e.StackTrace);
    return (int)e.ErrorCode;
}
catch (Exception e)
{
    Log.Debug("Caught exception at program.");
    Log.Exception(e);
    ExecutionContext.ServiceProvider.GetService<ITelemetryService>()!.LogException(e);
    return (int)ErrorCode.Default;
}
