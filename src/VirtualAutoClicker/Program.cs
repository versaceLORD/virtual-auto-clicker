using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VirtualAutoClicker;
using VirtualAutoClicker.Cli;
using VirtualAutoClicker.Clicking;
using VirtualAutoClicker.Interop;

if (!Console.IsOutputRedirected)
{
    Console.Title = $"Virtual Autoclicker [{AppInfo.Version}]";
}

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    // Command-line arguments are parsed by System.CommandLine, not by configuration.
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IMouseInput, Win32MouseInput>();
builder.Services.AddSingleton<IWindowLocator, ProcessWindowLocator>();
builder.Services.AddSingleton<IReplConsole, SystemConsole>();
builder.Services.AddSingleton<ClickerManager>();
builder.Services.AddSingleton<CliCommands>();
builder.Services.AddSingleton(new StartupArguments(args));
builder.Services.AddHostedService<ReplHostedService>();

using var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
