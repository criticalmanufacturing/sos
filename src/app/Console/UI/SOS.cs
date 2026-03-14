using Spectre.Console;
using System.Diagnostics;
using Sos.UI.Utils;
using Cmf.Cli.Plugin.Sos.Commands;
using System.CommandLine;

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
            string action = new OperationSelection(selectedNamespace, selectedPod).Run();
            string pid = new PidSelection().Run(); // PID can be -1 and in that case we use ProcessInspector to gather it.
            string output = new OutputSelection().Run();

            if (action == "Dump")
            {
                DumpCommand dumpCommand = new DumpCommand();
                dumpCommand.Execute(pod: selectedPod, 
                                    output: output,
                                    pid: pid.ToString(),
                                    @namespace: selectedNamespace,
                                    container: null,
                                    image: null); // TODO handle this in a better way
            }
            else if (action == "DotnetCounters")
            {
                int duration = new DurationSelection().Run();
                DotnetCountersCommand dotnetCountersCommand = new DotnetCountersCommand();
                dotnetCountersCommand.Execute(pod: selectedPod,
                                            output: output,
                                            pid: pid.ToString(),
                                            format: "json",
                                            counters: "System.Runtime",
                                            container: null,
                                            @namespace: selectedNamespace,
                                            image: null, // TODO handle this in a better way
                                            duration: duration);  
            }
        }
    }
}