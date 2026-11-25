using System.CommandLine;

namespace VirtualAutoClicker.Cli.Commands;

/// <summary><c>help</c>: same output as <c>--help</c> on the root command.</summary>
internal sealed class HelpCommand : Command
{
    public HelpCommand(RootCommand root, IReplConsole console)
        : base("help", "Show the list of commands.")
    {
        SetAction(_ => root.Parse(["--help"]).Invoke(new InvocationConfiguration { Output = console.Out }));
    }
}
