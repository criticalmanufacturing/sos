using Spectre.Console;

namespace Sos.UI
{
    public class PidSelection
    {
        public int? Run()
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select PID option:[/]")
                    .HighlightStyle(new Style(Color.Cyan1))
                    .AddChoices("Auto detect", "Enter manually"));

            if (choice == "Enter manually")
            {
                return AnsiConsole.Ask<int>("[green]Enter PID:[/]");
            }

            return null;
        }
    }
}