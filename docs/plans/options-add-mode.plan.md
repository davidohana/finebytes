---
name: Options add mode
overview: "Add MFR7-style Rename List add policy to the Options dialog: Files / Folders / Files and folders radios plus Add folder contents, persisted in renameList and read from ConfigStore at add time (File List–style Options-owned prefs)."
todos:
  - id: p1-options-ui
    content: "P1: OptionsDialogViewModel + AXAML radios/checkbox + AppTips; Commit to EnsureRenameList; VM + headless Options tests"
    status: completed
  - id: p2-live-sync-docs
    content: "P2: MainWindow OK pushes AddMode/AddFolderContents to RenameListViewModel; docs/plans/options-add-mode.plan.md + amend options-dialog.plan.md; sync test"
    status: completed
  - id: deeper-configstore-owner
    content: "Deeper: ConfigStore-owned add policy at add time; CaptureSession omit + SaveOnClose merge; NotifyAddPolicyChanged on Options OK"
    status: completed
isProject: false
---

# Options add-mode UI

Parent: [options-dialog.plan.md](options-dialog.plan.md) (explicitly skipped add-mode; this supersedes that skip). Prefs live on [`RenameListPrefs`](../../Mfr.Models/Config/SessionPrefs.cs) (`addMode`, `addFolderContents`); add paths read them from [`ConfigStore`](../../Mfr.Models/Config/ConfigStore.cs) (same Options-owned pattern as File List double-click).

## Decisions (locked)

- **Placement:** Options dialog only (no Rename List toolbar / menu / on-the-fly heuristics).
- **Controls:** Radio trio bound to `RenameListAddMode` — **Files** / **Folders** / **Files and folders** — plus checkbox **Add folder contents** (not MFR7’s two coupled “Add files/Add folders” checks).
- **Persist:** `renameList.addMode` + `renameList.addFolderContents` via `ConfigStore.EnsureRenameList()` on OK; existing `ConfigStore.Save()` path unchanged.
- **Runtime owner:** Add Selected/All/drop read add policy from `ConfigStore` at call time (not VM fields). Options OK calls `NotifyAddPolicyChanged()` so can-execute refreshes.
- **Close-save:** `CaptureSession` omits add policy; `UiSessionPersistence.SaveOnClose` merges pane fields onto `EnsureRenameList()` and preserves Options-owned add fields (mirror File List double-click / remember).
- **Contents checkbox:** Always enabled (still meaningful in Files-only mode when a folder source is expanded).

## MFR7 reference brief

- **Sources:** Help `optionswin.html` / `optionswin1.gif`; `Core/MFRGui/Forms/Main/Options.cs` (`cbAddFiles` / `cbAddFolders` / `cbAddFolderContents`); add path reads `OptionsForm` from `RenameList.cs`.
- **Behavior:** Three General-tab checks; defaults files=on, folders=off, contents=on; unchecking folders forces files on and disables the files checkbox. Values gate include-files / include-folders / recurse when adding.
- **finebytes parity gap:** Same capability, clearer UX (enum radios + contents). Defaults already match (`Files` + `addFolderContents: true`).

## Non-goals

- Rename List toolbar toggles or duplicate controls elsewhere
- On-the-fly / heuristic add policy
- CLI `/F` `/D` `/R` switches (MFR7 `ProcessCML`)
- Changing engine add semantics or `RenameListAddMode` shape

## Flow

```mermaid
flowchart LR
  Open["Options open"] --> Draft["OptionsDialogViewModel drafts AddMode + AddFolderContents"]
  Draft --> Ok["OK"]
  Ok --> Store["EnsureRenameList + ConfigStore.Save"]
  Ok --> Notify["NotifyAddPolicyChanged"]
  Store --> Add["Add Selected / All / drop reads ConfigStore"]
  Notify --> CanExec["Add can-execute refresh"]
```

## Phases

### P1 — Options draft + UI + Commit store

- **Scope:** [`OptionsDialogViewModel.cs`](../../Mfr.App.Ui/ViewModels/Options/OptionsDialogViewModel.cs) — load/commit `AddMode` + `AddFolderContents` from/to `ConfigStore.EnsureRenameList()`. [`OptionsDialog.axaml`](../../Mfr.App.Ui/Views/Options/OptionsDialog.axaml) — label row + three `CompactRadioButton`s (`EnumToBooleanConverter`) + `CompactCheckBox` for contents. [`AppTips.cs`](../../Mfr.App.Ui/Resources/AppTips.cs) tips for each control.
- **Exit:** Dialog shows and round-trips prefs in memory via `Commit()`.
- **Tests:** Extend [`OptionsDialogViewModelTests`](../../Mfr.Tests/Ui/Options/OptionsDialogViewModelTests.cs); headless label/tip asserts in [`OptionsDialogTests`](../../Mfr.Tests/Ui/Options/OptionsDialogTests.cs).
- **Status:** Completed (`f6b72177`).

### P2 — Live Rename List sync + docs

- **Scope (superseded by deeper follow-up):** Originally pushed VM fields after Commit; now add paths read `ConfigStore` and Options OK only notifies can-execute. Plan file + [options-dialog.plan.md](options-dialog.plan.md) skip amendment remain.
- **Exit:** Changing Options and adding in the same session uses the new policy without restart; close-save keeps Options values via merge.
- **Status:** Completed (`64a49a9d`); deeper ownership follow-up below.

### Deeper — ConfigStore-owned add policy

- **Scope:** Drop `RenameListViewModel` AddMode/AddFolderContents observables; read `ConfigStore` in add/can-execute; omit from `CaptureSession`; merge in `SaveOnClose`; `NotifyAddPolicyChanged` on Options OK.
- **Status:** Completed (this follow-up).

## Key reuse

- `EnumToBooleanConverter` + `CompactRadioButton` / `CompactCheckBox` patterns already in Options.
- File List Options-owned close-save merge pattern for double-click / remember.
- No new persisted schema — UI + ownership alignment only.
