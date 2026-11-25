namespace VirtualAutoClicker.Cli;

/// <summary>
/// <see cref="IReplConsole"/> on top of <see cref="Console"/>. Coloured writes are serialized so
/// output from different threads never mixes colours.
/// </summary>
internal sealed class SystemConsole : IReplConsole
{
    public const string Prompt = "VAC >> ";

    private readonly Lock _gate = new();

    public TextWriter Out => Console.Out;

    public TextWriter ErrorOut => Console.Error;

    public void WriteInfo(string message) => Write(message, color: null);

    public void WriteSuccess(string message) => Write(message, ConsoleColor.Green);

    public void WriteWarning(string message) => Write(message, ConsoleColor.Yellow);

    public void WriteError(string message) => Write(message, ConsoleColor.Red);

    public void WritePrompt()
    {
        lock (_gate)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(Prompt);
            Console.ResetColor();
        }
    }

    public Task<string?> ReadLineAsync(CancellationToken cancellationToken) =>
        // Console.ReadLine cannot be cancelled; run it on the pool and stop waiting on cancellation.
        Task.Run(Console.ReadLine, CancellationToken.None).WaitAsync(cancellationToken);

    private void Write(string message, ConsoleColor? color)
    {
        lock (_gate)
        {
            if (color is { } c)
            {
                Console.ForegroundColor = c;
            }

            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
