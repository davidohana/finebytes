# AGENTS

## What to know

- Use `just` for common tasks: `restore`, `build`, `test`, `format`, `lint`, `run-ui`.
- Before committing, run `just format` then `just lint` (or `just build` after format if you only need compile/analyzer checks).
- Markdown tools need a local `.venv` once: `python -m venv .venv` and `pip install -r requirements-md.txt` (see `CONTRIBUTING.md`).
- Formatting: CSharpier for `.cs` / `.csproj` / `.axaml` / `.slnx` layout; `dotnet format style` / `analyzers` for non-whitespace rules; mdformat (+ gfm, frontmatter) for `.md` (via `just format-md`, also part of `just format`).
- Linting: `just lint` runs C# checks (`dotnet build`, `csharpier check`, `dotnet format … --verify-no-changes`) and markdown checks (`just lint-md`: `mdformat --check`, `pymarkdown scan`).
- Follow project code style from `.editorconfig`.
- Project skills live in `.agents/skills/` only (Cursor and Antigravity both load this path). Do not add a parallel `.cursor/skills/` tree.
- Always-on agent constraints live in this file. On-demand workflows live in skills under `.agents/skills/`.
- UI interaction tests: see skill `mfr-ui-headless-tests` and `Mfr.Tests/Ui/FileList/FileListViewTests.cs`.
- Plans: save all implementation / feature plans under `docs/plans/` (kebab-case names, e.g. `docs/plans/rename-list-ui.plan.md`). Do not put project plans in `.cursor/plans/` or elsewhere.

## C# code style

- Public API docs: every `public` member gets `summary` plus `param` / `returns` / `exception` as needed. Start `<summary>` with one short line; put extra detail in a following `<para>...</para>` block.
- Private methods: prefix every `private` method name with `_` (including `static` and `async`).
- Control flow: prefer early `return` and early `continue`/`break`; keep the happy path at base indentation.
- Braces: always use braces; CSharpier owns brace and whitespace layout (do not fight it with hand-tuned wrapping).
- LINQ vs loops: prefer LINQ for filtering/projection/aggregation when it improves readability; use loops when a LINQ expression would be harder to read or too complex.
- Ternary usage: only use ternary for simple value selection (`condition ? valueA : valueB` with trivial operands).
- Avoid complex ternary branches that call functions, construct objects, throw, or span multiple lines; use `if`/`else` instead.
- Line wrapping: CSharpier wraps from `max_line_length` (120); prefer readable statement splits over fighting the printer.
- Statement complexity: avoid complex single statements; split them into multiple statements with well-named local variables.
- Boolean complexity: extract non-trivial boolean conditions into a well-named local (e.g. `isConflict`) before branching.
- Named arguments: use named args when parameter meaning is not obvious by position (especially multiple same-type args).
- Dictionary/HashSet naming: use `keyToVal`-style names (for sets, use boolean-style values like `pathToIsIncluded`).
- Access modifiers: prefer `private` over `internal` when members are not required outside the declaring type.
- Warning suppressions: every suppression (`#pragma`, `[SuppressMessage]`, `.editorconfig` overrides, etc.) must include a brief nearby comment explaining why it is needed and why safer alternatives were not used.
- Filter `_Setup` caches: always assign every instance cache field (including clear to null/default). Record `with` copies those fields while resetting setup-complete; conditional assign leaves stale values after an option is cleared (see `BaseFilter` remarks).

### Namespaces and folders

- Namespace **must** match folder path under the project (`RootNamespace` = project name). Example: `Mfr.App.Ui/Views/FilterEditors/Space/Foo.cs` → `namespace Mfr.App.Ui.Views.FilterEditors.Space`.
- AXAML `x:Class` namespace must match the view’s folder the same way; keep the code-behind namespace identical.
- When adding or moving types, update **folder + namespace + usings + x:Class** together — never leave a file in a new folder with an old namespace.
- Mirror feature folders across UI pairs: `ViewModels/<Feature>/` ↔ `Views/<Feature>/` (and `Mfr.Tests/Ui/<Feature>/` for that pane’s tests). Put category editors under the same `FilterGroup` name as `Mfr.Filters` (e.g. `…/FilterEditors/Trimming/`).
- Enforced by IDE0130 (warning) in `.editorconfig`; do not suppress without a nearby comment.

## Refactoring compatibility policy

- This project is new; when refactoring, do not preserve legacy behavior solely for backward compatibility.
- Prefer clean, current designs over compatibility shims, adapter layers, or deprecated-path fallbacks.
- If a breaking change is needed during refactor, implement the simpler forward path and update tests/docs accordingly.

### Persistence and session (no legacy migrations)

- Do **not** add JSON converters, dual-property records, or load-time adapters to read old saved shapes (renamed keys, removed enums, old property names).
- Persist **one current schema** per field (`session.json`, config snapshots, etc.). Use normal `[JsonPropertyName]` / record properties — not custom migration converters.
- On load, missing or unrecognized persisted data may fall back to **defaults** (same as first launch). Do not silently remap obsolete values unless the user explicitly asks for migration.
- Do **not** add tests whose only purpose is proving legacy JSON still loads after a schema change.
- When replacing a persisted type (e.g. sort keys: enum → field key), delete the old type and update callers/tests; do not keep both paths “just in case.”

Example (Rename List Auto-Sort): session `sortFields` entries are `{ "key": …, "descending": … }` only — no `"column"` enum, no `SessionState*JsonConverter` for backward compatibility.

## Cursor Cloud specific instructions

Cloud Agents run on Linux (Ubuntu 24.04), but Magic File Renamer is a Windows-targeted desktop app (`WinExe`, `.ico`, `app.manifest`, COM interop, Windows path/attribute semantics). The repo-managed `.cursor/` environment (`Dockerfile` + `install.sh` + `environment.json`) provisions the .NET 10 SDK, `just`, Avalonia native deps, `Xvfb`, fonts, and the markdown `.venv` automatically.

- Build, lint, and format all work on Linux: `just build`, `just lint`, `just format`. CRLF is enforced cross-platform via `.gitattributes`, so do not "fix" line endings by hand.
- Running the UI needs a display: `Xvfb` is started on `:99` at boot and `DISPLAY=:99` is preset, so `just run-ui` renders headlessly. Capture the window with `xwd -root -silent | convert - shot.png`.
- Unit tests are expected to pass on Linux. Windows-only cases use `[WindowsFact]` and show as Skipped (drive-letter tokens, Hidden attribute commits). Prefer `TestPaths.Absolute(...)` over hard-coded `C:\…` strings in new tests.
- `just test` should report 0 failures; Skipped > 0 for Windows-only facts is normal.

## References

- `CONTRIBUTING.md`
- `docs/mfr-folder-layering.md`
- `docs/magic-file-renamer-design.md` (consult for architecture/feature-level changes)
- `docs/keyboard-shortcuts.md` (menu/toolbar/File List keys; keep in sync with `AppShortcuts`)
- `docs/audio-tag-model.md` (embedded audio tag read/write/Apply)
- `docs/image-metadata-model.md` (lazy MetadataExtractor image properties + EXIF cache)
- `docs/debts.md` (deferred / later-work list)
- `docs/plans/` (implementation / feature plans)
- `docs/plans/rename-list-ui.plan.md` (Rename List UI; 1–13 + 14a done; next 14b–16)
- `.editorconfig`
- Skills: `.agents/skills/` — `mfr-code-review`, `mfr-implement-filter`, `mfr-implement-filter-editor`, `mfr7-reference`, `mfr-ui-headless-tests`
