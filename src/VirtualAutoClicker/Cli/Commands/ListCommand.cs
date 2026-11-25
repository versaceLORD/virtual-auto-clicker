using System.CommandLine;
using System.Globalization;
using System.Text;
using VirtualAutoClicker.Clicking;

namespace VirtualAutoClicker.Cli.Commands;

/// <summary><c>list</c></summary>
internal sealed class ListCommand : Command
{
    private readonly ClickerManager _manager;
    private readonly IReplConsole _console;

    public ListCommand(ClickerManager manager, IReplConsole console)
        : base("list", "List all clickers.")
    {
        _manager = manager;
        _console = console;

        Aliases.Add("listall");
        Aliases.Add("listautoclickers");

        SetAction(_ => Execute());
    }

    private int Execute()
    {
        var clickers = _manager.Clickers;
        if (clickers.Count == 0)
        {
            _console.WriteInfo("No clickers. Start one with: start \"Process\" X,Y interval [name]");
            return 0;
        }

        _console.WriteInfo(FormatTable(clickers));
        return 0;
    }

    internal static string FormatTable(IReadOnlyList<Clicker> clickers)
    {
        string[] headers = ["Name", "State", "Process", "Position", "Interval", "Clicks"];
        var rows = clickers
            .Select(c =>
            {
                var target = c.Target;
                return new[]
                {
                    c.Name,
                    c.StatusText,
                    $"{target.ProcessName} ({target.ProcessId})",
                    c.Options.Position.ToString(),
                    string.Create(CultureInfo.InvariantCulture, $"{c.Options.Interval.TotalMilliseconds:0} ms"),
                    c.ClickCount.ToString(CultureInfo.InvariantCulture),
                };
            })
            .ToList();

        var widths = headers
            .Select((h, i) => Math.Max(h.Length, rows.Max(r => r[i].Length)))
            .ToArray();

        var builder = new StringBuilder();
        AppendRow(builder, headers, widths);
        AppendRow(builder, [.. widths.Select(w => new string('-', w))], widths);
        foreach (var row in rows)
        {
            AppendRow(builder, row, widths);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendRow(StringBuilder builder, string[] cells, int[] widths)
    {
        builder.Append("  ");
        for (var i = 0; i < cells.Length; i++)
        {
            builder.Append(cells[i].PadRight(widths[i]));
            if (i < cells.Length - 1)
            {
                builder.Append("  ");
            }
        }

        builder.AppendLine();
    }
}
