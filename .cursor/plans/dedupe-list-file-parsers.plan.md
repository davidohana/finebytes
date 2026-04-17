# Deduping file-based list parsers (updated)

## Constraint

**Breaking changes are acceptable** when they simplify alignment across [NameListParser](d:/Devl/finebytes/Mfr.Filters/Formatting/NameListParser.cs), [CasingListParser](d:/Devl/finebytes/Mfr.Filters/Case/CasingListParser.cs), and [ReplaceListParser](d:/Devl/finebytes/Mfr.Filters/Replace/ReplaceListParser.cs). Update tests and [Mfr.Filters/docs](d:/Devl/finebytes/Mfr.Filters/docs/) for any visible behavior or error-message changes ([AGENTS.md](d:/Devl/finebytes/AGENTS.md) / project rules allow forward-only refactors).

## Shared extraction (unchanged direction)

- Add a small internal helper (e.g. `ListFileParseHelpers.cs` in `Mfr.Filters`) with:
  - **`_ValidateListFilePath(string filePath, string listKindLabel)`** — empty path + existence checks.
  - **`_IsListFileCommentLine(string line)`** — one canonical rule: after leading whitespace (`TrimStart`), treat `//`, `\\`, or `# ` as comments (same family of markers as today).

## Stronger alignment (now in scope because breaks are OK)

1. **Comment semantics** — Implement **once** using `TrimStart` + prefix checks. Do not preserve separate “fully trimmed vs trim-start-only” paths; document the single rule. Adjust tests only if an edge case intentionally changes.

2. **Line-length limits** — Consider **one shared maximum** for all three list file types (e.g. align casing-list with name-list’s 65536, or pick a single project-wide cap). Replace-list’s 1000-char limit is product-specific; either keep it as an extra validation step after shared read, or document a deliberate lower cap for replace files. Decide in implementation based on safety vs UX.

3. **Error messages** — Optionally normalize phrasing (e.g. consistent `{kind} file not found` / `cannot be empty`) so maintenance is trivial; update assertions in parser tests.

4. **Reading lines** — Optional: centralize `File.ReadAllLines` in one place if you add encoding or pre-pass validation later; not required for a first refactor.

## What stays format-specific

- NameList: skip empty lines, index mapping to `GlobalIndex`, “at least one name entry”.
- CasingList: one word per line, last duplicate wins, dictionary output.
- ReplaceList: `S:`/`R:` pairs, `<EMPTY>`, pair ordering, replace-specific length cap if kept stricter than the global max.

## Tests (required): dedicated suite per parser

Each parser must have its **own** test class in `Mfr.Tests`, mirroring the parser type (one test file per parser, not a shared “list file parsers” mega-suite):

| Parser | Test class |
|--------|------------|
| `NameListParser` | **`NameListParserTests`** ([`Mfr.Tests/Models/Filters/Formatting/`](d:/Devl/finebytes/Mfr.Tests/Models/Filters/Formatting/) — **add**; today coverage is mostly via `NameListFilterTests`) |
| `CasingListParser` | **`CasingListParserTests`** (already exists — extend/update as behavior changes) |
| `ReplaceListParser` | **`ReplaceListParserTests`** (already exists — extend/update as behavior changes) |

**Scope:** Parser tests call `NameListParser.ParseFile`, `CasingListParser.ParseFile`, and `ReplaceListParser.ParseFile` directly and assert parse results and `UserException` cases (path, empty file, comments, format rules). Filter-level tests (`NameListFilterTests`, etc.) remain for integration; they do not replace parser tests.

**Shared helper tests:** Add **`ListFileParseHelpersTests`** that call `_ValidateListFilePath` and `_IsListFileCommentLine` directly. **`InternalsVisibleTo` is approved:** add `[assembly: InternalsVisibleTo("Mfr.Tests")]` on `Mfr.Filters` (or merge into an existing `AssemblyInfo` / global usings pattern if the project already uses one). Rationale:

- **Path and comment rules** are documented once in tests (empty path, missing file, `#` without space, leading whitespace, each marker).
- **Parser tests** can focus on format-specific behavior and a **smaller** set of smoke cases for comments/paths, instead of duplicating the full matrix three times.

## Verification

- Run `ListFileParseHelpersTests`, the three parser test classes, and any affected filter tests under `Mfr.Tests`; update docs under `Mfr.Filters/docs/` for Replace / Casing / Name list files if behavior or limits change.

## Todos

- [ ] Add `ListFileParseHelpers` with `_ValidateListFilePath` and `_IsListFileCommentLine` + XML docs
- [ ] Refactor three parsers to use helpers; remove duplicate private methods
- [ ] Apply stronger alignment items (comment rule, optional unified max length, messages) as decided during implementation
- [ ] Add **`InternalsVisibleTo("Mfr.Tests")`** to `Mfr.Filters` and **`ListFileParseHelpersTests`** for shared path + comment behavior
- [ ] Add or extend **dedicated tests per parser** (`NameListParserTests`, `CasingListParserTests`, `ReplaceListParserTests`); update filter docs; run `just test` / solution build per AGENTS
