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

## References

- `CONTRIBUTING.md`
- `docs/mfr-folder-layering.md`
- `docs/magic-file-renamer-design.md` (consult for architecture/feature-level changes)
- `docs/keyboard-shortcuts.md` (menu/toolbar/File List keys; keep in sync with `AppShortcuts`)
- `docs/audio-tag-model.md` (embedded audio tag read/write/Apply)
- `docs/image-metadata-model.md` (lazy MetadataExtractor image properties + EXIF cache)
- `docs/debts.md` (deferred / later-work list)
- `docs/rename-list-phase5-followups.md` (deferred shuttle/column refactors after Phase 5)
- `docs/rename-list-phase6b-followups.md` (6c–6e shipped; next is 8a Refresh)
- `.editorconfig`
- Skills: `.agents/skills/` — `mfr-code-review`, `mfr-implement-filter`, `mfr7-reference`, `mfr-ui-headless-tests`
