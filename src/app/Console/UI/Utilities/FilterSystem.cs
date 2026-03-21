using Spectre.Console;

namespace Sos.UI.Utils
{
    /// <summary>
    /// This class is a filtering system used to display information so the user can filter by writing what he wants.
    /// </summary>
    public static class FilterSystem
    {
        public static string Select(string title, string[] items)
        {
            string input = "";
            string? selected = null;
            int previousLines = 0;

            while (selected == null)
            {
                if (previousLines > 0)
                {
                    for (int i = 0; i < previousLines; i++)
                    {
                        Console.Write("\x1b[1A");
                        Console.Write("\x1b[2K");
                    }
                }

                var filtered = items
                    .Where(i => i.Contains(input, StringComparison.OrdinalIgnoreCase))
                    .Take(10)
                    .ToArray();

                Console.Write($"{title}: {input}\n");

                foreach (var item in filtered)
                {
                    AnsiConsole.MarkupLine($"  [yellow]{item}[/]");
                }

                previousLines = filtered.Length + 1;

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    if (filtered.Length == 1)
                        selected = filtered[0];

                    continue;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                        input = input[..^1];
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    input += key.KeyChar;
                }
            }

            return selected;
        }
    }
}