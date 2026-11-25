using System.Diagnostics;

namespace VirtualAutoClicker.Interop;

/// <inheritdoc />
internal sealed class ProcessWindowLocator : IWindowLocator
{
    public WindowTarget? Find(string processName)
    {
        var name = NormalizeProcessName(processName);
        if (name.Length == 0)
        {
            return null;
        }

        var processes = Process.GetProcessesByName(name);
        try
        {
            foreach (var process in processes)
            {
                try
                {
                    var handle = process.MainWindowHandle;
                    if (handle != 0)
                    {
                        return new WindowTarget(handle, process.Id, process.ProcessName);
                    }
                }
                catch (InvalidOperationException)
                {
                    // The process exited while we were looking at it.
                }
            }

            return null;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    public bool IsAlive(nint window) => window != 0 && NativeMethods.IsWindow(window);

    public bool TryGetCursorPosition(nint window, out int x, out int y)
    {
        x = y = 0;
        if (!NativeMethods.GetCursorPos(out var point) || !NativeMethods.ScreenToClient(window, ref point))
        {
            return false;
        }

        (x, y) = (point.X, point.Y);
        return true;
    }

    internal static string NormalizeProcessName(string processName)
    {
        var name = processName.Trim().Trim('"').Trim();
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }
}
