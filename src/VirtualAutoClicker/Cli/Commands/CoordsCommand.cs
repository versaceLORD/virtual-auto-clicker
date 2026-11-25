using System.CommandLine;
using VirtualAutoClicker.Interop;

namespace VirtualAutoClicker.Cli.Commands;

/// <summary><c>coords "Process" [--delay seconds]</c>, also known as <c>pick</c>.</summary>
internal sealed class CoordsCommand : Command
{
    private readonly IWindowLocator _windows;
    private readonly IReplConsole _console;
    private readonly TimeProvider _time;

    private readonly Argument<string> _process = new("process")
    {
        Description = "Name of the process whose window to measure.",
    };

    private readonly Option<int> _delay = new("--delay", "-d")
    {
        Description = "Seconds to wait before reading the cursor position.",
        DefaultValueFactory = _ => 3,
    };

    public CoordsCommand(IWindowLocator windows, IReplConsole console, TimeProvider time)
        : base("coords", "Show the cursor position relative to a window, to find the X,Y to use with 'start'.")
    {
        _windows = windows;
        _console = console;
        _time = time;

        Arguments.Add(_process);
        Options.Add(_delay);
        Aliases.Add("pick");

        SetAction(ExecuteAsync);
    }

    private async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var processName = ProcessWindowLocator.NormalizeProcessName(parseResult.GetRequiredValue(_process));
        if (_windows.Find(processName) is not { } target)
        {
            _console.WriteError($"No running process named '{processName}' with a window was found.");
            return 1;
        }

        var seconds = Math.Clamp(parseResult.GetValue(_delay), 0, 60);
        _console.WriteInfo($"Move the cursor over the spot in {target.ProcessName} you want to click...");
        for (var remaining = seconds; remaining > 0; remaining--)
        {
            _console.WriteInfo($"  {remaining}...");
            await Task.Delay(TimeSpan.FromSeconds(1), _time, cancellationToken).ConfigureAwait(false);
        }

        if (!_windows.TryGetCursorPosition(target.Handle, out var x, out var y))
        {
            _console.WriteError("Could not read the cursor position.");
            return 1;
        }

        if (x < 0 || y < 0)
        {
            _console.WriteWarning($"The cursor is outside the window ({x},{y}).");
            return 1;
        }

        _console.WriteSuccess($"Cursor is at {x},{y} in {target.ProcessName}. Example: start \"{target.ProcessName}\" {x},{y} 100");
        return 0;
    }
}
