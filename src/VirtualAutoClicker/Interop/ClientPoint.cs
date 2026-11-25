using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace VirtualAutoClicker.Interop;

/// <summary>
/// A point in a window's client area (0,0 is the top-left corner of the client area).
/// </summary>
internal readonly record struct ClientPoint(int X, int Y)
{
    /// <summary>
    /// Highest coordinate that survives the signed 16-bit <c>GET_X_LPARAM</c>/<c>GET_Y_LPARAM</c> round trip.
    /// </summary>
    public const int MaxCoordinate = short.MaxValue;

    /// <summary>
    /// Packs the point the same way as the Win32 <c>MAKELPARAM(x, y)</c> macro.
    /// </summary>
    public nint ToLParam() => (nint)(uint)(((Y & 0xFFFF) << 16) | (X & 0xFFFF));

    /// <summary>
    /// Parses <c>"X,Y"</c>. Both values must be integers in <c>0..32767</c>.
    /// </summary>
    public static bool TryParse(string? text, [NotNullWhen(false)] out string? error, out ClientPoint point)
    {
        point = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Coordinates are required, expected the format X,Y (for example 450,450).";
            return false;
        }

        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            error = $"Invalid coordinates '{text}', expected the format X,Y (for example 450,450).";
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
        {
            error = $"Invalid coordinates '{text}', X and Y must be whole numbers.";
            return false;
        }

        if (x is < 0 or > MaxCoordinate || y is < 0 or > MaxCoordinate)
        {
            error = $"Invalid coordinates '{text}', X and Y must be between 0 and {MaxCoordinate}.";
            return false;
        }

        point = new ClientPoint(x, y);
        error = null;
        return true;
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{X},{Y}");
}
