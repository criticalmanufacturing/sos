using Spectre.Console;
using System.Diagnostics;
using Sos.UI.Utils;

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

            var namespaces = GetNamespaces();

            if (namespaces.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]No namespaces found! Are you sure you are logged in into openshift ?[/]");
                return;
            }

            var selectedNamespace = FilterSystem.Select("Enter namespace", namespaces);

            AnsiConsole.MarkupLine($"\n[blue]Selected namespace:[/] [green]{selectedNamespace}[/]");

            new PodSelection(selectedNamespace).Run();
        }
    }
}