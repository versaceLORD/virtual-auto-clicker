namespace VirtualAutoClicker.Interop;

/// <summary>
/// How the click messages are delivered to the target window.
/// </summary>
internal enum ClickMode
{
    /// <summary>Queue the messages with <c>PostMessage</c> (never blocks on a hung window).</summary>
    Post,

    /// <summary>Deliver the messages synchronously with <c>SendMessageTimeout</c>.</summary>
    Send,
}

/// <summary>
/// Sends background mouse clicks to a window without moving the real cursor.
/// </summary>
internal interface IMouseInput
{
    /// <summary>
    /// Sends a left button down/up pair to <paramref name="window"/> at <paramref name="position"/>.
    /// </summary>
    /// <returns><see langword="false"/> if the messages could not be delivered (for example the window is gone).</returns>
    bool LeftClick(nint window, ClientPoint position, ClickMode mode);
}
