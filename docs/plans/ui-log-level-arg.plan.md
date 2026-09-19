---
name: UI log-level CLI arg
overview: Let the desktop UI set Serilog minimum level via -l/--log-level (same names as console), default info, parsed before LogSession.Start.
todos:
  - id: p1-parse-and-start
    content: "P1: Shared log-level parse + Program.Main apply before LogSession.Start; UiStartupArgsParser accepts -l/--log-level"
    status: pending
  - id: p2-help-tests
    content: "P2: Parser/Program tests + help/cml + whatsnew"
    status: pending
isProject: false
---

# UI log-level CLI arg plan

Parent: [docs/plans/ui-cmdline-params.plan.md](ui-cmdline-params.plan.md) (desktop argv already ships for sources / `--initial-folder` / add modifiers). This plan adds one host option only.

## Decisions (locked)

- **Surface:** Command-line only for the desktop UI. No Options dialog control; no `config.json` persistence of log level (session only for that process).
- **Syntax:** Match console — `-l` / `--log-level <LEVEL>` with `debug|info|warn|error` (case-insensitive). Default when omitted: **`info`** (`LogEventLevel.Information`), same as today.
- **When applied:** Parse **before** `LogSession.Start` in [`Mfr.App.Ui/Program.cs`](../../Mfr.App.Ui/Program.cs). Logger cannot be restarted later without a redesign; `App.axaml.cs` / `UiStartupArgsApplier` stay out of the level path.
- **Invalid value:** Soft-fail like other desktop argv: fall back to `info`, keep a short warning string, surface it on the status bar after the main window is up (same pattern as soft-fail parse in `App`). Do not abort process launch.
- **Shared parse:** Move `CliLogging.ParseLogLevel` (+ default name constant) to a shared type under Engine logging (e.g. `Mfr.Engine.Logging.LogLevelParser`) so CLI and UI cannot drift. CLI keeps calling the shared API.
- **Parser awareness:** [`UiStartupArgsParser`](../../Mfr.App.Ui/UiStartupArgsParser.cs) must accept `-l` / `--log-level` and consume the value so those tokens are not `Unknown option`. It does not need to store the level on `UiStartupArgs` if `Program` already applied it (ignore after consume is fine).

## MFR7 reference brief

- **Sources:** MFR7 Help (whatsnew / cmdline) — no GUI log-level launch flag found; console-oriented logging only.
- **Behavior / UX:** N/A — MFR8 addition aligned with current `mfr --log-level`, not MFR7 GUI parity.
- **Parity gaps:** None expected.

## Non-goals

- Options UI or persisted prefs for log level
- Changing log level mid-session without restart
- Verbose / Fatal as extra names (stick to console’s four)
- Console sink for the UI (file session only, as today)

## Design sketch

```text
Program.Main
  ConfigStore.Load / EnsureDefaultFile
  ResolveUiLogLevel(args) → (LogEventLevel level, string? softWarning)
  LogSession.Start(level, ConfigStore.Log)
  Avalonia…
    App parses UiStartupArgs (consumes --log-level without error)
    if softWarning → status bar
```

`ResolveUiLogLevel`: scan argv for `-l` / `--log-level` (last wins if repeated); missing → `info`; bad value → `info` + warning text. Prefer calling shared `LogLevelParser` inside a try/catch that maps `UserException` to the soft warning (CLI can keep throwing).

## Phases

### P1 — Parse early + start logger + accept flag in UI parser

- **Scope / files:**
  - New shared parser (e.g. [`Mfr.Engine/Logging/LogLevelParser.cs`](../../Mfr.Engine/Logging/LogLevelParser.cs)); retarget [`CliLogging.ParseLogLevel`](../../Mfr.App.Cli/CliLogging.cs)
  - [`Program.cs`](../../Mfr.App.Ui/Program.cs) — resolve level from `args` before `LogSession.Start`
  - Small helper to hold optional soft-fail warning for App (static one-shot or return via existing crash/status path — keep minimal)
  - [`UiStartupArgsParser.cs`](../../Mfr.App.Ui/UiStartupArgsParser.cs) — recognize `-l` / `--log-level` and require a value
- **Exit criteria:**
  - Default launch still logs at Information
  - `Mfr.App.Ui.exe --log-level debug` (or `-l debug`) writes Debug events (e.g. beta time probe when enforcement on)
  - Unknown levels do not crash the UI; warning reaches the user
  - CLI still accepts the same level names via shared parser
- **Tests:** Unit tests for shared parser; UI argv tests that `--log-level` is accepted alongside sources / `--initial-folder`; soft-fail path for bad level

### P2 — Help

- **Scope / files:** [`help/guide/cml.html`](../../help/guide/cml.html) (desktop table + usage line); brief bullet in [`help/intro/whatsnew.html`](../../help/intro/whatsnew.html) if desktop cmdline is listed there
- **Exit criteria:** Help documents `-l` / `--log-level` for the desktop UI with the same level names as console; link integrity still green
- **Tests:** Help link tests if anchors change

## Implement

Use `@mfr-impl-plan` (or implement P1 then P2) when ready. Do not treat Options / config persistence as in scope unless a follow-up plan says so.
