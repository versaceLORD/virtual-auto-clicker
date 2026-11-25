using System.CommandLine;
using VirtualAutoClicker.Clicking;

namespace VirtualAutoClicker.Cli.Commands;

/// <summary><c>pause name</c></summary>
internal sealed class PauseCommand : Command
{
    private readonly ClickerManager _manager;
    private readonly IReplConsole _console;
    private readonly Argument<string> _name = ClickerLookup.NameArgument();

    public PauseCommand(ClickerManager manager, IReplConsole console)
        : base("pause", "Pause a running clicker.")
    {
        _manager = manager;
        _console = console;

        Arguments.Add(_name);
        Aliases.Add("pauseautoclicker");

        SetAction((parseResult, _) => ExecuteAsync(parseResult));
    }

    private async Task<int> ExecuteAsync(ParseResult parseResult)
    {
        var name = parseResult.GetRequiredValue(_name);
        if (ClickerLookup.FindOrWarn(_manager, _console, name) is not { } clicker)
        {
            return 1;
        }

        if (!await clicker.PauseAsync().ConfigureAwait(false))
        {
            _console.WriteWarning($"Clicker '{clicker.Name}' is not running ({clicker.StatusText}).");
            return 1;
        }

        _console.WriteSuccess($"Clicker '{clicker.Name}' paused.");
        return 0;
    }
}
