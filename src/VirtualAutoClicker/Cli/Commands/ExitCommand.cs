using System.CommandLine;

namespace VirtualAutoClicker.Cli.Commands;

/// <summary><c>exit</c>, also known as <c>quit</c>. The REPL stops once <see cref="Requested"/> is set.</summary>
internal sealed class ExitCommand : Command
{
    public ExitCommand()
        : base("exit", "Stop all clickers and quit.")
    {
        Aliases.Add("quit");
        SetAction(_ =>
        {
            Requested = true;
            return 0;
        });
    }

    public bool Requested { get; private set; }
}
