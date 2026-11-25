namespace VirtualAutoClicker.Interop;

/// <summary>
/// The top-level window of a process that clicks are sent to.
/// </summary>
internal readonly record struct WindowTarget(nint Handle, int ProcessId, string ProcessName);

/// <summary>
/// Finds and inspects target windows.
/// </summary>
internal interface IWindowLocator
{
    /// <summary>
    /// Finds the main window of the first process named <paramref name="processName"/> that has one.
    /// A trailing <c>.exe</c> is ignored.
    /// </summary>
    WindowTarget? Find(string processName);

    /// <summary>Whether <paramref name="window"/> still refers to an existing window.</summary>
    bool IsAlive(nint window);

    /// <summary>Gets the current cursor position relative to <paramref name="window"/>'s client area.</summary>
    bool TryGetCursorPosition(nint window, out int x, out int y);
}
