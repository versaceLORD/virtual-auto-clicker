using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VirtualAutoClicker.Clicking;
using VirtualAutoClicker.Logging;

namespace VirtualAutoClicker.Cli;

/// <summary>Process arguments, made available through DI.</summary>
internal sealed record StartupArguments(IReadOnlyList<string> Args);

/// <summary>
/// Runs the startup command (if any) and then the interactive prompt. Stops the host when the
/// user exits, input ends, or Ctrl+C is pressed; all clickers are stopped on the way out.
/// </summary>
internal sealed class ReplHostedService(
    CliCommands cli,
    ClickerManager manager,
    IReplConsole console,
    StartupArguments startup,
    IHostApplicationLifetime lifetime,
    ILogger<ReplHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        try
        {
            if (startup.Args.Count > 0)
            {
                Environment.ExitCode = await cli.InvokeAsync(startup.Args, stoppingToken).ConfigureAwait(false);

                // One-shot mode: only stay alive if there is something to manage.
                if (cli.ExitRequested || !manager.HasClickers)
                {
                    return;
                }
            }

            WriteBanner();
            await RunReplAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Ctrl+C or host shutdown.
        }
        finally
        {
            var stopped = await manager.StopAllAsync().ConfigureAwait(false);
            if (stopped > 0)
            {
                console.WriteInfo($"Stopped {stopped} clicker(s).");
            }

            lifetime.StopApplication();
        }
    }

    private async Task RunReplAsync(CancellationToken stoppingToken)
    {
        while (!cli.ExitRequested)
        {
            console.WritePrompt();
            var line = await console.ReadLineAsync(stoppingToken).ConfigureAwait(false);
            if (line is null)
            {
                return; // End of input.
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                await cli.InvokeAsync(line, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
#pragma warning disable CA1031 // A failing command must never take down the REPL (and the running clickers).
            catch (Exception ex)
#pragma warning restore CA1031
            {
                Log.CommandFailed(logger, ex, line);
                console.WriteError($"Command failed: {ex.Message}");
            }
        }
    }

    private void WriteBanner()
    {
        console.WriteInfo("- - - - - - - - - - - - - - - - - -");
        console.WriteInfo($"{AppInfo.Name} - Your idle success [version {AppInfo.Version}]");
        console.WriteInfo($"by @versaceLORD - {AppInfo.RepositoryUrl}");
        console.WriteInfo("- - - - - - - - - - - - - - - - - -");
        console.WriteInfo("Type 'help' for the list of commands and 'exit' to quit.");
        console.WriteInfo(string.Empty);
    }
}
