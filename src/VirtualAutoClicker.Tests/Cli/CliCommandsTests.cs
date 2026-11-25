using VirtualAutoClicker.Clicking;
using VirtualAutoClicker.Interop;
using VirtualAutoClicker.Tests.Fakes;

namespace VirtualAutoClicker.Tests.Cli;

[TestClass]
public sealed class CliCommandsTests
{
    [TestMethod]
    public async Task Start_QuotedProcessNameWithSpaces()
    {
        await using var h = new TestHarness();
        h.Windows.Add("My Game", 100);

        var exitCode = await h.Cli.InvokeAsync("start \"My Game\" 450,451 50 farm");

        Assert.AreEqual(0, exitCode);
        var clicker = h.Manager.Find("farm");
        Assert.IsNotNull(clicker);
        Assert.AreEqual("My Game", clicker.Options.ProcessName);
        Assert.AreEqual(new ClientPoint(450, 451), clicker.Options.Position);
        Assert.AreEqual(TimeSpan.FromMilliseconds(50), clicker.Options.Interval);
        Assert.AreEqual(ClickMode.Post, clicker.Options.Mode);
    }

    [TestMethod]
    public async Task Start_LegacyCommandName_IsCaseInsensitive()
    {
        await using var h = new TestHarness();
        h.Windows.Add("Firestone", 100);

        var exitCode = await h.Cli.InvokeAsync("StartAutoClicker \"Firestone\" 450,450 50");

        Assert.AreEqual(0, exitCode);
        Assert.IsNotNull(h.Manager.Find("clicker-1"));
    }

