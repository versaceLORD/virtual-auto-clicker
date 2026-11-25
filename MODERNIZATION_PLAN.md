# Virtual Auto Clicker — 2026 Modernization Plan

> **Status (2026-11-25):** Phases 0–8 are implemented on branch `modernize/net10` (not yet committed or merged).
>
> **Verification:**
> - `dotnet build` passes with 0 warnings, with warnings treated as errors.
> - `dotnet format --verify-no-changes` is clean.
> - `dotnet restore --locked-mode` passes.
> - `dotnet test` passes 67/67, and stayed green across 8 repeated runs.
> - The win-x64 Native AOT publish succeeds with 0 trim/AOT warnings (4.8 MB exe).
> - The app was smoke-tested manually against Notepad.
>
> **Not yet verified:** the GitHub Actions workflows have not run yet, and the win-arm64 build has not been tried in CI. Unchecked items below are optional work that was skipped on purpose; each one notes why.

## 1. Current state (baseline)

| Area | Today | Problem |
|---|---|---|
| Target framework | `net5.0` | EOL since May 2022; only SDK 9 installed locally, so it cannot build/test as-is |
| Language | C# 9 (implicit) | Misses C# 10–14 features (file-scoped namespaces, primary ctors, collection expressions, `required`, `field` keyword) |
| Solution | `.sln` (VS 2019 format, mixed project-type GUIDs) | `.slnx` is the default in the .NET 10 SDK / VS 2026 |
| Build config | No `global.json`, `Directory.Build.props`, `.editorconfig`, CPM | No SDK pinning, no shared settings, no analyzers, warnings not errors |
| Interop | `[DllImport] SendMessage(IntPtr, uint, int, int)` | Wrong widths for `WPARAM`/`LPARAM`/`LRESULT`; runtime marshalling; not AOT/trim friendly |
| Concurrency | Fire-and-forget `Task.Factory.StartNew(async …)`, CTS never disposed, shared state not synchronized | Unobserved exceptions, races (`Coordinates` nulled mid-click), leaks |
| Architecture | Static `VacEnvironment` service locator, mutable models, console I/O in domain classes | Hard to test, tightly coupled |
| CLI parsing | Hand-rolled quote merging in `ConsoleHelper.GetInputArguments` | Several index bugs (see §6) |
| Tests | 3 MSTest 2.2.5 tests on the arg parser, `Microsoft.NET.Test.Sdk` 16.10 | Outdated and too few tests; the expected/actual arguments to `Assert.AreEqual` are swapped |
| CI | `ubuntu-latest`, `checkout@v2`, `setup-dotnet@v1`, .NET 5 | Windows-only app built on Linux; deprecated actions; no release pipeline |
| Publishing | Two pubxml files, both pointing to `bin\Release\netcoreapp3.1\publish\` | Builds overwrite each other; not single-file/AOT |
| Versioning | Hard-coded `"1.1.2"` const | Not tied to assembly/tag |

## 2. Target state

- **.NET 10 (LTS, supported until Nov 2028)**, `net10.0-windows`, C# 14, pinned via `global.json` (`rollForward: latestFeature`).
- **Native AOT single-file executables** for `win-x64` and `win-arm64` (drop `win-x86` unless there is demand). Small, fast-starting, and no runtime install needed.
- **Source-generated interop** via `Microsoft.Windows.CsWin32` (or `[LibraryImport]`), using `PostMessage` and correct `nint` types.
- **Generic Host** (`Microsoft.Extensions.Hosting`) with DI, `ILogger`, `IOptions`, and graceful shutdown.
- **System.CommandLine 2.x** for both startup arguments and REPL command parsing.
- **MSTest 4 (`MSTest.Sdk`) on Microsoft.Testing.Platform**, with tests on parsing, the click scheduler and lParam packing.
- **GitHub Actions on `windows-latest`**: build, test, format check, and AOT publish + release on tag.
- Central Package Management, `.editorconfig`, analyzers at `latest-recommended`, `TreatWarningsAsErrors`.

## 3. Phased plan

Each phase ends with a green build and passing tests, and is merged as its own PR.

### Phase 0 — Housekeeping (small, safe)
- [x] Create branch `modernize/net10`.
- [x] Delete the stale branches `feature/mouse-down-only` (local + remote) and `feature/fetch-coord-cmd` (remote). Not salvaged.
- [x] Add `TestResults/`, `*.binlog` and `artifacts/` to `.gitignore`.
- [x] Add `.editorconfig` (the standard .NET template plus the repo's conventions: file-scoped namespaces, `var` preferences, naming rules).

### Phase 1 — Toolchain & build infrastructure
- [x] `global.json`: `{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" }, "test": { "runner": "Microsoft.Testing.Platform" } }`
- [x] Migrate the solution: `dotnet sln VirtualAutoClicker.sln migrate` → `VirtualAutoClicker.slnx`, then delete the `.sln`.
- [x] Add `Directory.Build.props` at the root:
  ```xml
  <Project>
    <PropertyGroup>
      <TargetFramework>net10.0-windows</TargetFramework>
      <LangVersion>latest</LangVersion>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
      <AnalysisLevel>latest-recommended</AnalysisLevel>
      <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
      <GenerateDocumentationFile>true</GenerateDocumentationFile>
      <NoWarn>$(NoWarn);CS1591</NoWarn>
      <Deterministic>true</Deterministic>
      <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
      <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts</ArtifactsPath>
      <Version>2.0.0</Version>
      <Authors>Emil Widehov</Authors>
      <Copyright>Copyright 2020-2026 Emil Widehov</Copyright>
    </PropertyGroup>
  </Project>
  ```
- [x] Add `Directory.Packages.props` with `ManagePackageVersionsCentrally=true` and move all versions there.
- [ ] ~~Optionally add `Nerdbank.GitVersioning` or `MinVer`~~ **Skipped:** `<Version>` lives in `Directory.Build.props`, the release workflow overrides it from the tag (`-p:Version=`), and `AppInfo.Version` reads it from the assembly. This replaces the `VersionNumber` const.
- [x] Simplify the app csproj to `OutputType=Exe`, `AssemblyName=VirtualAutoClicker`, `SupportedOSPlatform` (implied by `-windows`).
- [x] Fix every new warning/error introduced by nullable + analyzers in the test project.

**Exit criteria:** `dotnet build -warnaserror` and `dotnet test` pass on SDK 10.

### Phase 2 — Interop correctness (highest functional value)
- [x] **Done with the no-dependency alternative** (`Interop/NativeMethods.cs`): `[LibraryImport]` for `PostMessageW`, `SendMessageTimeoutW`, `IsWindow`, `GetCursorPos`, `ScreenToClient`. CsWin32 was not needed for 5 functions.
  - Originally proposed: `Microsoft.Windows.CsWin32` with a `NativeMethods.txt`. Alternative used: `[LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static partial bool PostMessage(nint hWnd, uint msg, nuint wParam, nint lParam);`
- [x] Move interop into a `Win32/` (or `Interop/`) folder behind an `IMouseInput` interface so it can be faked in tests.
- [x] Use **`PostMessage`** instead of `SendMessage` so a hung target window can't block the clicker (`AutoClicker.cs:139-151`).
- [x] Pass `MK_LBUTTON` as `wParam` on button-down (`AutoClicker.cs:142`; `Buttons.MkLbutton` is currently unused).
- [x] Fix lParam packing: `MAKELPARAM(x, y) = (nint)((y << 16) | (x & 0xFFFF))` (`AutoClicker.cs:137`).
- [x] Validate the HWND (`!= 0` and `IsWindow`), and call `Process.Refresh()` or re-resolve the handle when it becomes invalid (`AutoClicker.cs:50`, `:140`).
- [x] Dispose `Process` instances from `GetProcessesByName`. When several processes match, pick the one with a non-zero `MainWindowHandle` (or let the user choose by PID).
- [x] Delete `Constants/Buttons.cs` (constants now live in `NativeMethods`).
- [x] Send mode uses `SendMessageTimeout` with `SMTO_ABORTIFHUNG` (1 s) instead of plain `SendMessage`, selectable with `--mode send`.

### Phase 3 — Architecture: Generic Host, DI, logging
- [x] Add `Microsoft.Extensions.Hosting`. `Program.cs` becomes:
  ```csharp
  var builder = Host.CreateApplicationBuilder(args);
  builder.Services.AddSingleton<IMouseInput, Win32MouseInput>();
  builder.Services.AddSingleton<IProcessWindowLocator, ProcessWindowLocator>();
  builder.Services.AddSingleton<ClickerManager>();
  builder.Services.AddHostedService<ReplHostedService>();
  builder.Services.AddSingleton(TimeProvider.System);
  await builder.Build().RunAsync();
  ```
- [x] Remove the static `VacEnvironment` service locator. Commands get `ClickerManager` through constructor injection (primary constructors).
- [x] Rename `AutoClickerWorker` to `ClickerManager`:
  - Store clickers in a `ConcurrentDictionary<string, Clicker>` keyed by name.
  - Use `TryAdd` and reject duplicate names. This fixes the orphaned-clicker bug at `StartAutoClicker.cs:79/87-90`.
  - Implement `IAsyncDisposable`, which stops all clickers on shutdown. This replaces the double `Picnic()` in `Program.cs:58-63`.
- [x] Rewrite `AutoClicker` as `Clicker`:
  - Immutable configuration: `sealed record ClickerOptions(string Name, string ProcessName, Point Position, TimeSpan Interval, ...)` with validation.
  - Run loop: `Task RunAsync(CancellationToken)` using **`PeriodicTimer`** (via `TimeProvider` for testability) instead of `Task.Factory.StartNew(async …)`.
  - Keep the running `Task` and await it on stop, so exceptions are observed and logged.
  - Own and dispose one `CancellationTokenSource` per run. Pause/resume swaps it safely under a `Lock` (C# 13 `System.Threading.Lock`).
  - State enum `ClickerState { Running, Paused, Stopped, Faulted }` instead of `bool Active`.
  - No console writes inside the domain class; use `ILogger` or return results.
- [x] Replace `Models/Coordinates` with `readonly record struct ClientPoint(int X, int Y)` (or `System.Drawing.Point`).
- [x] Logging: `ILogger<T>` with `[LoggerMessage]` source-generated methods. Console formatter for interactive output; optional file sink (e.g. Serilog) only if needed.
- [ ] Optional: `appsettings.json` + `IOptions<ClickerDefaults>` for the default interval, saved presets and the prompt text. **Not done** (no concrete need yet). The host already loads an optional `appsettings.json` next to the exe, so `Logging:LogLevel` can be tuned without code changes.

### Phase 4 — CLI / REPL rewrite
- [x] Adopt **System.CommandLine 2.x** (stable). Define one `RootCommand` with subcommands:
  - `start <process> <x>,<y> <intervalMs> [--name]`
  - `pause <name>`, `resume <name>`, `list`, `stop [name]` (alias `picnic`), `exit`/`quit`
- [x] Use the same command tree for both:
  - **One-shot mode:** `VirtualAutoClicker start "Firestone" 450,450 50`
  - **REPL mode:** reads lines, tokenizes them with `CommandLineParser.SplitCommandLine`, and invokes the same tree.

  This removes the hand-rolled `ConsoleHelper.GetInputArguments` and its index bugs (`ConsoleHelper.cs:74, 78-84, 101, 104-106`).
- [x] Add a custom `Point` argument parser with validation: two integers in the range 0–32767 (the signed 16-bit range Windows unpacks with `GET_X_LPARAM`), required comma (`StartAutoClicker.cs:25`).
- [x] Validate the interval: ≥ 1 ms, with a sane lower bound/warning, e.g. < 10 ms (`StartAutoClicker.cs:38`).
- [x] Replace the `Command` enum and `Enum.TryParse` lookup (`VacEnvironment.cs:64`) with System.CommandLine aliases. This fixes numeric/comma input being resolved as commands.
- [x] REPL loop:
  - `Console.ReadLine` with no `ReadKey` gate (`Program.cs:22` fails on redirected stdin).
  - Ctrl+C → host shutdown (handled by the Generic Host's `ConsoleLifetime`).
  - Global try/catch per command so one bad command can't crash the app.
- [x] Console output stays plain `System.Console` (decided: no Spectre.Console). Rework `ConsoleHelper` into a small `IReplConsole` (`Cli/SystemConsole.cs`):
  - Serialize writes with a `Lock` so coloured output is thread-safe.
  - Use `Console.ResetColor()` instead of forcing `White`.
  - Use `Environment.NewLine` instead of `"\n\r"`.
  - Use one consistent `VAC >>` prefix. **Changed:** `VAC >>` is used only as the prompt; messages are coloured by severity instead of being prefixed, so output isn't confused with the prompt.
  - Render `list` as a simple aligned text table.
- [x] Optional feature: a `pick` / `coords` command that reports the cursor's client coordinates over the target window (`GetCursorPos` + `ScreenToClient`), so users don't have to guess X,Y.

### Phase 5 — Tests
- [x] Upgrade to **MSTest 4** with `MSTest.Sdk` project SDK (`<Project Sdk="MSTest.Sdk/4.x">`), which runs on Microsoft.Testing.Platform (decided).
- [x] Remove `Microsoft.NET.Test.Sdk` and `coverlet.collector`. Use `Microsoft.Testing.Extensions.CodeCoverage` instead.
- [x] Tests to add:
  - Command parsing:
    - Quoted process names with spaces.
    - Quote not in first position.
    - Missing comma.
    - Negative/zero interval.
    - Unknown and numeric commands.
  - `MAKELPARAM` packing: edge values 0, 0xFFFF, and high bits.
  - `ClickerManager`:
    - Duplicate names are rejected.
    - Stop/pause/resume state transitions.
    - Stop-all on dispose.
  - `Clicker` with `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) and a fake `IMouseInput`. Assert click count after advancing time. Assert no clicks while paused. Assert a faulted state when the window disappears.
- [x] Fix the reversed expected/actual arguments in the existing `Assert.AreEqual` calls (`ConsoleHelperTests.cs:22-26, 42-46, 62-66`), or delete those tests once `ConsoleHelper` parsing is removed.
- [ ] Optional: an automated integration smoke test that clicks a test WinForms window, run only on Windows CI. **Not done.** A manual smoke test against Notepad was run instead: start, duplicate name, list, pause/resume, stop, the window-closed fault, and the AOT build.

### Phase 6 — Publishing
- [x] Delete both outdated `.pubxml` files.
- [x] Add to the app csproj:
  ```xml
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
  <IsAotCompatible>true</IsAotCompatible>
  <OptimizationPreference>Size</OptimizationPreference>
  ```
- [x] Publish with `dotnet publish -c Release -r win-x64` and `-r win-arm64`. Output goes to `artifacts/publish/...` with no collisions.
- [x] Verify there are zero trim/AOT warnings (CsWin32, LibraryImport and System.CommandLine 2 are all AOT-safe).
- [x] Add an application manifest (`app.manifest`) with `dpiAware` / `PerMonitorV2`, so client coordinates match what users see on high-DPI displays.
- [ ] Optional: sign the binaries; publish to **winget** (`wingetcreate`). **Not done** (needs a signing certificate and a winget manifest PR).

### Phase 7 — CI/CD
- [x] Replace `.github/workflows/dotnet-core.yml` with `ci.yml`:
  - `runs-on: windows-latest`
  - `actions/checkout@v7`, `actions/setup-dotnet@v6` with `global-json-file: global.json` (latest majors as of Nov 2026)
  - `dotnet restore --locked-mode` (enable `RestorePackagesWithLockFile`)
  - `dotnet format --verify-no-changes`
  - `dotnet build -c Release --no-restore`
  - `dotnet test -c Release --no-build` with coverage + upload of test results
- [x] Add `release.yml` triggered on version tags (`2.0.0` or `v2.0.0`; existing tags have no `v` prefix):
  - Matrix of `win-x64` (on `windows-latest`) and `win-arm64` (built natively on `windows-11-arm`).
  - CI also does an AOT publish for both RIDs on every push or PR.
  - AOT publish, zip, SHA256 checksum.
  - Create the GitHub Release (`softprops/action-gh-release@v3`).
  - Build provenance attestation (`actions/attest-build-provenance`).
- [x] Add `.github/dependabot.yml` for `nuget` and `github-actions`.
- [ ] Optional: CodeQL workflow for C#. **Not done.** Enable it with GitHub's "default setup" under Settings → Code security; no workflow file is needed.

### Phase 8 — Docs
- [x] Update `README.md`:
  - Correct exe name: `VirtualAutoClicker.exe`, not `virtual-autoclicker-console.exe`.
  - Requirements: Windows 10/11; no runtime needed.
  - New command syntax, one-shot vs. REPL mode, and an `exit` command.
  - Remove the "not best practise" disclaimer once the work is done.
  - Build-from-source instructions: `dotnet build`, `dotnet test`.
- [x] Add a `CHANGELOG.md` entry for 2.0.0 that lists the breaking changes: command syntax, x86 dropped, and .NET 10.
- [x] Update the LICENSE/README copyright years.

## 4. Proposed target layout

```
/
├─ .editorconfig
├─ .github/
│  ├─ dependabot.yml
│  └─ workflows/ci.yml, release.yml
├─ Directory.Build.props
├─ Directory.Packages.props
├─ global.json
├─ VirtualAutoClicker.slnx
├─ README.md / CHANGELOG.md / LICENSE / MODERNIZATION_PLAN.md
└─ src/
   ├─ VirtualAutoClicker/
   │  ├─ Program.cs                  (host builder)
│  ├─ AppInfo.cs                  (name/version)
│  ├─ app.manifest
│  ├─ Cli/                        (CliCommands, IReplConsole, SystemConsole, ReplHostedService)
│  ├─ Clicking/                   (Clicker, ClickerManager, ClickerOptions, ClickerState)
│  ├─ Interop/                    (NativeMethods, ClientPoint, IMouseInput, Win32MouseInput, IWindowLocator, ProcessWindowLocator)
   │  └─ Logging/                    (LoggerMessage definitions)
   └─ VirtualAutoClicker.Tests/
      ├─ Cli/  Clicking/  Interop/
      └─ Fakes/                      (FakeMouseInput, FakeWindowLocator)
```

## 5. Packages (target versions, pinned in `Directory.Packages.props`)

| Package | Purpose |
|---|---|
| `Microsoft.Extensions.Hosting` **10.0.15** | Host, DI, logging, configuration |
| `System.CommandLine` **2.0.15** | CLI + REPL parsing. 3.0.0 has shipped alongside .NET 11; staying on the .NET 10-aligned 2.0.x line |
| ~~`Microsoft.Windows.CsWin32`~~ | Not used; `[LibraryImport]` instead |
| `MSTest.Sdk` **4.5.3** (pinned in `global.json` `msbuild-sdks`) | Tests on Microsoft.Testing.Platform |
| `Microsoft.Extensions.TimeProvider.Testing` **10.11.1** | Deterministic timer tests |
| `Microsoft.Testing.Extensions.CodeCoverage` | Coverage. Included by default by `MSTest.Sdk`, so no explicit reference is needed |
| ~~`MinVer` / `Nerdbank.GitVersioning`~~ | Not used; the version comes from the tag through `-p:Version` |

> Note: .NET 11 (STS) shipped in Nov 2026. This plan intentionally targets **.NET 10 LTS** (supported until Nov 2028). Revisit when .NET 12 LTS ships in Nov 2027.

## 6. Known bugs to fix as part of the work (tracked)

All 15 are fixed on branch `modernize/net10`. Regression tests cover #1, #2, #3, #5, #8 and #12. #15 is resolved by removing the old tests along with `ConsoleHelper`.

| # | Location | Bug | Fixed in |
|---|---|---|---|
| 1 | `StartAutoClicker.cs:79,87-90` | A duplicate name removes the running clicker from the dictionary without stopping it, so it keeps clicking unmanaged | Phase 3 |
| 2 | `StartAutoClicker.cs:25` | Coordinates without a comma throw an uncaught exception and crash the app | Phase 4 |
| 3 | `StartAutoClicker.cs:38` | Interval not validated (0 = tight loop; negative values make the task throw) | Phase 4 |
| 4 | `ConsoleHelper.cs:74,78-84,101,104-106` | Quote-merging index bugs | Phase 4 (parser removed) |
| 5 | `VacEnvironment.cs:64` | Numeric/comma input resolves to commands | Phase 4 |
| 6 | `AutoClicker.cs:50` | `MainWindowHandle` not checked against `IntPtr.Zero` | Phase 2 |
| 7 | `AutoClicker.cs:132-137` | Race: `Coordinates` can be nulled during a click | Phase 3 |
| 8 | `AutoClicker.cs:137` | lParam packing uses `+` instead of `| (x & 0xFFFF)` | Phase 2 |
| 9 | `AutoClicker.cs:19-20` | P/Invoke uses `int` for pointer-sized params | Phase 2 |
| 10 | `AutoClicker.cs:72-79` | Fire-and-forget `StartNew(async)`; unobserved exceptions | Phase 3 |
| 11 | `AutoClicker.cs:47,93,107,122` | CTS leaks | Phase 3 |
| 12 | `Pause.cs:18`, `Resume.cs:18` | Success message printed even when the clicker was not found | Phase 4 |
| 13 | `Program.cs:22` | `ReadKey` fails with redirected stdin | Phase 4 |
| 14 | `Program.cs:58-63` | Double `Picnic()` on Ctrl+C; no exit command | Phase 3/4 |
| 15 | `ConsoleHelperTests.cs` | Expected/actual arguments swapped in `Assert.AreEqual` | Phase 5 |

## 7. Risks & decisions

- **Breaking CLI changes.** System.CommandLine changes the syntax a little (e.g. options vs. positionals). Mitigation: keep the positional form `start "Proc" X,Y I [Name]` and the existing aliases (`startautoclicker`, `picnic`, …) as System.CommandLine aliases.
- **PostMessage vs. SendMessage.** Some games/frameworks only respond to one or the other, or ignore synthetic messages entirely. Mitigation: make the method selectable (implemented as `start ... --mode post|send`), defaulting to `post`.
- **Dropping x86.** Win x86 is rare in 2026. Native AOT supports `win-x86`, so it can be kept in the matrix if users ask.
- **DPI.** Making the process per-monitor DPI aware changes the coordinate space. Document this, and test on a 150% scaled display. **Still open:** this is documented in the README, but no test on a scaled display has been done yet.
- **Native AOT for arm64 on a dev machine** needs the MSVC ARM64 build tools; CI avoids this by building on `windows-11-arm`.

## 8. Suggested execution order & effort

| Phase | Effort | Risk |
|---|---|---|
| 0 Housekeeping | 0.5 h | Low |
| 1 Toolchain (.NET 10, slnx, props, CPM, analyzers) | 2 h | Low |
| 2 Interop | 2 h | Medium (behaviour change) |
| 3 Host/DI/clicker rewrite | 4–6 h | Medium |
| 4 CLI/REPL | 3–4 h | Medium (UX change) |
| 5 Tests | 3 h | Low |
| 6 Publishing (AOT) | 1–2 h | Low–Medium |
| 7 CI/CD | 1–2 h | Low |
| 8 Docs | 1 h | Low |

**Total:** about 3 working days. Ship as **v2.0.0**.
