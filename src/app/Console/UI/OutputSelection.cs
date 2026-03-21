using Spectre.Console;

namespace Sos.UI
{
    public class OutputSelection
    {
        private const string DefaultPath = "/tmp/dump_pod_timestamp.extension";

        public string Run()
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select output option:[/]")
                    .HighlightStyle(new Style(Color.Cyan1))
                    .AddChoices(
                        $"Use default ({DefaultPath})",
                        "Enter manually"
                    ));

            if (choice == "Enter manually")
            {
                return AnsiConsole.Ask<string>(
                    "[green]Enter output path:[/]",
                    DefaultPath);
            }

            return DefaultPath;
        }
    }
}