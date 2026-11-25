using System.CommandLine;
using VirtualAutoClicker.Clicking;

namespace VirtualAutoClicker.Cli.Commands;

/// <summary><c>stop [name]</c>, also known as <c>picnic</c>.</summary>
internal sealed class StopCommand : Command
{
    private readonly ClickerManager _manager;
    private readonly IReplConsole _console;

    private readonly Argument<string?> _name = new("name")
    {
        Description = "Clicker to stop. Omit to stop all clickers.",
        Arity = ArgumentArity.ZeroOrOne,
    };

    public StopCommand(ClickerManager manager, IReplConsole console)
        : base("stop", "Stop and remove a clicker, or all clickers.")
    {
        _manager = manager;
        _console = console;

        Arguments.Add(_name);
        Aliases.Add("stopautoclicker");
        Aliases.Add("picnic");

        SetAction((parseResult, _) => ExecuteAsync(parseResult));
    }

    private async Task<int> ExecuteAsync(ParseResult parseResult)
    {
        var name = parseResult.GetValue(_name);
        if (string.IsNullOrWhiteSpace(name))
        {
            var stopped = await _manager.StopAllAsync().ConfigureAwait(false);
            if (stopped == 0)
            {
                _console.WriteInfo("There are no clickers to stop.");
            }
            else
            {
                _console.WriteSuccess($"Stopped {stopped} clicker(s).");
            }

            return 0;
        }

        if (!await _manager.StopAsync(name).ConfigureAwait(false))
        {
            ClickerLookup.WarnNotFound(_console, name);
            return 1;
        }

        _console.WriteSuccess($"Clicker '{name}' stopped.");
        return 0;
    }
}
