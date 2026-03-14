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

            string action = new OperationSelection(selectedNamespace, selectedPod).Run();

            var pid = new PidSelection().Run(); // pid can be null

            string output = new OutputSelection().Run();

            if (action == "Dump")
            {
                DumpCommand dumpCommand = new DumpCommand();
                dumpCommand.Execute(pod: selectedPod, 
                                    output: output,
                                    pid: pid?.ToString(), // TODO make pid auto detector
                                    @namespace: selectedNamespace,
                                    container: null,
                                    image: null); // TODO handle this in a better way
            }
        }
    }
}