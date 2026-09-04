---
name: mfr7-reference
description: >-
  Retrieves Magic File Renamer 7 (MFR7) reference data from legacy source, installed
  help, screenshots, and UI code when implementing features in finebytes. Use when
  porting filters, formatter tokens, UI panes, shortcuts, presets, or matching MFR7
  behavior; when the user mentions MFR7, MFR 7.4, legacy, or parity with the old app.
---

# MFR7 reference retrieval

Use MFR7 as the **behavior and UX reference** for the finebytes rewrite. This skill tells you **where to look** and **how to extract** capability specs before implementing in `finebytes`.

## Reference locations

| Resource                 | Path                                        | Best for                                                                       |
| ------------------------ | ------------------------------------------- | ------------------------------------------------------------------------------ |
| Legacy source            | `D:\Devl\mfr7`                              | Filter logic, options, editor UI, shortcuts, preset XML                        |
| Installed app            | `C:\Program Files\FineBytes\MFR7\MFR.exe`   | Live behavior, dialogs, manual QA                                              |
| Help HTML + screenshots  | `C:\Program Files\FineBytes\MFR7\Help\`     | User-facing descriptions, examples, option dialogs (GIF/PNG in `Help/Images/`) |
| Help (source copy)       | `D:\Devl\mfr7\Site\finebytes\mfr\Help\`     | Same HTML as install; use when Help is missing from Program Files              |
| Tooltips / control hints | `C:\Program Files\FineBytes\MFR7\hints.txt` | Status-bar and toolbar hint text                                               |
| Already-ported behavior  | `finebytes` repo                            | Tests/docs that cite MFR7; do not re-derive what is already captured           |

If paths differ on another machine, search for `mfr7` under the user's `Devl` folder and `FineBytes\MFR7` under Program Files.

## Workflow

Copy this checklist and track progress:

```text
MFR7 reference:
- [ ] 1. Scope the feature (filter / token / UI pane / shortcut / preset / metadata)
- [ ] 2. Check finebytes first (existing impl, tests, Mfr.Filters/docs)
- [ ] 3. Locate MFR7 artifact (see lookup table below)
- [ ] 4. Read help page + screenshot for UX and examples
- [ ] 5. Read source: filter class, editor, Apply/transform logic
- [ ] 6. Note options, defaults, edge cases, target fields
- [ ] 7. Write capability brief (template below)
- [ ] 8. Implement in finebytes; add tests mirroring MFR7 examples
```

### Step 1 — Scope

| Feature kind            | Start in finebytes                          | Then in MFR7                                                                    |
| ----------------------- | ------------------------------------------- | ------------------------------------------------------------------------------- |
| Preset filter           | `Mfr.Filters/<Group>/`, `Mfr.Filters/docs/` | `Core/MfrFilters/Filters/<Group>/`                                              |
| Formatter token         | `Mfr.Filters/Formatting/Tokens/`            | `Core/MfrFilters/FormattingParams/` + `Help/*fp.html`                           |
| Filter palette / groups | `FilterGroup.cs`, `FilterCatalog.cs`        | `Core/MfrLib/Filters/FilterGroups.cs`, `[FilterInfo(...)]`                      |
| Main window / panes     | `Mfr.App.Ui/`, `Mfr.App.Ui/README.md`       | `Core/MFRGui/Forms/`                                                            |
| Keyboard shortcuts      | `docs/keyboard-shortcuts.md`                | `Core/MFRGui/Forms/Main/Main.cs`, `RenameList/RenameList.cs` (`.Shortcut =`)    |
| Audio tags              | `docs/audio-tag-model.md`                   | `Core/MfrFilters/Filters/Audio/`, `Help/id3*.html`                              |
| Image / EXIF            | `docs/image-metadata-model.md`              | `Help/eximagefp.html`, `basicimagefp.html`, `MetaDataExtractor` usage in source |
| CLI                     | `Mfr.App.Cli/`                              | `Core/MfrConsole/Console.cs`, `Help/console.html`                               |
| Presets                 | `Mfr.Engine/Presets/PresetJsonOptions.cs`   | MFR7 XML preset serialization in `Core/MfrLib/` (search `Preset`, `Serialize`)  |

### Step 2 — Check finebytes first

Before diving into MFR7:

1. Grep finebytes for `MFR7`, `MFR 7`, or the feature name.
1. Read matching tests — they often encode parity examples (e.g. `Id3v2TokenTests`, `SpaceCharacterFilterTests`).
1. Read `Mfr.Filters/docs/<Group>/<FilterType>.md` if present.
1. Read architecture docs: `docs/mfr-folder-layering.md`, `docs/magic-file-renamer-design.md`, domain docs under `docs/`.

If finebytes already documents behavior, treat MFR7 as confirmation only.

### Step 3 — Find the MFR7 artifact

**Filters:** grep legacy source for `[FilterInfo(` or the display name:

```powershell
rg -l "Space Character" "D:\Devl\mfr7\Core\MfrFilters"
rg "\[FilterInfo" "D:\Devl\mfr7\Core\MfrFilters\Filters" -g "*Filter.cs"
```

Each filter declares `[FilterInfo(name, group, shortDesc, helpFileName, innerGroupOrder)]`. The `helpFileName` (e.g. `spacecharfilter.html`) is the help page and usually matches a screenshot `Help/Images/<stem>.gif`.

**Help index:** open `Help/filters.html` (filter catalog by group) or `Help/fp.html` (formatter parameters). `Help/index.html` links the full tree.

**UI:** filter option dialogs live beside filters as `*FilterEditor.cs` under `Core/MfrFilters/Filters/`. Shell layout: `Core/MFRGui/Forms/Main/`.

**Formatter tokens:** legacy classes under `Core/MfrFilters/FormattingParams/`; help pages named `*fp.html` (e.g. `filenamefp.html`, `id3v2fp.html`).

Name mapping (MFR7 class → finebytes type, groups, help file): see [filter-map.md](filter-map.md). Formatter parameter mapping: see [formatter-tokens.md](formatter-tokens.md).

### Step 4 — Help pages and screenshots

For a filter or token:

1. Read `Help/<helpFileName>` from the install or Site copy.
1. Extract: purpose, option labels, **worked examples** (`<CODE>` blocks), target-field notes.
1. Open referenced images under `Help/Images/` — these are the **option-dialog screenshots** (GIF/PNG). Read image files directly when implementing UI parity.

Help pages also link related topics (e.g. Space Character → following filters use its separator).

### Step 5 — Source code

Read in this order:

1. **Filter class** (`*Filter.cs`) — `Apply`, `BeforeGroupApply`, public option fields, target handling.
1. **Editor** (`*FilterEditor.cs` + `.resx`) — control layout, defaults, validation messages.
1. **Shared base** — e.g. `BaseFormatterFilter`, `Filter.cs` in `Core/FiltersBase/`.
1. **GUI integration** — how the main window invokes the feature (`Core/MFRGui/`).

For formatter behavior, trace from `FormattingParams/*FormattingParameter.cs` into format-string parsing.

### Step 6 — Live app (optional)

Launch `"C:\Program Files\FineBytes\MFR7\MFR.exe"` when you need to verify dialog layout, tab order, or ambiguous behavior. Compare side-by-side with `just run-ui` in finebytes.

### Step 7 — Capability brief

Produce this before coding:

```markdown
## MFR7 reference: [Feature name]

### Sources
- Help: Help/[file].html (+ Images/[file].gif)
- Code: [paths under D:\Devl\mfr7]
- finebytes status: [none | partial | done]

### Behavior
- Purpose: ...
- Options (name, type, default): ...
- Apply order / pipeline interactions: ...
- Target fields: prefix / extension / full name / path / tags / attrs

### Examples (from help or tests)
| Input | Options | Output |
|-------|---------|--------|

### UX notes
- Display name, group, dialog controls, shortcuts

### Parity gaps / intentional diffs
- ...
```

### Step 8 — Implement in finebytes

- Filters: follow [mfr-implement-filter/SKILL.md](../mfr-implement-filter/SKILL.md).
- Preserve MFR7 **display-name exceptions** (`TagRemover` → `Audio Tag Remover`, etc.); see `FilterCatalogTests.Known_Display_Names_And_Groups`.
- Map MFR7 groups to `FilterGroup`: Case, Space, Trimmer→Trimming, Replace, Format→Formatting, ID3→Audio, Attrs→Attributes, Misc.
- Prefer clean modern design over legacy shims (see `AGENTS.md` refactoring policy); document intentional behavior changes in the brief.
- Add tests using help examples as assertion cases.

## Quick search commands

Run from any directory:

```powershell
# MFR7 filter by display name
rg -i "display name fragment" "D:\Devl\mfr7\Core\MfrFilters"

# Help page for a filter (from FilterInfo help file name)
Get-Content "C:\Program Files\FineBytes\MFR7\Help\spacecharfilter.html"

# finebytes tests mentioning MFR7
rg -i "mfr7|MFR 7" "D:\Devl\finebytes"

# Shortcuts in legacy main window
rg "\.Shortcut" "D:\Devl\mfr7\Core\MFRGui" -g "*.cs"

# Formatter parameter classes
Get-ChildItem "D:\Devl\mfr7\Core\MfrFilters\FormattingParams" -Recurse -Filter "*FormattingParameter.cs"
```

## UI shell reference (MFR 7.4)

Documented in `Mfr.App.Ui/README.md` and `Help/ui.html`, `Help/parts.html`, `Help/overview.html`:

- Splitter shell: File List (left), Rename List, Available Filters, Applied Filters, preview areas.
- File List types: Large Icons, Small Icons, Report, List, Tiles, Thumbnails.
- Key panes: `fileexp.html`, `renamelist.html`, `availfilterlist.html`, `appliedfilterlist.html`, `filteropts.html`, `presetmanager.html`.

Use `Help/Images/hotspots.gif` for labeled shell regions.

## Related finebytes skills and docs

- Implement filters: [mfr-implement-filter](../mfr-implement-filter/SKILL.md)
- Filter name / group map: [filter-map.md](filter-map.md)
- Formatter token map: [formatter-tokens.md](formatter-tokens.md)
- Shortcuts: `docs/keyboard-shortcuts.md`
- Layering: `docs/mfr-folder-layering.md`
