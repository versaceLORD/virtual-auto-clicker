using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using VirtualAutoClicker.Interop;

namespace VirtualAutoClicker.Clicking;

internal enum StartClickerResult
{
    Started,
    DuplicateName,
    ProcessNotFound,
}

/// <summary>
/// Owns all clickers. Thread-safe. Disposing the manager stops every clicker.
/// </summary>
internal sealed class ClickerManager(
    IMouseInput mouse,
    IWindowLocator windows,
    TimeProvider time,
    ILogger<Clicker> clickerLogger) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Clicker> _clickers = new(StringComparer.OrdinalIgnoreCase);
    private int _nameCounter;

    /// <summary>All clickers, ordered by name.</summary>
    public IReadOnlyList<Clicker> Clickers =>
        [.. _clickers.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];

    public bool HasClickers => !_clickers.IsEmpty;

    /// <summary>Generates an unused name of the form <c>clicker-N</c>.</summary>
    public string NextName()
    {
        string name;
        do
        {
            name = string.Create(CultureInfo.InvariantCulture, $"clicker-{Interlocked.Increment(ref _nameCounter)}");
        }
        while (_clickers.ContainsKey(name));

        return name;
    }

    public StartClickerResult TryStart(ClickerOptions options, out Clicker? clicker)
    {
        ArgumentNullException.ThrowIfNull(options);
        clicker = null;

        if (_clickers.ContainsKey(options.Name))
        {
            return StartClickerResult.DuplicateName;
        }

        if (windows.Find(options.ProcessName) is not { } target)
        {
            return StartClickerResult.ProcessNotFound;
        }

        var candidate = new Clicker(options, target, mouse, windows, time, clickerLogger);
        if (!_clickers.TryAdd(options.Name, candidate))
        {
            return StartClickerResult.DuplicateName;
        }

        candidate.Start();
        clicker = candidate;
        return StartClickerResult.Started;
    }

    public Clicker? Find(string name) => _clickers.GetValueOrDefault(name);

    /// <summary>Stops and removes the clicker named <paramref name="name"/>.</summary>
    public async Task<bool> StopAsync(string name)
    {
        if (!_clickers.TryRemove(name, out var clicker))
        {
            return false;
        }

        await clicker.StopAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>Stops and removes every clicker.</summary>
    /// <returns>The number of clickers stopped.</returns>
    public async Task<int> StopAllAsync()
    {
        var stopped = 0;
        foreach (var name in _clickers.Keys)
        {
            if (await StopAsync(name).ConfigureAwait(false))
            {
                stopped++;
            }
        }

        return stopped;
    }

    public async ValueTask DisposeAsync() => await StopAllAsync().ConfigureAwait(false);
}
