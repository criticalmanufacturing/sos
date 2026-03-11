using Spectre.Console;

namespace Sos.UI;

public class MainMenu
{
    public void Show()
    {
        AnsiConsole.Clear();

        var whale = @"
                        ','. '. ; : ,','
                            '..'.,',..'
                                ';.'  ,'
                                ;;
                                ;'
                :._   _.------------.___
        __      :__:-'                  '--.
    __   ,' .'    .'             ______________'.
/__ '.-  _\___.'          0  .' .'  .'  _.-_.'
    '._                     .-': .' _.' _.'_.'
        '----'._____________.'_'._:_:_.-'--'";

        var content = new Markup(
            $"[blue]{whale}[/]\n" +
            "\n[bold cyan]SOS Container[/]\n" +
            "[grey]v1.0.0[/]"
        );

        var panel = new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(1, 1),
            Width = 55 // controls panel width explicitly
        };

        AnsiConsole.Write(
            Align.Left(panel)
        );

        AnsiConsole.WriteLine();

        var selectedCommand = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]What would you like to do today?[/]")
                .PageSize(10)
                .HighlightStyle(new Style(foreground: Color.Cyan1))
                .AddChoices(new[]
                {
                    "SOS",
                    "Exit"
                }));

        AnsiConsole.Clear();

        // Switch for handling commands
        switch (selectedCommand)
        {
            case "SOS":
                new SOS().Run();
                break;

            case "Exit":
                AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                break;
        }
    }
}