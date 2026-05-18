using Spectre.Console;

namespace Sos.UI.Utils
{
    public class SessionDurationSelection
    {
        public int Run()
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select session duration option:[/]")
                    .HighlightStyle(new Style(Color.Cyan1))
                    .AddChoices("Use default value (20 minutes)", "Enter manually"));

            if (choice == "Enter manually")
            {
                return AnsiConsole.Prompt(
                    new TextPrompt<int>("[green]Enter the debug session duration in minutes:[/]")
                        .ValidationErrorMessage("[red]Please enter a valid positive number[/]")
                        .Validate(duration =>
                            duration > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Duration must be greater than 0[/]")));
            }

            return 20; // Default session duration
        }
    }
}