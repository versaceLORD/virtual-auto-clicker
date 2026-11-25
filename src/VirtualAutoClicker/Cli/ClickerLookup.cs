using System.CommandLine;
using VirtualAutoClicker.Clicking;

namespace VirtualAutoClicker.Cli;

/// <summary>CLI helpers shared by the commands that act on an existing clicker by name.</summary>
internal static class ClickerLookup
{
    public static Argument<string> NameArgument() => new("name") { Description = "Name of the clicker (see 'list')." };

    public static Clicker? FindOrWarn(ClickerManager manager, IReplConsole console, string name)
    {
        var clicker = manager.Find(name);
        if (clicker is null)
        {
            WarnNotFound(console, name);
        }

        return clicker;
    }

    public static void WarnNotFound(IReplConsole console, string name) =>
        console.WriteWarning($"No clicker named '{name}'. Use 'list' to see all clickers.");
}
