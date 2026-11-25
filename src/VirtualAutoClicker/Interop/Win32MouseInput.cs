namespace VirtualAutoClicker.Interop;

/// <inheritdoc />
internal sealed class Win32MouseInput : IMouseInput
{
    private const uint SendTimeoutMs = 1000;

    public bool LeftClick(nint window, ClientPoint position, ClickMode mode)
    {
        var lParam = position.ToLParam();

        return mode switch
        {
            ClickMode.Post =>
                NativeMethods.PostMessage(window, NativeMethods.WmLButtonDown, NativeMethods.MkLButton, lParam) &&
                NativeMethods.PostMessage(window, NativeMethods.WmLButtonUp, 0, lParam),

            ClickMode.Send =>
                Send(window, NativeMethods.WmLButtonDown, NativeMethods.MkLButton, lParam) &&
                Send(window, NativeMethods.WmLButtonUp, 0, lParam),

            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown click mode."),
        };
    }

    private static bool Send(nint window, uint message, nuint wParam, nint lParam) =>
        NativeMethods.SendMessageTimeout(
            window, message, wParam, lParam, NativeMethods.SmtoAbortIfHung, SendTimeoutMs, out _) != 0;
}
