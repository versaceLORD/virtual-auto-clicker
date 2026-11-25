using VirtualAutoClicker.Clicking;
using VirtualAutoClicker.Interop;
using VirtualAutoClicker.Tests.Fakes;

namespace VirtualAutoClicker.Tests.Clicking;

[TestClass]
public sealed class ClickerManagerTests
{
    private static ClickerOptions Options(string name, string process = "game") =>
        new(name, process, new ClientPoint(1, 1), TimeSpan.FromMilliseconds(100));

    [TestMethod]
    public async Task TryStart_StartsAndTracksClicker()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);

        var result = h.Manager.TryStart(Options("a"), out var clicker);

        Assert.AreEqual(StartClickerResult.Started, result);
        Assert.IsNotNull(clicker);
        Assert.AreEqual(ClickerState.Running, clicker.State);
        Assert.AreSame(clicker, h.Manager.Find("A"));
        Assert.HasCount(1, h.Manager.Clickers);
    }

    [TestMethod]
    public async Task TryStart_DuplicateName_IsRejected_AndOriginalKeepsRunning()
    {
        // Regression: in 1.x a duplicate name removed the running clicker from the list without stopping it.
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);
        h.Manager.TryStart(Options("a"), out var original);

        var result = h.Manager.TryStart(Options("A"), out var duplicate);

        Assert.AreEqual(StartClickerResult.DuplicateName, result);
        Assert.IsNull(duplicate);
        Assert.AreSame(original, h.Manager.Find("a"));
        Assert.AreEqual(ClickerState.Running, original!.State);
    }

    [TestMethod]
    public async Task TryStart_UnknownProcess_IsRejected()
    {
        await using var h = new TestHarness();

        var result = h.Manager.TryStart(Options("a", "missing"), out var clicker);

        Assert.AreEqual(StartClickerResult.ProcessNotFound, result);
        Assert.IsNull(clicker);
        Assert.IsFalse(h.Manager.HasClickers);
    }

    [TestMethod]
    public async Task NextName_SkipsNamesInUse()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);
        h.Manager.TryStart(Options("clicker-1"), out _);

        Assert.AreEqual("clicker-2", h.Manager.NextName());
        Assert.AreEqual("clicker-3", h.Manager.NextName());
    }

    [TestMethod]
    public async Task StopAsync_StopsAndRemoves()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);
        h.Manager.TryStart(Options("a"), out var clicker);

        Assert.IsTrue(await h.Manager.StopAsync("a"));
        Assert.IsFalse(await h.Manager.StopAsync("a"));
        Assert.AreEqual(ClickerState.Stopped, clicker!.State);
        Assert.IsNull(h.Manager.Find("a"));
    }

    [TestMethod]
    public async Task DisposeAsync_StopsAllClickers()
    {
        var h = new TestHarness();
        h.Windows.Add("game", 100);
        h.Manager.TryStart(Options("a"), out var a);
        h.Manager.TryStart(Options("b"), out var b);

        await h.DisposeAsync();

        Assert.AreEqual(ClickerState.Stopped, a!.State);
        Assert.AreEqual(ClickerState.Stopped, b!.State);
        Assert.IsFalse(h.Manager.HasClickers);
    }

    [TestMethod]
    public async Task StopAllAsync_ReturnsCount()
    {
        await using var h = new TestHarness();
        h.Windows.Add("game", 100);
        h.Manager.TryStart(Options("a"), out _);
        h.Manager.TryStart(Options("b"), out _);

        Assert.AreEqual(2, await h.Manager.StopAllAsync());
        Assert.AreEqual(0, await h.Manager.StopAllAsync());
    }
}
