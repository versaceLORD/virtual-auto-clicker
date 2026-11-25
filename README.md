## virtual-auto-clicker - Your idle success

[![CI](https://github.com/versaceLORD/virtual-auto-clicker/actions/workflows/ci.yml/badge.svg)](https://github.com/versaceLORD/virtual-auto-clicker/actions/workflows/ci.yml)

### Reason for repo's existence

There is a bunch of resources out there related to autoclickers. But on the windows platform I failed to find a good alternative for an autoclicker that allowed you to still be in control of your mouse.

This simple yet efficient console application fulfills your needs for a quick and effective clicker while still letting you use your PC freely! Clicks are sent straight to the target window in the background; your real cursor never moves.

### Requirements

* Windows 10 or Windows 11, x64 or ARM64.
* Nothing else: the download is a single native executable, no .NET runtime needed.

### How to download

* In the [releases](https://github.com/versaceLORD/virtual-auto-clicker/releases) tab you will find a zip for `win-x64` and `win-arm64` for each version.
* Each zip has a `.sha256` checksum and a GitHub build provenance attestation (`gh attestation verify <zip> -R versaceLORD/virtual-auto-clicker`).

### How to use

Start `VirtualAutoClicker.exe`. You get a `VAC >>` prompt; type a command and press Enter.

```text
VAC >> start "Firestone" 450,450 50
Clicker 'clicker-1' started on Firestone (pid 1234) at 450,450 every 50 ms.
```

`start "P" X,Y I [N]`

* P = Name of the Windows process to click in (as shown in Task Manager's *Details* tab, `.exe` is optional). Quote names that contain spaces.
* X,Y = Coordinates relative to the top-left corner of the window's client area (0,0). Use `coords` to find them.
* I = Interval in milliseconds (at least 1).
* N = Optional name of the clicker, useful when running several clickers. Defaults to `clicker-1`, `clicker-2`, ...
* `--mode send` delivers clicks synchronously instead of posting them; try it if a game ignores the default mode.

You can also pass a command directly when starting the program, e.g. `VirtualAutoClicker.exe start "Firestone" 450,450 50`. If a clicker was started the prompt stays open so you can manage it.

To quit, type `exit` (or press Ctrl+C). All clickers are stopped when the program closes.

## Full list of commands

Commands are case-insensitive. Type `help` or `<command> --help` for details.

| Command | Aliases | Description |
|---|---|---|
| `start "P" X,Y I [N] [--mode post\|send]` | `startautoclicker` | Start a new clicker |
| `pause N` | `pauseautoclicker` | Pause a clicker |
| `resume N` | `resumeautoclicker` | Resume a paused clicker, or restart a failed one once its process is back |
| `stop [N]` | `picnic`, `stopautoclicker` | Stop and remove clicker N, or all clickers when N is omitted |
| `list` | `listall`, `listautoclickers` | Show all clickers with their state and click count |
| `coords "P" [--delay S]` | `pick` | After S seconds (default 3), print the cursor position relative to P's window |
| `help` | | Show all commands |
| `exit` | `quit` | Stop all clickers and quit |

### Notes

* If the target window is closed, the clicker re-attaches to a new window of the same process when it can, otherwise it is marked *failed* (see `list`).
* The program is per-monitor DPI aware, so `coords` reports physical pixels. On scaled displays, use `coords` rather than measuring by hand.
* In the source, stopping all clickers is referred to as `picnic`; if you watch any twitch streams you probably get the reference. :)

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```shell
dotnet build
dotnet test
dotnet publish src/VirtualAutoClicker -c Release -r win-x64   # Native AOT, output in artifacts/publish
```

Native AOT publishing needs the *Desktop development with C++* Visual Studio workload (plus the ARM64 build tools for `win-arm64`).

## License
```
Apache License
Version 2.0, January 2004
http://www.apache.org/licenses/

Copyright 2020-2026 Emil Widehov/@versaceLORD
```
