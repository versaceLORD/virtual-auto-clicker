using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using VirtualAutoClicker.Cli;
using VirtualAutoClicker.Clicking;
using VirtualAutoClicker.Interop;

namespace VirtualAutoClicker.Tests.Fakes;

internal sealed class FakeMouseInput : IMouseInput
{
    private readonly ConcurrentQueue<(nint Window, ClientPoint Position, ClickMode Mode)> _clicks = new();

    /// <summary>Windows for which clicks fail (as if the window was destroyed).</summary>
    public ConcurrentDictionary<nint, bool> FailingWindows { get; } = new();

    public IReadOnlyList<(nint Window, ClientPoint Position, ClickMode Mode)> Clicks => [.. _clicks];

    public int Count => _clicks.Count;

    public bool LeftClick(nint window, ClientPoint position, ClickMode mode)
    {
        if (FailingWindows.ContainsKey(window))
        {
            return false;
        }

        _clicks.Enqueue((window, position, mode));
        return true;
    }

    public async Task WaitForCountAsync(int expected)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (Count < expected)
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(5, CancellationToken.None);
        }
    }
}

internal sealed class FakeWindowLocator : IWindowLocator
{
    public ConcurrentDictionary<string, WindowTarget> Processes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentDictionary<nint, bool> DeadWindows { get; } = new();

    public (int X, int Y)? CursorPosition { get; set; }

    public WindowTarget Add(string processName, nint handle, int processId = 1234)
    {
        var target = new WindowTarget(handle, processId, processName);
        Processes[processName] = target;
        return target;
    }

    public WindowTarget? Find(string processName) =>
        Processes.TryGetValue(processName, out var target) ? target : null;

    public bool IsAlive(nint window) => window != 0 && !DeadWindows.ContainsKey(window);

    public bool TryGetCursorPosition(nint window, out int x, out int y)
    {
        (x, y) = CursorPosition ?? (0, 0);
        return CursorPosition is not null;
    }
}

internal sealed class FakeReplConsole : IReplConsole
{
    private readonly ConcurrentQueue<(string Level, string Message)> _lines = new();

    public TextWriter Out { get; } = new StringWriter();

    public TextWriter ErrorOut { get; } = new StringWriter();

    public IReadOnlyList<(string Level, string Message)> Lines => [.. _lines];

    public string AllText => string.Join(Environment.NewLine, _lines.Select(l => l.Message)) + Out + ErrorOut;

    public void WriteInfo(string message) => _lines.Enqueue(("info", message));

    public void WriteSuccess(string message) => _lines.Enqueue(("success", message));

    public void WriteWarning(string message) => _lines.Enqueue(("warning", message));

    public void WriteError(string message) => _lines.Enqueue(("error", message));

    public void WritePrompt()
    {
    }

    public Task<string?> ReadLineAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    public bool Has(string level, string fragment) =>
        _lines.Any(l => l.Level == level && l.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Wires the real clicker/CLI classes to fakes.</summary>
internal sealed class TestHarness : IAsyncDisposable
{
    public TestHarness()
    {
        Manager = new ClickerManager(Mouse, Windows, Time, NullLogger<Clicker>.Instance);
        Cli = new CliCommands(Manager, Windows, Console, Time);
    }

    public FakeMouseInput Mouse { get; } = new();

    public FakeWindowLocator Windows { get; } = new();

    public FakeTimeProvider Time { get; } = new();

    public FakeReplConsole Console { get; } = new();

    public ClickerManager Manager { get; }

    public CliCommands Cli { get; }

    public Clicker CreateClicker(string process = "game", nint handle = 100, int intervalMs = 100)
    {
        var target = Windows.Add(process, handle);
        var options = new ClickerOptions("c1", process, new ClientPoint(10, 20), TimeSpan.FromMilliseconds(intervalMs));
        return new Clicker(options, target, Mouse, Windows, Time, NullLogger<Clicker>.Instance);
    }

    public ValueTask DisposeAsync() => Manager.DisposeAsync();
}
