using Spectre.Console;

namespace Sos.UI
{
    public class OperationSelection
    {
        private readonly string _namespace;
        private readonly string _pod;

        public OperationSelection(string selectedNamespace, string selectedPod)
        {
            _namespace = selectedNamespace;
            _pod = selectedPod;
        }

        public string Run()
        {
            AnsiConsole.Clear();

            var panel = new Panel($"[bold]Namespace:[/] [yellow]{_namespace}[/]\n[bold]Pod:[/] [yellow]{_pod}[/]")
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1),
                BorderStyle = new Style(Color.Blue)
            };

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            var selectedAction = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select an action:[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(foreground: Color.Cyan1))
                    .AddChoices(new[]
                    {
                        "Dump",
                        "dotnetCounters"
                    }));

            AnsiConsole.MarkupLine($"\n[blue]Selected action:[/] [green]{selectedAction}[/]");

            return selectedAction;
        }
    }
}