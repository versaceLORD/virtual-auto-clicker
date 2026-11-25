using System.Reflection;

namespace VirtualAutoClicker;

internal static class AppInfo
{
    public const string Name = "virtual-auto-clicker";
    public const string RepositoryUrl = "https://github.com/versaceLORD/virtual-auto-clicker";

    /// <summary>Product version without the source revision suffix (e.g. <c>2.0.0</c>).</summary>
    public static string Version { get; } = GetVersion();

    private static string GetVersion()
    {
        var informational = typeof(AppInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.0.0";

        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? informational : informational[..plus];
    }
}
