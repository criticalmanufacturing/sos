using Cmf.CLI.Core;
using Cmf.CLI.Core.Objects;
using Cmf.CLI.Core.Enums;
using Cmf.Sos.Plugin.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Cmf.Sos.Plugin
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Environment.SetEnvironmentVariable("cmf_sos_enable_telemetry", "1");

                var (rootCommand, parser) = await StartupModule.Configure(
                    packageName: "@criticalmanufacturing/cmf-sos",
                    envVarPrefix: "cmf_sos",
                    description: "SOS Container troubleshooting plugin",
                    args: args);

                // Register commands
                rootCommand.AddCommand(SosCommand.Build());

                using var activity = ExecutionContext.ServiceProvider
                    .GetService<ITelemetryService>()!
                    .StartActivity("Main");

                var result = await parser.InvokeAsync(args);
                activity?.SetTag("execution.success", true);

                return result;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return (int)ErrorCode.Default;
            }
        }
    }
}
