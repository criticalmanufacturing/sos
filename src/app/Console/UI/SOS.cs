using Spectre.Console;
using System.Diagnostics;
using Sos.UI.Utils;
using Cmf.Cli.Plugin.Sos.Commands;
using Cmf.CLI.Utilities;

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
                    FileName = "oc",
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
                AnsiConsole.MarkupLine("[red]No namespaces found! Are you sure you are logged in into openshift ?[/]");
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

                case "DotnetCounters":
                    int duration = new DurationSelection().Run();
                    new DotnetCountersCommand().Execute(
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
                    new RemoteDebugCommand().Execute(
                        pod: selectedPod,
                        pid: AskForPid(),
                        @namespace: selectedNamespace,
                        container: null,
                        image: null!); // TODO handle this in a better way
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
    }
}