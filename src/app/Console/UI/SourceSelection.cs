using Spectre.Console;

namespace Sos.UI.Utils
{
    public class SourceSelection
    {
        public string Run()
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Enter the path to your source code:[/]")
                    .AllowEmpty());
        }
    }
}