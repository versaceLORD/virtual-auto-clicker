# Changelog

## Unreleased

* Removed NuGet lock files; packages are restored only from nuget.org via `nuget.config`, at the exact versions in `Directory.Packages.props`.

## 2.0.0

Full modernization of the code base.

### Breaking changes

* Requires Windows 10/11 (x64 or x86). Each release has a *standalone* Native AOT exe (no .NET needed) and a *framework-dependent* single-file exe (needs the .NET 10 Runtime) for both **win-x64** and **win-x86**.
* The executable is `VirtualAutoClicker.exe`.
* Clickers without a name are now called `clicker-1`, `clicker-2`, ... instead of a GUID.
* Starting a clicker with a name that is already in use is rejected.
* Coordinates must be `X,Y` with both values in `0..32767`; the interval must be at least 1 ms.
* Clicks are posted (`PostMessage`) by default instead of sent synchronously. Use `start ... --mode send` for the old behaviour (now with a 1 s timeout so a hung window cannot freeze the clicker).
* No more "press Enter before typing a command": the `VAC >>` prompt is always ready.

### Added

* `exit`/`quit` command, `help` command and `--help` on every command.
* `stop N` stops a single clicker; `stop` without a name still stops all of them.
* `coords` (`pick`) command to read the cursor position relative to a window.
* `list` shows a table with state, process id, position, interval and click count.
* Commands can be passed on the command line (`VirtualAutoClicker.exe start "Game" 10,10 100`).
* Clickers re-attach to a new window if the process restarts, and are marked *failed* instead of silently dying. `resume` restarts a failed clicker.
* Per-monitor DPI awareness.

### Fixed

* A duplicate clicker name orphaned the running clicker, which kept clicking and could no longer be stopped.
* `startautoclicker "P" 450 50` (no comma) crashed the application.
* Interval `0` caused a tight loop; negative intervals made the clicker die silently.
* Quoted process names were parsed incorrectly unless they were the first argument.
* Numeric or comma separated input (`3`, `list,start`) was interpreted as a command.
* Processes without a main window were accepted and clicks were sent to window handle 0.
* Race condition where stopping a clicker during a click could throw on a background thread.
* X coordinates above 65535 corrupted the Y coordinate.
* P/Invoke used 32-bit `int` for pointer-sized parameters.
* `pause`/`resume` reported success for clickers that did not exist.
* The application crashed when input was redirected.

### Internal

* .NET 10 (LTS), C# 14, `.slnx`, Central Package Management, lock files, analyzers with warnings as errors, `.editorconfig`.
* Generic Host with dependency injection and source-generated logging; no more static global state.
* System.CommandLine for parsing; `[LibraryImport]` source-generated interop.
* MSTest 4 on Microsoft.Testing.Platform, 67 tests using `FakeTimeProvider`.
* GitHub Actions on Windows: format check, build, test with coverage, publish of all release variants. Merging to `main` with a new `<Version>` creates the GitHub release automatically, with checksums and build provenance. Dependabot.

## 1.1.2 and earlier

See the [GitHub releases](https://github.com/versaceLORD/virtual-auto-clicker/releases).
