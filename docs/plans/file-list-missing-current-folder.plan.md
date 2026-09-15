# File List missing-current-folder UX

Parent / related: [file-list-cut-copy-paste-delete.plan.md](file-list-cut-copy-paste-delete.plan.md) (Paste/shell status); Options remember-last-folder in [options-dialog.plan.md](options-dialog.plan.md).

## Decisions (locked)

- Stay on the dead path after list failure (`NotFound` / other `ListingError`); do not auto-navigate.
- Empty list + existing in-pane message; add recovery buttons: **Go Up** (when parent/climb exists) and **This PC** (Windows `ComputerPath`) / equivalent computer root on non-Windows.
- Gate destination verbs while `HasListingError`: Paste and Show-in-Explorer-for-current-folder (`SelectedEntry is null`). Selection Cut/Copy/Delete already need rows (empty on error).
- Recovery buttons show for **all** `ListingError` kinds (not only `NotFound`).
- **Go Up** climbs to the first existing ancestor (or computer/network sentinel), not only the immediate parent.
- Typed missing path: keep current non-navigate behavior; set a one-line `LastStatusMessage` error.
- Startup: keep `ResolveStartPath` fallback; when remembered path was unusable, set one **warning** status (`Last folder unavailable — opened …`) and seed main `StatusHint` after PropertyChanged is wired (ctor status alone is missed).
- Close-save: keep `_IsPersistableFolder` (`Directory.Exists`); no schema change. Do not clear prefs beyond that.
- Non-goals: `FileSystemWatcher`, refresh-on-focus, New Folder (does not exist), Rename List–style per-row missing gray, toast spam.

## MFR7 reference brief

- Sources: `D:\Devl\mfr7\Core\MFRGui\Forms\FileList\MFRExplorer.cs` (embedded Gong ShellView).
- Saved path: `Options/SaveFileListPos` → `LoadPathConfig()`; on failure logs only (`Cannot navigate to saved config path`) — no user-facing copy; shell stays at default.
- Refresh: `RefreshContents()` — shell owns missing-folder UX.
- Parity gap: finebytes uses custom listing + `ListingError`; this plan adds explicit recovery and status (better than MFR7 silent restore).

## Current hooks

- VM: `Mfr.App.Ui/ViewModels/FileList/FileListViewModel.cs` — `ListingError`, `_ApplyListingResult`, `GoUp`, `_CanPaste`, `_CanShowInExplorer`, `CaptureSession`, `LastStatusMessage`.
- Catalog: `Mfr.App.Ui/Services/FileList/FileListCatalog.cs` — `FormatListingError`, `ResolveStartPath`, `TryResolvePath`.
- Path: `Mfr.App.Ui/Services/FileList/FileListPath.cs` — `GetParentPath`.
- Overlay: `Mfr.App.Ui/Views/FileList/FileListView.axaml`.
- Persist: `Mfr.App.Ui/Services/Session/UiSessionPersistence.cs` — `_IsPersistableFolder`.

## Phases

### P1 — Gate commands on listing health

- Scope / files: `FileListViewModel` — treat destination gate as filesystem path **and** `!HasListingError`. Notify Paste / ShowInExplorer CanExecute when `ListingError` changes.
- Exit: Paste and empty-selection Show-in-Explorer disabled while overlay shows; re-enabled after navigating to a good folder.
- Tests: VM — after simulated `NotFound` listing, `PasteCommand.CanExecute` false; after navigate away, true when clipboard pasteable.
- Status: done (SHA `0d8d65d9`, review deferred)

### P2 — Recovery overlay + Go Up climb

- Scope / files:
  - `FileListPath` (or helper): `TryGetFirstExistingAncestor(path)` walking `GetParentPath` until `Directory.Exists` or a sentinel.
  - `GoUp()` uses that climb result.
  - Overlay AXAML: **Go Up** + **This PC** buttons; keep Show-log button.
- Exit: From a deleted leaf (parent also deleted), one Go Up lands on first live ancestor; overlay offers both recovery actions.
- Tests: path helper unit tests; VM Go Up from nested deleted tree; optional headless button visibility.
- Status: done (SHA `6a6ac9b7`, reviewed)

### P3 — Status for typed miss + startup fallback

- Scope / files:
  - `CommitPath` / `_Navigate`: when resolve fails, set `LastStatusMessage` error; do not change `CurrentPath`.
  - `ResolveStartPath`: signal fallback; VM sets one neutral status when remembered path was unusable.
- Exit: Status for bad address commit and cold start on stale `LastOpenedDirectory`; no prefs schema change.
- Tests: extend `CommitPath_Ignores_Missing_Directory`; startup/fallback status test.
- Status: done (SHA `3ec8be4f`, reviewed)

### P4 — Docs touch (minimal)

- Scope: write this plan under `docs/plans/`; optional one-liner in `docs/debts.md` for watcher/focus refresh. Skip keyboard-shortcuts unless a tip already covers listing errors.
- Exit: plan file present; debts optional one-liner.
- Status: done

## Implementation notes

- Prefer early returns; private methods `_`-prefixed.
- Reuse `FileListListingFailure.NotFound` messaging; do not invent a second missing-folder state machine.
- `CaptureSession` may still emit a dead `LastOpenedDirectory`; close-save already skips non-existent paths — leave that contract; startup status covers the restore gap.

## Deferred deeper work (from reviews)

- ~~Share Catalog’s timed `Directory.Exists` with `TryGetFirstExistingAncestor`~~ — done (`FileListIo`).
- ~~Sticky listing error until success~~ — done (clear on success; `ShowListingError` hides while `IsListing`).
- Optional later: `FileSystemWatcher` / refresh-on-focus (see `docs/debts.md`).
