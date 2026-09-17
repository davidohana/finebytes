# Generate Rename Script plan

Parent: Cursor backlog MFR7 feature parity item **11** (P2).

## Decisions (locked)

- **Product:** Ship a modernized **script export**, not a 1:1 `.bat`-only clone and not a skip.
- **Formats (v1):** Windows **`.bat`** and **PowerShell `.ps1`** only. No `.sh`/bash (WinExe product).
- **Scope:** Emit **path rename/move** and **RAHS attribute** deltas only — same capability ceiling as MFR7 Help `batchfile.html`. **Do not** emit file dates, audio tags, or image tags (those stay GO / CLI / engine commit).
- **Not a dry-run substitute:** GO preview + CLI `--dry-run` / `-o` JSON remain the primary “inspect without writing” paths. This feature is for **offline / reviewable / hand-editable** apply scripts.
- **UX:** **Tools → Generate Rename Script…** (MFR7 name was “Generate Batch Renaming File”). Save dialog offers both types; **format = chosen extension** (`.bat` / `.ps1`). No options dialog.
- **Architecture:** Build a small **operation IR** once from the Rename List, then format to bat/ps1. Do not duplicate emit logic in two string templates.
- **Modernizations vs MFR7:**
  - UTF-8 text (`.ps1` with BOM so Windows PowerShell 5.x reads non-ASCII paths).
  - Attribute commands target the **destination path after rename/move** (fix MFR7 quirk that `attrib` used the original path).
  - Escape paths per shell (`"…"` for cmd; single-quoted literals with `''` for PowerShell).
  - Skip rows with **PreviewError**; skip rows with only tag/date changes.
  - Same-folder rename → `ren` / `Rename-Item`; folder change → `mkdir`/`New-Item` + `move`/`Move-Item` (keep MFR7 case-only folder rename via rename, not move).
- **Non-goals (v1):** CLI `--export-script`, undo scripts, running the script from the app, dates/tags in scripts, multi-format beyond bat/ps1.

## Status

**Done** (P1–P3). Shipped modernized script export: Tools → Generate Rename Script… writes Windows `.bat` or PowerShell `.ps1` from Rename List path/RAHS deltas (IR + emitters + UI). Parity backlog item **11** points here.

## MFR7 reference brief

### Sources

- Help: `Help/batchfile.html` (install or `D:\Devl\mfr7\Site\finebytes\mfr\Help\`)
- Code: `RenameItemList.GenerateBatchRenamingFile`, `RenameItem.GenerateBatchApplyCommand`, `BasicPG` / `ExtendedPG.GenerateBatchApplyCommand`; UI `Main.cs` `mniBatch_Click`
- finebytes status: **none** (CSV/txt export only; Tools = Options + Reset)

### Behavior

- Purpose: Export pending Rename List name/path/attr changes as a runnable script without pressing GO.
- Options: none — SaveFileDialog only (`*.bat`).
- Target fields: names, locations, RAHS attrs. Not dates / ID3.
- Emit: `ren` same-folder; `if not exist … mkdir` + `move` on folder change; `attrib ±R±A±H±S` when attrs set.

### UX notes

- Menu: Tools → Generate Batch Renaming File; no shortcut; success MessageBox.

### Parity gaps / intentional diffs

- Dual format + UTF-8; post-rename attrib path; Tools label “Generate Rename Script…”; status-bar outcome instead of blocking MessageBox where the app already uses `LastStatusMessage`.

## Existing stubs (finebytes)

- Export siblings: `RenameList.ExportCsv` / `ExportNameList`; UI via `FileSavePicker` + Rename List export commands.
- Change detection: `RenameItem.HasPreviewChanges` / `IsPreviewPathUnchanged` — script export needs a **narrower** “path or RAHS attrs changed” predicate.
- Attrs: `FileAttributesRahs` (R/A/H/S).
- Tools menu: `MainWindow.axaml` Options + Reset only.

## Non-goals

- Replacing GO or CLI dry-run
- Scripting dates / tags
- `.sh`, undo `.bat`, CLI export flag (v1)
- Executing the generated script from MFR

## Phases

### P1 — Engine IR + bat/ps1 emitters

- **Scope / files:**
  - New under `Mfr.Engine/` (e.g. `RenameScript/`): op records (`RenameSameFolder`, `MoveWithParent`, `SetRahsAttributes`), collector from `IEnumerable<RenameItem>` / `RenameList`, `RenameScriptFormat` enum (`Bat`, `PowerShell`), formatters, `RenameList.ExportRenameScript(path, format)`.
  - Header comment with app name + finebytes URL (parity with MFR7 REM block).
  - RAHS: emit only bits that differ Original→Preview; apply to **preview** full path when path also changes.
- **Exit criteria:** Given Original/Preview pairs, bat and ps1 text match golden fixtures for rename-only, move+mkdir, attrs-only, rename+attrs (attrib/set on **new** path), PreviewError skip, tag-only skip.
- **Tests:** `Mfr.Tests/Engine/RenameScriptExportTests.cs` (pure string asserts; no disk apply).
- **Status:** Done

### P2 — Tools UI + save picker

- **Scope / files:**
  - Extend `FileSavePicker` to accept multiple `FilePickerFileType` choices (bat + ps1 + all).
  - `MainWindowViewModel` command → pick path → infer format from extension → `RenameList.ExportRenameScript` → `LastStatusMessage` (success / cancel / no scriptable changes / IO error).
  - `MainWindow.axaml` Tools item + `AppTips`.
  - Optional thin VM test; headless only if picker hooks already exist for export patterns.
- **Exit criteria:** Menu path writes `.bat` or `.ps1` from current list; cancel leaves list unchanged.
- **Tests:** VM or picker-hook test for format inference + status text; reuse engine fixtures.
- **Status:** Done

### P3 — Docs / backlog note

- **Scope / files:** Short note in `docs/` or design doc only if needed; mark parity item 11 addressed in Cursor backlog / future `mfr7-feature-parity.plan.md` when that doc is saved. No new user Help site required unless Help/About lands first.
- **Exit criteria:** Plan + parity backlog point at this feature as shipped modernized script export.
- **Tests:** none.
- **Status:** Done
