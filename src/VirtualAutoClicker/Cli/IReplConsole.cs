namespace VirtualAutoClicker.Cli;

/// <summary>
/// Console I/O used by the REPL and the commands. Abstracted so commands can be tested.
/// </summary>
internal interface IReplConsole
{
    /// <summary>Writer for regular output (also used by System.CommandLine for help).</summary>
    TextWriter Out { get; }

    /// <summary>Writer for error output (also used by System.CommandLine for parse errors).</summary>
    TextWriter ErrorOut { get; }

    void WriteInfo(string message);

    void WriteSuccess(string message);

    void WriteWarning(string message);

    void WriteError(string message);

    void WritePrompt();

    /// <summary>Reads a line, or returns <see langword="null"/> when input has ended.</summary>
    Task<string?> ReadLineAsync(CancellationToken cancellationToken);
}
