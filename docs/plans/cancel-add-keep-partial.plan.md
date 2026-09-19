# Cancel Add — keep already-added

## Decisions (locked)

- **Scope:** Rename List **Add** only (Add Selected / Add All / drop / startup). Preview / commit / refresh / metadata cancel stay as today.
- **UX:** Two buttons on the Add progress dialog:
  - **Cancel** (Esc / `IsCancel`) — discard staging batch (current behavior, MFR7 parity).
  - **Keep added** — stop resolving and **insert** whatever is already in the staging batch.
- **Meaning of “already added”:** items collected in the engine staging `batch` (dialog “Added N”), not rows already in the live list. Pre-existing list rows are never touched.
- **Mid-metadata cancel + Keep:** insert the batch as-is; rows hydrated so far keep metadata, remaining stay lazy (same as incomplete resolve).
- **Unexpected exception during add:** still discard (do not treat as Keep).
- **Not in scope:** changing Cancel to always keep; File List browse cancel; confirmation MessageBox after Cancel.

## MFR7 reference brief

- **Sources:** `AdderProgressDialog.cs`, `RenameList.GenerateAdder` / `AddPaths`, `AdderLauncher.Stop`; Help `renamelist.html` (no cancel-add docs).
- **Behavior:** Cancel discards the whole staging batch; never inserts; no keep-partial option.
- **UX:** Single **&Cancel**; Esc = Cancel.
- **Parity:** finebytes already matches discard-on-Cancel. **Keep added** is intentional new UX beyond MFR7.

## Non-goals

- Incremental live insert during the walk (rows appear one-by-one while dialog is open).
- Keep option on non-Add progress ops.
- Persisting a preference for default cancel disposition.

## Approach

### Engine — commit staging on Keep

In [`Mfr.Engine/RenameList/RenameList.cs`](../../Mfr.Engine/RenameList/RenameList.cs) `AddSources`:

- Add a cancel-disposition flag the UI can set **at cancel time** (mutable holder passed into `AddSources`, e.g. `RenameListAddCancelDisposition` with `KeepPartial`), default discard.
- After resolve (and after optional metadata), if canceled **and** `KeepPartial` and `batch.Count > 0`: `_InsertCollectedItems` (same as success).
- Else if canceled / failed: `_DiscardCollectedItems` (unchanged).
- Skip metadata when already canceled **unless** Keep was requested mid-resolve — Keep after resolve-only cancel inserts without forcing a full metadata pass; if cancel hits mid-metadata and Keep is set, insert after the hydrate loop breaks (partial OK).
- Extend [`RenameListAddSummary`](../../Mfr.Engine/RenameList/) with `WasCanceled` / `KeptPartial` (or equivalent) so status can say e.g. “Added N (stopped)”.

### Progress VM + dialog

- [`RenameListProgressViewModel`](../../Mfr.App.Ui/ViewModels/RenameList/RenameListProgressViewModel.cs): `KeepAddedCommand` (Add-only `CanExecute`); sets disposition to Keep then `_cts.Cancel()`.
- Change `RunAsync` result from `bool` to a small enum/struct: `Completed` | `Canceled` | `CanceledKeep` (other ops only use Completed/Canceled).
- [`RenameListProgressDialog.axaml`](../../Mfr.App.Ui/Views/RenameList/RenameListProgressDialog.axaml): bottom bar with **Keep added** (visible when `Operation == Add`) + **Cancel**.
- [`_RunProgressAsync`](../../Mfr.App.Ui/ViewModels/RenameList/RenameListViewModel.Progress.cs): plumb result; invoke discard `onCancel` only for plain `Canceled`, not `CanceledKeep`.

### Add VM wiring

In [`RenameListViewModel.Add.cs`](../../Mfr.App.Ui/ViewModels/RenameList/RenameListViewModel.Add.cs):

- Pass disposition holder into `AddSources`.
- On `CanceledKeep`: `_SyncEntriesAfterAdd` + sort/selection/status like success (with stopped wording); **no** `_RollbackAddedItems`.
- On `Canceled`: keep today’s `OnAddCanceled` discard path.

### Tests + help

- Flip/extend engine tests that assert discard-only; add Keep cases (resolve mid-walk, metadata mid-hydrate).
- UI: `AddSelected_Cancel_Discards…` stays; add Keep keeps Entries / ItemCount.
- Progress VM: Keep returns `CanceledKeep`; Cancel returns `Canceled`.
- Help: `help/intro/whatsnew.html` + rename-list progress mention if a matching guide page exists.

## Phases

### P1 — Engine Keep disposition

- Scope: `AddSources`, cancel disposition type, `RenameListAddSummary`, engine tests in `RenameListTests.cs`.
- Exit: Cancel+discard still empty; Cancel+Keep inserts staging; metadata mid-cancel + Keep inserts partial hydrate.
- Tests: keep-partial + existing discard cases still green.
- Status: Done

### P2 — Progress dialog + Add VM + help

- Scope: Progress VM/`RunAsync` result, AXAML buttons, `_RunProgressAsync`, `RenameListViewModel.Add.cs`, VM tests, help whatsnew.
- Exit: Cancel discards; Keep added stops and list shows resolved items; Esc still discard; non-Add dialogs unchanged (single Cancel).
- Tests: discard + keep UI tests; progress result enum coverage.
- Status: Done
