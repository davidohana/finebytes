# Rename List GO (Phase 15) plan

Parent: [docs/plans/rename-list-ui.plan.md](rename-list-ui.plan.md) § Phase 15 — GO.

## Decisions (locked)

- **CommitError survives Preview** — Change [`RenameItem.ResetState`](../../Mfr.Models/Rename/RenameItem.cs) so it does **not** clear `CommitError` / `RenameStatus.CommitError` (MFR7 keeps `ApplyError` across post-GO re-preview). Add explicit `ClearCommitErrors()` on the list (or per-item); call it at **GO start** and on **F5** `RefreshOriginals` (with override clear already there).
- **Ensure preview** — GO always runs `Preview` once after clearing commit errors, then `Commit` with that plan (KISS; avoids caching stale plans). No Contents/`IsPreview=false` path. User may see Preview progress dialog before Commit progress on large lists.
- **Preview-error warn** — If `plan.ErrorCount > 0`, show [`ConfirmMessageDialog`](../../Mfr.App.Ui/Views/ConfirmMessageDialog.axaml.cs): *“N items with preview errors will be ignored. Do you want to continue?”* OK=continue, Cancel/Escape=abort (default No). Engine already skips `PreviewError` rows.
- **failFast** — GUI `failFast: false` (continue after per-item failures). Cancel via progress token stops remaining work.
- **Progress** — Extend engine `Commit` with `IProgress` + `CancellationToken` (mirror Preview/Refresh). UI: new `RenameListProgressOperation.Commit` + copy; GO may run Preview then Commit as two sequential `_RunProgressAsync` calls.
- **Post-GO refresh** — After Commit: `_RefreshFieldDisplay`; if Auto-Preview on, allow normal re-preview (plum survives via ResetState change). Do **not** clear commit errors except GO start / F5.
- **Post-apply status** — After Commit, set `LastGoStatus` for the main status bar (success and/or error counts; errors hint to right-click **Show Rename Error**). No summary dialog.
- **Go CanExecute** — Enabled when rename list has ≥1 row and `!IsBusy` (empty list no-ops; no ChangeCount gate).
- **Plum vs lavender** — Row class: `HasCommitError` → plum; else `HasPreviewError` → lavender.
- **Out of scope** — Undo Log, trial, Contents warn, **Show Last Rename Errors** (note in [docs/debts.md](../debts.md)).

## MFR7 reference brief

### Sources

- Help: `applychanges.html`, `renamelist.html` (Highlighting / Show Rename Error), `statusbar.html`
- Hints: `hints.txt` `#GO`
- Code: `Shld.MainForm_GoRequested`; `RenameItemList.Apply` / `ClearApplyErrors`; `ApplyProgressDialog`; `RenameGrid` plum; `PropStatus.ForceValue` PreviewStart/End
- finebytes: engine Commit done; UI `Go()` stub

### Behavior

1. Clear apply errors → ensure preview → warn if preview-error count → apply with progress/Stop → plum rows → status-bar outcome → Show Rename Error on row.
1. Preview-error rows skipped; Forced/manual overrides apply only via last Preview snapshot (finebytes PreviewStart/End already).
1. Ctrl+G / menu **GO** / toolbar **GO!**; F5 clears apply-error highlight.

### Parity gaps (intentional)

- Always Preview on GO (vs MFR7 skip when Auto-Preview on).
- No Undo Log / Show Last Rename Errors in this plan.
- Progress uses existing Rename List progress dialog, not MFR7 `ApplyProgressDialog` chrome.

## Non-goals

- Phase 16 color legend; 14f drag-out; Undo/Log/Options stubs; session persistence of commit errors; new error dialog type.

## Phases

### P1 — Commit-error lifetime (engine)

- [x] **Status:** done

**Scope / files**

