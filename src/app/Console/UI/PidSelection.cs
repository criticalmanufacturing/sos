using Spectre.Console;

namespace Sos.UI
{
    public class PidSelection
    {
        public string Run()
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select PID option:[/]")
                    .HighlightStyle(new Style(Color.Cyan1))
                    .AddChoices("Auto detect", "Enter manually"));

            if (choice == "Enter manually")
            {
                return AnsiConsole.Ask<string>("[green]Enter PID:[/]");
            }

            return "-1"; // In this case, use default value defined in the command.
        }
    }
}