using System.CommandLine;
using VirtualAutoClicker.Clicking;

namespace VirtualAutoClicker.Cli.Commands;

/// <summary><c>resume name</c></summary>
internal sealed class ResumeCommand : Command
{
    private readonly ClickerManager _manager;
    private readonly IReplConsole _console;
    private readonly Argument<string> _name = ClickerLookup.NameArgument();

    public ResumeCommand(ClickerManager manager, IReplConsole console)
        : base("resume", "Resume a paused (or failed) clicker.")
    {
        _manager = manager;
        _console = console;

        Arguments.Add(_name);
        Aliases.Add("resumeautoclicker");

        SetAction(Execute);
    }

    private int Execute(ParseResult parseResult)
    {
        var name = parseResult.GetRequiredValue(_name);
        if (ClickerLookup.FindOrWarn(_manager, _console, name) is not { } clicker)
        {
            return 1;
        }

        var before = clicker.State;
        if (clicker.Resume())
        {
            _console.WriteSuccess($"Clicker '{clicker.Name}' resumed.");
            return 0;
        }

        _console.WriteWarning(before switch
        {
            ClickerState.Running => $"Clicker '{clicker.Name}' is already running.",
            ClickerState.Faulted => $"Clicker '{clicker.Name}' cannot resume: no window of '{clicker.Options.ProcessName}' was found.",
            _ => $"Clicker '{clicker.Name}' cannot be resumed ({clicker.StatusText}).",
        });
        return 1;
    }
}
