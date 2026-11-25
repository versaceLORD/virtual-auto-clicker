using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Text;
using VirtualAutoClicker.Clicking;
using VirtualAutoClicker.Interop;

namespace VirtualAutoClicker.Cli;

/// <summary>
/// The command tree, shared by one-shot mode (process arguments) and the interactive REPL.
/// </summary>
internal sealed class CliCommands
{
    private readonly ClickerManager _manager;
    private readonly IWindowLocator _windows;
    private readonly IReplConsole _console;
    private readonly TimeProvider _time;

    public CliCommands(ClickerManager manager, IWindowLocator windows, IReplConsole console, TimeProvider time)
    {
        _manager = manager;
        _windows = windows;
        _console = console;
        _time = time;
        Root = BuildRoot();
    }

    public RootCommand Root { get; }

    /// <summary>Set by the <c>exit</c> command.</summary>
    public bool ExitRequested { get; private set; }

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

    private RootCommand BuildRoot()
    {
        var root = new RootCommand(
            "Sends background mouse clicks to a window, so you can keep using your own mouse. " +
            "Run without arguments for the interactive prompt.");

        root.Subcommands.Add(BuildStart());
        root.Subcommands.Add(BuildPause());
        root.Subcommands.Add(BuildResume());
        root.Subcommands.Add(BuildStop());
        root.Subcommands.Add(BuildList());
        root.Subcommands.Add(BuildCoords());
        root.Subcommands.Add(BuildHelp(root));
        root.Subcommands.Add(BuildExit());

        return root;
    }

