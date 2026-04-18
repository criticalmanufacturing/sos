using Spectre.Console;
using System.Diagnostics;
using Sos.UI.Utils;
using Cmf.Cli.Plugin.Sos.Commands;
using Cmf.CLI.Utilities;
using Cmf.Cli.Plugin.Sos.Runtime;

namespace Sos.UI
{
    public class SOS
    {
        private string[] GetNamespaces()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "kubectl",
                    Arguments = "get ns -o name",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Replace("namespace/", "").Trim())
                .ToArray();
        }

        public void Run()
        {
            AnsiConsole.MarkupLine("[green]SOS is starting...[/]");

            string[] namespaces = GetNamespaces();

            if (namespaces.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]No namespaces found! Are you sure you are logged in into the cluster ?[/]");
                return;
            }

            string selectedNamespace = FilterSystem.Select("Enter namespace", namespaces);

            AnsiConsole.MarkupLine($"\n[blue]Selected namespace:[/] [green]{selectedNamespace}[/]");

            string selectedPod = new PodSelection(selectedNamespace).Run();

            // After selecting the namespace we gather the mandatory information for any operation 
            string action = new OperationSelection(selectedNamespace, selectedPod).Run(); // This action string must be equal to the switch cases below.

            switch (action)
            {
                case "Dump":
                    new DumpCommand().Execute(
                        pod: selectedPod,
                        output: AskForOutput(),
                        pid: AskForPid(),
                        @namespace: selectedNamespace,
                        container: null,
                        image: null!); // TODO handle this in a better way
                    break;

                case "Runtime Metrics":
                    int duration = new DurationSelection().Run();
                    new RuntimeMetricsCommand().Execute(
                        pod: selectedPod,
                        output: AskForOutput(),
                        pid: AskForPid(),
                        format: "json",
                        counters: "System.Runtime",
                        container: null,
                        @namespace: selectedNamespace,
                        image: null!, // TODO handle this in a better way
                        duration: duration);
                    break;

                case "Interactive Shell":
                    new InteractiveShellCommand().Execute(
                        pod: selectedPod,
                        @namespace: selectedNamespace,
                        container: null,
                        image: null!); // TODO handle this in a better way
                    break;

                case "Remote Debug":
                    string? source = null;
                    string? pid = null;
                    // Determine the runtime by checking if the pod name contains known identifiers (e.g., 'host', 'node').
                    // This is more reliable for the UI than assuming the pod name is an exact match to a registered app name.
                    AppRuntime podRuntime = AppRuntimeRegistry.GetRuntimeFromPodName(selectedPod);

                    if (podRuntime == AppRuntime.Dotnet)
                    {
                        // If the pod is .NET, we need to ask for the source code path
                        source = AskForSource();
                    }
                    else if(podRuntime == AppRuntime.NodeJs)
                    {
                        // If the pod is Node.js, we need to ask for the Process ID (PID)
                        pid = AskForPid();
                    }
                    else 
                    {
                        throw new CliException("Unknown selected pod runtime. Please ensure this is a compatible pod with remote debug.");
                    }

                    new RemoteDebugCommand().Execute(
                        pod: selectedPod,
                        pid: pid,
                        @namespace: selectedNamespace,
                        container: null,
                        image: null!,
                        source: source);
                    break;

                default:
                    throw new CliException($"Unknown action: {action}");
            }
        }

        private string AskForPid()
        {
            return new PidSelection().Run(); // PID can be -1 and in that case we use ProcessInspector to gather it.
        }

        private string AskForOutput()
        {
            return new OutputSelection().Run();
        }

        private string AskForSource()
        {
            return new SourceSelection().Run();
        }
    }
}