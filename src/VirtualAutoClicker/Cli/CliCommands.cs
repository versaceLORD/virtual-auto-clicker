using System.CommandLine;
using System.CommandLine.Parsing;
using VirtualAutoClicker.Cli.Commands;
using VirtualAutoClicker.Clicking;
using VirtualAutoClicker.Interop;

namespace VirtualAutoClicker.Cli;

/// <summary>
/// Builds the command tree (one class per command in <c>Cli/Commands</c>) and runs input against it.
/// Shared by one-shot mode (process arguments) and the interactive REPL.
/// </summary>
internal sealed class CliCommands
{
    private readonly IReplConsole _console;
    private readonly ExitCommand _exit = new();

    public CliCommands(ClickerManager manager, IWindowLocator windows, IReplConsole console, TimeProvider time)
    {
        _console = console;

        Root = new RootCommand(
            "Sends background mouse clicks to a window, so you can keep using your own mouse. " +
            "Run without arguments for the interactive prompt.");

        Root.Subcommands.Add(new StartCommand(manager, console));
        Root.Subcommands.Add(new PauseCommand(manager, console));
        Root.Subcommands.Add(new ResumeCommand(manager, console));
        Root.Subcommands.Add(new StopCommand(manager, console));
        Root.Subcommands.Add(new ListCommand(manager, console));
        Root.Subcommands.Add(new CoordsCommand(windows, console, time));
        Root.Subcommands.Add(new HelpCommand(Root, console));
        Root.Subcommands.Add(_exit);
    }

    public RootCommand Root { get; }

    /// <summary>Set by the <c>exit</c> command.</summary>
    public bool ExitRequested => _exit.Requested;

    /// <summary>Parses and runs a REPL line such as <c>start "My Game" 450,450 50</c>.</summary>
    public Task<int> InvokeAsync(string commandLine, CancellationToken cancellationToken = default) =>
        InvokeAsync([.. CommandLineParser.SplitCommandLine(commandLine)], cancellationToken);

    /// <summary>Parses and runs pre-split arguments.</summary>
    public Task<int> InvokeAsync(IReadOnlyList<string> args, CancellationToken cancellationToken = default)
    {
        var parseResult = Root.Parse(NormalizeCommandName(args));
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                _console.WriteError(error.Message);
            }

            var commandName = parseResult.CommandResult.Command == Root ? null : parseResult.CommandResult.Command.Name;
            _console.WriteInfo(commandName is null
                ? "Type 'help' for the list of commands."
                : $"Type '{commandName} --help' for usage.");
            return Task.FromResult(1);
        }

        var configuration = new InvocationConfiguration
        {
            Output = _console.Out,
            Error = _console.ErrorOut,
            EnableDefaultExceptionHandler = false,
        };

        return parseResult.InvokeAsync(configuration, cancellationToken);
    }

    /// <summary>
    /// Command names were case-insensitive in 1.x (<c>StartAutoClicker</c>), keep it that way.
    /// </summary>
    private string[] NormalizeCommandName(IReadOnlyList<string> args)
    {
        var tokens = args.ToArray();
        if (tokens.Length == 0)
        {
            return tokens;
        }

        var match = Root.Subcommands.FirstOrDefault(c =>
            c.Name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase) ||
            c.Aliases.Any(a => a.Equals(tokens[0], StringComparison.OrdinalIgnoreCase)));

        if (match is not null)
        {
            tokens[0] = match.Name;
        }

        return tokens;
    }
}
