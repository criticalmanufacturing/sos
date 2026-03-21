using Spectre.Console;

namespace Sos.UI
{
    public class DurationSelection
    {
        public int Run()
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select duration option:[/]")
                    .HighlightStyle(new Style(Color.Cyan1))
                    .AddChoices("Use default value (60 seconds)", "Enter manually"));

            if (choice == "Enter manually")
            {
                return AnsiConsole.Ask<int>("[green]Enter duration in seconds:[/]");
            }

            return -1; // In this case, use default value defined in the command.
        }
    }
}