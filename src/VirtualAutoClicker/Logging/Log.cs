using Microsoft.Extensions.Logging;
using VirtualAutoClicker.Interop;

namespace VirtualAutoClicker.Logging;

/// <summary>
/// Source-generated log messages.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Clicker '{Name}' started on {ProcessName} (pid {ProcessId}) at {Position} every {IntervalMs} ms")]
    public static partial void ClickerStarted(ILogger logger, string name, string processName, int processId, ClientPoint position, double intervalMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Clicker '{Name}' {State}")]
    public static partial void ClickerStateChanged(ILogger logger, string name, string state);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Clicker '{Name}': window of {ProcessName} is gone, re-attached to pid {ProcessId}")]
    public static partial void ClickerReattached(ILogger logger, string name, string processName, int processId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Clicker '{Name}': the window of {ProcessName} is gone, clicker stopped")]
    public static partial void ClickerWindowLost(ILogger logger, string name, string processName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Clicker '{Name}' failed")]
    public static partial void ClickerFailed(ILogger logger, Exception exception, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Command '{CommandLine}' failed")]
    public static partial void CommandFailed(ILogger logger, Exception exception, string commandLine);
}