- [`Mfr.Models/Rename/RenameItem.cs`](../../Mfr.Models/Rename/RenameItem.cs) — `ResetState` keep `CommitError`; add clear helper if needed
- [`Mfr.Engine/RenameList/RenameList.cs`](../../Mfr.Engine/RenameList/RenameList.cs) — `ClearCommitErrors()`; call from `RefreshOriginals`
- Engine tests: Preview after CommitError preserves error; F5 clears it

**Exit:** Post-Preview plum data still on item; F5 clears; GO start can clear via API.

### P2 — Commit progress + cancel (engine)

- [x] **Status:** done

**Scope / files**

- [`CommitExecutor`](../../Mfr.Engine/Commit/CommitExecutor.cs) / [`RenameList.Commit`](../../Mfr.Engine/RenameList/RenameList.cs) — `CancellationToken` + `IProgress<RenameListProgress>`
- [`RenameListProgressPhase`](../../Mfr.Engine/RenameList/RenameListProgressPhase.cs) — e.g. `ApplyCommit`
- [`RenameListProgressTracker`](../../Mfr.Engine/RenameList/RenameListProgressTracker.cs) — begin/report helpers
- Extend existing commit tests for cancel mid-run / progress reports

**Exit:** Commit reportable and stoppable like Preview; `failFast: false` unchanged.

### P3 — Plum + Show Rename Error (UI)

- [x] **Status:** done

**Scope / files**

- [`RenameListEntry`](../../Mfr.App.Ui/ViewModels/RenameList/RenameListEntry.cs) — `HasCommitError`
- [`RenameListView.axaml(.cs)`](../../Mfr.App.Ui/Views/RenameList/RenameListView.axaml) + [`Themes/RenameList.axaml`](../../Mfr.App.Ui/Themes/RenameList.axaml) — `rename-list-commit-error`, MFR7 Plum brushes (light/dark)
- New `RenameListViewModel.CommitErrors.cs` + `RenameListCommitErrorDisplay` → existing dialog; extend `CanShowRowErrorMenu`
- Row menu item **Show Rename Error** (with Preview / Load error items)
- VM + headless tests (class, menu visibility, dialog content)

**Exit:** Synthetic/`CommitError` rows show plum; menu opens shared dialog.

### P4 — Wire GO (UI + docs)

- [x] **Status:** done

**Scope / files**

- [`MainWindowViewModel.Go`](../../Mfr.App.Ui/ViewModels/MainWindow/MainWindowViewModel.cs) — real `CanExecute`; async orchestration (wait pending Auto-Preview drain if any, then `RenameListViewModel` commit-go)
- New `RenameListViewModel.Commit.cs` (or extend Preview partial): clear errors → Preview → confirm → Commit → refresh → `LastGoStatus` status bar
- Dialog events (preview-error confirm) mirrored like `RowErrorDialogRequested` from view code-behind
- [`RenameListProgressOperation`](../../Mfr.App.Ui/ViewModels/RenameList/RenameListProgressOperation.cs) + [`RenameListProgressCopy`](../../Mfr.App.Ui/ViewModels/RenameList/RenameListProgressCopy.cs) — Commit arm
- [`docs/keyboard-shortcuts.md`](../keyboard-shortcuts.md) — remove GO from stubs sentence
- [`docs/debts.md`](../debts.md) — Show Last Rename Errors deferred
- Mark parent Phase 15 done when exiting
- Tests: CanExecute; warn abort; commit called; overrides survive into commit (engine assert OK); F5 clears plum; progress op; success/error status text

**Exit:** Ctrl+G / menu / toolbar perform real renames; plum + Show Rename Error; preview-error rows skipped after warn; status-bar GO outcome; shortcuts doc updated.

## Test focus

- Engine: CommitError × Preview; Clear on Refresh; Commit cancel/progress; override original/preview → CommitOk path
- UI VM: Go enable rules; confirm cancel; Show Rename Error
- Headless: row plum class; menu item present (skill `mfr-ui-headless-tests`)
