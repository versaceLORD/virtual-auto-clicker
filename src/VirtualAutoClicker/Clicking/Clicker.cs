using Microsoft.Extensions.Logging;
using VirtualAutoClicker.Interop;
using VirtualAutoClicker.Logging;

namespace VirtualAutoClicker.Clicking;

/// <summary>
/// A single auto clicker: clicks <see cref="ClickerOptions.Position"/> in the target window
/// every <see cref="ClickerOptions.Interval"/> until paused or stopped.
/// </summary>
/// <remarks>
/// All state transitions happen under <see cref="_gate"/>. The click loop runs on the thread pool,
/// is driven by a <see cref="PeriodicTimer"/> on the injected <see cref="TimeProvider"/>, and is
/// always awaited when paused or stopped so no exception goes unobserved.
/// </remarks>
internal sealed class Clicker : IAsyncDisposable
{
    private readonly Lock _gate = new();
    private readonly IMouseInput _mouse;
    private readonly IWindowLocator _windows;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    private WindowTarget _target;
    private ClickerState _state = ClickerState.Created;
    private string? _faultReason;
    private CancellationTokenSource? _cts;
    private Task _loop = Task.CompletedTask;
    private long _clickCount;

    public Clicker(
        ClickerOptions options,
        WindowTarget target,
        IMouseInput mouse,
        IWindowLocator windows,
        TimeProvider time,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.Interval, ClickerOptions.MinimumInterval);

        Options = options;
        _target = target;
        _mouse = mouse;
        _windows = windows;
        _time = time;
        _logger = logger;
    }

    public ClickerOptions Options { get; }

    public string Name => Options.Name;

    public WindowTarget Target
    {
        get
        {
            lock (_gate)
            {
                return _target;
            }
        }
    }

    public ClickerState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    /// <summary>Why the clicker faulted, when <see cref="State"/> is <see cref="ClickerState.Faulted"/>.</summary>
    public string? FaultReason
    {
        get
        {
            lock (_gate)
            {
                return _faultReason;
            }
        }
    }

    /// <summary>
    /// The state for display, e.g. <c>Running</c> or <c>failed: window closed</c>.
    /// State and fault reason are read together so they always match.
    /// </summary>
    public string StatusText
    {
        get
        {
            lock (_gate)
            {
                return _state == ClickerState.Faulted && _faultReason is { } reason
                    ? $"failed: {reason}"
                    : _state.ToString();
            }
        }
    }

    public long ClickCount => Interlocked.Read(ref _clickCount);

    /// <summary>Starts a newly created clicker.</summary>
    public bool Start()
    {
        lock (_gate)
        {
            if (_state != ClickerState.Created)
            {
                return false;
            }

            StartLoopLocked();
        }

        var target = Target;
        Log.ClickerStarted(_logger, Name, target.ProcessName, target.ProcessId, Options.Position, Options.Interval.TotalMilliseconds);
        return true;
    }

    /// <summary>
    /// Resumes a paused clicker, or restarts a faulted one if its process has a window again.
    /// </summary>
    public bool Resume()
    {
        var replacement = State == ClickerState.Faulted ? _windows.Find(Options.ProcessName) : null;

        lock (_gate)
        {
            switch (_state)
            {
                case ClickerState.Paused:
                    break;
                case ClickerState.Faulted when replacement is { } target:
                    _target = target;
                    _faultReason = null;
                    break;
                default:
                    return false;
            }

            StartLoopLocked();
        }

        Log.ClickerStateChanged(_logger, Name, "resumed");
        return true;
    }

    /// <summary>Pauses a running clicker and waits for the click loop to finish.</summary>
    public Task<bool> PauseAsync() => HaltAsync(ClickerState.Paused);

    /// <summary>Stops the clicker for good and waits for the click loop to finish.</summary>
    public Task<bool> StopAsync() => HaltAsync(ClickerState.Stopped);

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private void StartLoopLocked()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _state = ClickerState.Running;
        _loop = Task.Run(() => RunAsync(token), CancellationToken.None);
    }

    private async Task<bool> HaltAsync(ClickerState newState)
    {
        CancellationTokenSource? cts;
        Task loop;

        lock (_gate)
        {
            if (_state == ClickerState.Running)
            {
                cts = _cts;
                loop = _loop;
                _cts = null;
            }
            else if (newState == ClickerState.Stopped && _state != ClickerState.Stopped)
            {
                // Created, Paused or Faulted: nothing is running, just mark it stopped.
                cts = null;
                loop = Task.CompletedTask;
            }
            else
            {
                return false;
            }

            _state = newState;
        }

        if (cts is not null)
        {
            await cts.CancelAsync().ConfigureAwait(false);
            await loop.ConfigureAwait(false);
            cts.Dispose();
        }

        Log.ClickerStateChanged(_logger, Name, newState == ClickerState.Paused ? "paused" : "stopped");
        return true;
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(Options.Interval, _time);
            do
            {
                if (!ClickOnce())
                {
                    Log.ClickerWindowLost(_logger, Name, Options.ProcessName);
                    Fault($"The window of '{Options.ProcessName}' was closed.", token);
                    return;
                }
            }
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Paused or stopped.
        }
#pragma warning disable CA1031 // The loop must never crash the process; the failure is surfaced through State/FaultReason.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.ClickerFailed(_logger, ex, Name);
            Fault(ex.Message, token);
        }
    }

    /// <summary>
    /// Sends one click. If the window is gone, tries to re-attach to a new window of the same process.
    /// </summary>
    /// <returns><see langword="false"/> if there is no window to click any more.</returns>
    private bool ClickOnce()
    {
        var target = Target;
        if (_mouse.LeftClick(target.Handle, Options.Position, Options.Mode))
        {
            Interlocked.Increment(ref _clickCount);
            return true;
        }

        if (_windows.IsAlive(target.Handle))
        {
            // Transient failure (for example a full message queue or a hung window); try again next tick.
            return true;
        }

        if (_windows.Find(Options.ProcessName) is not { } replacement)
        {
            return false;
        }

        lock (_gate)
        {
            _target = replacement;
        }

        Log.ClickerReattached(_logger, Name, replacement.ProcessName, replacement.ProcessId);

        if (_mouse.LeftClick(replacement.Handle, Options.Position, Options.Mode))
        {
            Interlocked.Increment(ref _clickCount);
        }

        return true;
    }

    private void Fault(string reason, CancellationToken token)
    {
        CancellationTokenSource? cts = null;

        lock (_gate)
        {
            // Only fault if nobody paused/stopped us in the meantime.
            if (_state != ClickerState.Running || token.IsCancellationRequested)
            {
                return;
            }

            _state = ClickerState.Faulted;
            _faultReason = reason;
            cts = _cts;
            _cts = null;
        }

        cts?.Dispose();
    }
}
