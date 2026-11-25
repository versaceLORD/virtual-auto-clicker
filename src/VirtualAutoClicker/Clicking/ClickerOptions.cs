using VirtualAutoClicker.Interop;

namespace VirtualAutoClicker.Clicking;

/// <summary>
/// Immutable configuration of a single auto clicker.
/// </summary>
internal sealed record ClickerOptions(
    string Name,
    string ProcessName,
    ClientPoint Position,
    TimeSpan Interval,
    ClickMode Mode = ClickMode.Post)
{
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromMilliseconds(1);

    /// <summary>Intervals below this are allowed but flood the target with messages.</summary>
    public static readonly TimeSpan LowIntervalWarningThreshold = TimeSpan.FromMilliseconds(10);
}

/// <summary>
/// Lifecycle state of a clicker.
/// </summary>
internal enum ClickerState
{
    /// <summary>Created but never started.</summary>
    Created,
    Running,
    Paused,
    Stopped,

    /// <summary>Stopped because of an error, for example the target window was closed.</summary>
    Faulted,
}
