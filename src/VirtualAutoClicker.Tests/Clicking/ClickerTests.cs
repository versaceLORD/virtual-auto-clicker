using Microsoft.Extensions.Logging.Abstractions;
using VirtualAutoClicker.Clicking;
using VirtualAutoClicker.Interop;
using VirtualAutoClicker.Tests.Fakes;

namespace VirtualAutoClicker.Tests.Clicking;

[TestClass]
public sealed class ClickerTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    [TestMethod]
    public async Task Start_ClicksImmediately_ThenOnEveryInterval()
    {
        await using var h = new TestHarness();
        await using var clicker = h.CreateClicker();

        Assert.IsTrue(clicker.Start());
        await h.Mouse.WaitForCountAsync(1);

        h.Time.Advance(Interval);
        await h.Mouse.WaitForCountAsync(2);
        h.Time.Advance(Interval);
        await h.Mouse.WaitForCountAsync(3);

        Assert.AreEqual(ClickerState.Running, clicker.State);
        Assert.AreEqual(3L, clicker.ClickCount);
        Assert.IsTrue(h.Mouse.Clicks.All(c => c.Window == 100 && c.Position == new ClientPoint(10, 20) && c.Mode == ClickMode.Post));
    }

    [TestMethod]
    public async Task Start_Twice_ReturnsFalse()
    {
        await using var h = new TestHarness();
        await using var clicker = h.CreateClicker();

        Assert.IsTrue(clicker.Start());
        Assert.IsFalse(clicker.Start());
    }

    [TestMethod]
    public async Task Pause_StopsClicking_ResumeContinues()
    {
        await using var h = new TestHarness();
        await using var clicker = h.CreateClicker();
        clicker.Start();
        await h.Mouse.WaitForCountAsync(1);

        Assert.IsTrue(await clicker.PauseAsync());
        Assert.AreEqual(ClickerState.Paused, clicker.State);

        h.Time.Advance(Interval * 10);
        await Task.Delay(50);
        Assert.AreEqual(1, h.Mouse.Count);

        Assert.IsTrue(clicker.Resume());
        await h.Mouse.WaitForCountAsync(2);
        Assert.AreEqual(ClickerState.Running, clicker.State);
    }

    [TestMethod]
    public async Task Pause_WhenNotRunning_ReturnsFalse()
    {
        await using var h = new TestHarness();
        await using var clicker = h.CreateClicker();

        Assert.IsFalse(await clicker.PauseAsync());

        clicker.Start();
        await clicker.PauseAsync();
        Assert.IsFalse(await clicker.PauseAsync());
    }

    [TestMethod]
    public async Task Resume_WhenRunning_ReturnsFalse()
    {
        await using var h = new TestHarness();
        await using var clicker = h.CreateClicker();
        clicker.Start();

        Assert.IsFalse(clicker.Resume());
    }

    [TestMethod]
    public async Task Stop_IsFinal()
    {
        await using var h = new TestHarness();
        var clicker = h.CreateClicker();
        clicker.Start();
        await h.Mouse.WaitForCountAsync(1);

        Assert.IsTrue(await clicker.StopAsync());
        Assert.AreEqual(ClickerState.Stopped, clicker.State);
        Assert.IsFalse(clicker.Resume());
        Assert.IsFalse(await clicker.StopAsync());

        h.Time.Advance(Interval * 5);
        await Task.Delay(50);
        Assert.AreEqual(1, h.Mouse.Count);
    }

    [TestMethod]
    public async Task Stop_FromPaused_Works()
    {
        await using var h = new TestHarness();
        var clicker = h.CreateClicker();
        clicker.Start();
        await clicker.PauseAsync();

        Assert.IsTrue(await clicker.StopAsync());
        Assert.AreEqual(ClickerState.Stopped, clicker.State);
    }

    [TestMethod]
    public async Task WindowGone_NoReplacement_Faults()
    {
        await using var h = new TestHarness();
        await using var clicker = h.CreateClicker();
        clicker.Start();
        await h.Mouse.WaitForCountAsync(1);

        h.Mouse.FailingWindows[100] = true;
        h.Windows.DeadWindows[100] = true;
        h.Windows.Processes.Clear();
        h.Time.Advance(Interval);

        await WaitForStateAsync(clicker, ClickerState.Faulted);
        Assert.IsNotNull(clicker.FaultReason);
        Assert.Contains("closed", clicker.FaultReason);
    }

    [TestMethod]
    public async Task WindowGone_ProcessRestarted_Reattaches()
    {
        await using var h = new TestHarness();
        await using var clicker = h.CreateClicker();
        clicker.Start();
        await h.Mouse.WaitForCountAsync(1);

        h.Mouse.FailingWindows[100] = true;
        h.Windows.DeadWindows[100] = true;
        h.Windows.Add("game", 200, processId: 999);
        h.Time.Advance(Interval);

        await h.Mouse.WaitForCountAsync(2);
        Assert.AreEqual(ClickerState.Running, clicker.State);
        Assert.AreEqual((nint)200, clicker.Target.Handle);
        Assert.AreEqual(999, clicker.Target.ProcessId);
    }

    [TestMethod]
    public async Task ClickFails_WindowStillAlive_KeepsRunning()
    {
        await using var h = new TestHarness();
        await using var clicker = h.CreateClicker();
        h.Mouse.FailingWindows[100] = true;

        clicker.Start();
        h.Time.Advance(Interval);
        h.Time.Advance(Interval);
        await Task.Delay(50);

        Assert.AreEqual(ClickerState.Running, clicker.State);
        Assert.AreEqual(0L, clicker.ClickCount);
    }

    [TestMethod]
    public async Task Faulted_ResumeAfterProcessReturns_Restarts()
    {
        await using var h = new TestHarness();
        await using var clicker = h.CreateClicker();
        clicker.Start();
        await h.Mouse.WaitForCountAsync(1);

        h.Mouse.FailingWindows[100] = true;
        h.Windows.DeadWindows[100] = true;
        h.Windows.Processes.Clear();
        h.Time.Advance(Interval);
        await WaitForStateAsync(clicker, ClickerState.Faulted);

        Assert.IsFalse(clicker.Resume());

        h.Windows.Add("game", 300);
        Assert.IsTrue(clicker.Resume());
        await h.Mouse.WaitForCountAsync(2);
        Assert.AreEqual((nint)300, clicker.Target.Handle);
        Assert.IsNull(clicker.FaultReason);
    }

    [TestMethod]
    public void Constructor_RejectsZeroInterval()
    {
        var options = new ClickerOptions("c", "game", new ClientPoint(1, 1), TimeSpan.Zero);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new Clicker(options, new WindowTarget(1, 1, "game"), new FakeMouseInput(), new FakeWindowLocator(), TimeProvider.System, NullLogger.Instance));
    }

    private static async Task WaitForStateAsync(Clicker clicker, ClickerState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (clicker.State != state)
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(5, CancellationToken.None);
        }
    }
}