    private Command BuildStart()
    {
        var process = new Argument<string>("process")
        {
            Description = "Name of the process to click in, e.g. \"Firestone\". Quote names with spaces.",
        };

        var position = new Argument<ClientPoint>("position")
        {
            Description = "X,Y relative to the top-left corner of the window's client area, e.g. 450,450.",
            CustomParser = result =>
            {
                if (!ClientPoint.TryParse(result.Tokens[0].Value, out var error, out var point))
                {
                    result.AddError(error);
                }

                return point;
            },
        };

        var interval = new Argument<int>("interval")
        {
            Description = "Milliseconds between clicks (at least 1).",
            CustomParser = result =>
            {
                var text = result.Tokens[0].Value;
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms) || ms < 1)
                {
                    result.AddError($"Invalid interval '{text}', expected a whole number of milliseconds of at least 1.");
                }

                return ms;
            },
        };

        var name = new Argument<string?>("name")
        {
            Description = "Optional name for the clicker, used with pause/resume/stop. Defaults to clicker-N.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var mode = new Option<ClickMode>("--mode", "-m")
        {
            Description = "How clicks are delivered: post (default, never blocks) or send (synchronous, 1 s timeout).",
            DefaultValueFactory = _ => ClickMode.Post,
        };

        var command = new Command("start", "Start a new auto clicker.") { process, position, interval, name, mode };
        command.Aliases.Add("startautoclicker");

        command.SetAction(parseResult =>
        {
            var processName = ProcessWindowLocator.NormalizeProcessName(parseResult.GetRequiredValue(process));
            if (processName.Length == 0)
            {
                _console.WriteError("The process name cannot be empty.");
                return 1;
            }

            var clickerName = parseResult.GetValue(name);
            if (string.IsNullOrWhiteSpace(clickerName))
            {
                clickerName = _manager.NextName();
            }

            var options = new ClickerOptions(
                clickerName.Trim(),
                processName,
                parseResult.GetRequiredValue(position),
                TimeSpan.FromMilliseconds(parseResult.GetRequiredValue(interval)),
                parseResult.GetValue(mode));

            switch (_manager.TryStart(options, out var clicker))
            {
                case StartClickerResult.Started:
                    var target = clicker!.Target;
                    _console.WriteSuccess(
                        $"Clicker '{options.Name}' started on {target.ProcessName} (pid {target.ProcessId}) " +
                        $"at {options.Position} every {options.Interval.TotalMilliseconds:0} ms.");

                    if (options.Interval < ClickerOptions.LowIntervalWarningThreshold)
                    {
                        _console.WriteWarning(
                            $"Intervals below {ClickerOptions.LowIntervalWarningThreshold.TotalMilliseconds:0} ms can make the target application unresponsive.");
                    }

                    return 0;

                case StartClickerResult.DuplicateName:
                    _console.WriteError($"A clicker named '{options.Name}' already exists. Pick another name or stop it first.");
                    return 1;

                case StartClickerResult.ProcessNotFound:
                    _console.WriteError($"No running process named '{processName}' with a window was found.");
                    return 1;

                default:
                    throw new InvalidOperationException("Unknown start result.");
            }
        });

        return command;
    }

    private Command BuildPause()
    {
        var name = NameArgument();
        var command = new Command("pause", "Pause a running clicker.") { name };
        command.Aliases.Add("pauseautoclicker");

        command.SetAction(async (parseResult, _) =>
        {
            var clickerName = parseResult.GetRequiredValue(name);
            if (FindOrWarn(clickerName) is not { } clicker)
            {
                return 1;
            }

            if (!await clicker.PauseAsync().ConfigureAwait(false))
            {
                _console.WriteWarning($"Clicker '{clicker.Name}' is not running ({Describe(clicker)}).");
                return 1;
            }

            _console.WriteSuccess($"Clicker '{clicker.Name}' paused.");
            return 0;
        });

        return command;
    }

    private Command BuildResume()
    {
        var name = NameArgument();
        var command = new Command("resume", "Resume a paused (or failed) clicker.") { name };
        command.Aliases.Add("resumeautoclicker");

        command.SetAction(parseResult =>
        {
            var clickerName = parseResult.GetRequiredValue(name);
            if (FindOrWarn(clickerName) is not { } clicker)
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
                _ => $"Clicker '{clicker.Name}' cannot be resumed ({Describe(clicker)}).",
            });
            return 1;
        });

        return command;
    }

    private Command BuildStop()
    {
        var name = new Argument<string?>("name")
        {
            Description = "Clicker to stop. Omit to stop all clickers.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var command = new Command("stop", "Stop and remove a clicker, or all clickers.") { name };
        command.Aliases.Add("stopautoclicker");
        command.Aliases.Add("picnic");

        command.SetAction(async (parseResult, _) =>
        {
            var clickerName = parseResult.GetValue(name);
            if (string.IsNullOrWhiteSpace(clickerName))
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

            if (!await _manager.StopAsync(clickerName).ConfigureAwait(false))
            {
                WarnNotFound(clickerName);
                return 1;
            }

            _console.WriteSuccess($"Clicker '{clickerName}' stopped.");
            return 0;
        });

        return command;
    }

    private Command BuildList()
    {
        var command = new Command("list", "List all clickers.");
        command.Aliases.Add("listall");
        command.Aliases.Add("listautoclickers");

        command.SetAction(_ =>
        {
            var clickers = _manager.Clickers;
            if (clickers.Count == 0)
            {
                _console.WriteInfo("No clickers. Start one with: start \"Process\" X,Y interval [name]");
                return 0;
            }

            _console.WriteInfo(FormatTable(clickers));
            return 0;
        });

        return command;
    }

    private Command BuildCoords()
    {
        var process = new Argument<string>("process") { Description = "Name of the process whose window to measure." };
        var delay = new Option<int>("--delay", "-d")
        {
            Description = "Seconds to wait before reading the cursor position.",
            DefaultValueFactory = _ => 3,
        };

        var command = new Command(
            "coords",
            "Show the cursor position relative to a window, to find the X,Y to use with 'start'.")
        {
            process,
            delay,
        };
        command.Aliases.Add("pick");

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var processName = ProcessWindowLocator.NormalizeProcessName(parseResult.GetRequiredValue(process));
            if (_windows.Find(processName) is not { } target)
            {
                _console.WriteError($"No running process named '{processName}' with a window was found.");
                return 1;
            }

            var seconds = Math.Clamp(parseResult.GetValue(delay), 0, 60);
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
        });

        return command;
    }

    private Command BuildHelp(RootCommand root)
    {
        var command = new Command("help", "Show the list of commands.");
        command.SetAction(_ => root.Parse(["--help"]).Invoke(new InvocationConfiguration { Output = _console.Out }));
        return command;
    }

    private Command BuildExit()
    {
        var command = new Command("exit", "Stop all clickers and quit.");
        command.Aliases.Add("quit");
        command.SetAction(_ =>
        {
            ExitRequested = true;
            return 0;
        });

        return command;
    }

    private static Argument<string> NameArgument() => new("name") { Description = "Name of the clicker (see 'list')." };

    private Clicker? FindOrWarn(string name)
    {
        var clicker = _manager.Find(name);
        if (clicker is null)
        {
            WarnNotFound(name);
        }

        return clicker;
    }

    private void WarnNotFound(string name) =>
        _console.WriteWarning($"No clicker named '{name}'. Use 'list' to see all clickers.");

    private static string Describe(Clicker clicker) =>
        clicker.State == ClickerState.Faulted && clicker.FaultReason is { } reason
            ? $"failed: {reason}"
            : clicker.State.ToString();

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
                    Describe(c),
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
