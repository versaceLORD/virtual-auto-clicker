using System.CommandLine;
using System.Globalization;
using VirtualAutoClicker.Clicking;
using VirtualAutoClicker.Interop;

namespace VirtualAutoClicker.Cli.Commands;

/// <summary><c>start "Process" X,Y interval [name] [--mode post|send]</c></summary>
internal sealed class StartCommand : Command
{
    private readonly ClickerManager _manager;
    private readonly IReplConsole _console;

    private readonly Argument<string> _process = new("process")
    {
        Description = "Name of the process to click in, e.g. \"Firestone\". Quote names with spaces.",
    };

    private readonly Argument<ClientPoint> _position = new("position")
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

    private readonly Argument<int> _interval = new("interval")
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

    private readonly Argument<string?> _name = new("name")
    {
        Description = "Optional name for the clicker, used with pause/resume/stop. Defaults to clicker-N.",
        Arity = ArgumentArity.ZeroOrOne,
    };

    private readonly Option<ClickMode> _mode = new("--mode", "-m")
    {
        Description = "How clicks are delivered: post (default, never blocks) or send (synchronous, 1 s timeout).",
        DefaultValueFactory = _ => ClickMode.Post,
    };

    public StartCommand(ClickerManager manager, IReplConsole console)
        : base("start", "Start a new auto clicker.")
    {
        _manager = manager;
        _console = console;

        Arguments.Add(_process);
        Arguments.Add(_position);
        Arguments.Add(_interval);
        Arguments.Add(_name);
        Options.Add(_mode);
        Aliases.Add("startautoclicker");

        SetAction(Execute);
    }

    private int Execute(ParseResult parseResult)
    {
        var processName = ProcessWindowLocator.NormalizeProcessName(parseResult.GetRequiredValue(_process));
        if (processName.Length == 0)
        {
            _console.WriteError("The process name cannot be empty.");
            return 1;
        }

        var clickerName = parseResult.GetValue(_name);
        if (string.IsNullOrWhiteSpace(clickerName))
        {
            clickerName = _manager.NextName();
        }

        var options = new ClickerOptions(
            clickerName.Trim(),
            processName,
            parseResult.GetRequiredValue(_position),
            TimeSpan.FromMilliseconds(parseResult.GetRequiredValue(_interval)),
            parseResult.GetValue(_mode));

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
    }
}