    [TestMethod]
    public async Task Start_WithExeSuffixAndSendMode()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);

        var exitCode = await h.Cli.InvokeAsync("start game.exe 1,1 100 a --mode send");

        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(ClickMode.Send, h.Manager.Find("a")!.Options.Mode);
    }

    [TestMethod]
    [DataRow("start game 450 50", "Invalid coordinates")]
    [DataRow("start game 1,2,3 50", "Invalid coordinates")]
    [DataRow("start game 1,1 0", "Invalid interval")]
    [DataRow("start game 1,1 -5", "Invalid interval")]
    [DataRow("start game 1,1 abc", "Invalid interval")]
    public async Task Start_InvalidInput_ReportsError(string commandLine, string expected)
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);

        var exitCode = await h.Cli.InvokeAsync(commandLine);

        Assert.AreEqual(1, exitCode);
        Assert.IsTrue(h.Console.Has("error", expected), h.Console.AllText);
        Assert.IsFalse(h.Manager.HasClickers);
    }

    [TestMethod]
    public async Task Start_LowInterval_Warns()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);

        await h.Cli.InvokeAsync("start game 1,1 5");

        Assert.IsTrue(h.Console.Has("warning", "unresponsive"));
    }

    [TestMethod]
    public async Task Start_DuplicateName_ReportsError()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);
        await h.Cli.InvokeAsync("start game 1,1 100 a");

        var exitCode = await h.Cli.InvokeAsync("start game 1,1 100 a");

        Assert.AreEqual(1, exitCode);
        Assert.IsTrue(h.Console.Has("error", "already exists"));
        Assert.AreEqual(ClickerState.Running, h.Manager.Find("a")!.State);
    }

    [TestMethod]
    public async Task Start_UnknownProcess_ReportsError()
    {
        await using var h = new TestHarness();

        var exitCode = await h.Cli.InvokeAsync("start nope 1,1 100");

        Assert.AreEqual(1, exitCode);
        Assert.IsTrue(h.Console.Has("error", "No running process named 'nope'"));
    }

    [TestMethod]
    [DataRow("3")]
    [DataRow("list,start")]
    [DataRow("bogus")]
    public async Task UnknownCommand_IsRejected(string commandLine)
    {
        // Regression: 1.x resolved numeric and comma separated input through Enum.TryParse.
        await using var h = new TestHarness();

        var exitCode = await h.Cli.InvokeAsync(commandLine);

        Assert.AreEqual(1, exitCode);
        Assert.IsTrue(h.Console.Has("info", "help"));
    }

    [TestMethod]
    public async Task PauseResume_UnknownClicker_Warns()
    {
        await using var h = new TestHarness();

        Assert.AreEqual(1, await h.Cli.InvokeAsync("pause ghost"));
        Assert.AreEqual(1, await h.Cli.InvokeAsync("resume ghost"));
        Assert.AreEqual(2, h.Console.Lines.Count(l => l.Level == "warning" && l.Message.Contains("No clicker named 'ghost'", StringComparison.Ordinal)));
        Assert.IsFalse(h.Console.Has("success", "ghost"));
    }

    [TestMethod]
    public async Task PauseResume_Roundtrip()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);
        await h.Cli.InvokeAsync("start game 1,1 100 a");

        Assert.AreEqual(0, await h.Cli.InvokeAsync("pause a"));
        Assert.AreEqual(ClickerState.Paused, h.Manager.Find("a")!.State);
        Assert.AreEqual(1, await h.Cli.InvokeAsync("pause a"));

        Assert.AreEqual(0, await h.Cli.InvokeAsync("resume a"));
        Assert.AreEqual(ClickerState.Running, h.Manager.Find("a")!.State);
        Assert.AreEqual(1, await h.Cli.InvokeAsync("resume a"));
        Assert.IsTrue(h.Console.Has("warning", "already running"));
    }

    [TestMethod]
    [DataRow("stop")]
    [DataRow("picnic")]
    [DataRow("STOPAUTOCLICKER")]
    public async Task Stop_WithoutName_StopsAll(string command)
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);
        await h.Cli.InvokeAsync("start game 1,1 100 a");
        await h.Cli.InvokeAsync("start game 1,1 100 b");

        Assert.AreEqual(0, await h.Cli.InvokeAsync(command));
        Assert.IsFalse(h.Manager.HasClickers);
        Assert.IsTrue(h.Console.Has("success", "Stopped 2"));
    }

    [TestMethod]
    public async Task Stop_WithName_StopsOnlyThatClicker()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);
        await h.Cli.InvokeAsync("start game 1,1 100 a");
        await h.Cli.InvokeAsync("start game 1,1 100 b");

        Assert.AreEqual(0, await h.Cli.InvokeAsync("stop a"));
        Assert.IsNull(h.Manager.Find("a"));
        Assert.IsNotNull(h.Manager.Find("b"));
    }

    [TestMethod]
    public async Task List_ShowsTable()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100, processId: 4242);
        await h.Cli.InvokeAsync("start game 7,8 250 farm");

        await h.Cli.InvokeAsync("list");

        var table = h.Console.Lines[^1].Message;
        Assert.Contains("farm", table);
        Assert.Contains("Running", table);
        Assert.Contains("game (4242)", table);
        Assert.Contains("7,8", table);
        Assert.Contains("250 ms", table);
    }

    [TestMethod]
    public async Task List_Empty()
    {
        await using var h = new TestHarness();

        await h.Cli.InvokeAsync("list");

        Assert.IsTrue(h.Console.Has("info", "No clickers"));
    }

    [TestMethod]
    public async Task Coords_ReportsCursorPosition()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);
        h.Windows.CursorPosition = (12, 34);

        var exitCode = await h.Cli.InvokeAsync("coords game --delay 0");

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(h.Console.Has("success", "12,34"));
    }

    [TestMethod]
    public async Task Exit_SetsExitRequested()
    {
        await using var h = new TestHarness();

        Assert.IsFalse(h.Cli.ExitRequested);
        await h.Cli.InvokeAsync("quit");
        Assert.IsTrue(h.Cli.ExitRequested);
    }

    [TestMethod]
    public async Task Help_WritesCommandList()
    {
        await using var h = new TestHarness();

        Assert.AreEqual(0, await h.Cli.InvokeAsync("help"));
        Assert.Contains("startautoclicker", h.Console.Out.ToString()!);
    }
}
