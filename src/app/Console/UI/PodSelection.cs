using Spectre.Console;
using System.Diagnostics;
using Sos.UI.Utils;
using Cmf.CLI.Utilities;

namespace Sos.UI
{
    public class PodSelection
    {
        private readonly string _namespace;

        public PodSelection(string selectedNamespace)
        {
            _namespace = selectedNamespace;
        }

        private string[] GetPods()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "kubectl",
                    Arguments = $"get pods -n {_namespace} -o name",
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
                .Select(l => l.Replace("pod/", "").Trim())
                .ToArray();
        }

        public string Run()
        {
            var pods = GetPods();

            if (pods.Length == 0)
            {
                throw new CliException("No pods found in this namespace.");
            }

            var selectedPod = FilterSystem.Select("Enter pod", pods);

            AnsiConsole.MarkupLine($"\n[blue]Selected pod:[/] [green]{selectedPod}[/]");
            return selectedPod;
        }
    }
}