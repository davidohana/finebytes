---
name: Options dialog
overview: "Ship a minimal Options modal (MFR → Options / Ctrl+,) with three settings that already exist but have no UI: remember last folder, remember window state, and confirm-replace on preset load. Defer MFR7 add-mode duplicates, Explorer shell, double-click-to-add, and Undo/Log until those features exist."
todos:
  - id: p1-config-save
    content: "P1: ConfigStore.Save + round-trip test"
    status: completed
  - id: p2-options-dialog
    content: "P2: OptionsDialog ViewModel + AXAML (3 checkboxes)"
    status: pending
  - id: p3-wire-showoptions
    content: "P3: Enable ShowOptions, host dialog, persist on OK, update docs"
    status: pending
  - id: p4-tests
    content: "P4: VM + headless coverage; shortcuts no longer list Options as stub"
    status: pending
isProject: false
---

# Options dialog plan

Parent: [presets-ui.plan.md](presets-ui.plan.md) (confirm flag was out of scope until Options UI); stub in [keyboard-shortcuts.md](../keyboard-shortcuts.md).

## Decisions (locked)

- **v1 controls only** (all already persisted + used; no new domain settings):
  1. **Save File List last position** → `SessionState.FileList.RememberLastFolder` (MFR7 `SaveFileListPos`)
  1. **Remember window size and position** → `SessionState.MainWindow.RememberWindowState` (finebytes-only; no MFR7 twin)
  1. **Confirm before replacing Applied Filters on preset load** → `MfrConfig.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad` (`config.json`)
- **Single-pane dialog** (no General / Undo & Log tabs) — three checkboxes + OK/Cancel. Tabs when Log/Undo options return.
- **Do not duplicate** Rename List add-mode / add-folder-contents (already in Rename List UI / session).
- **Persist on OK**: mutate live `Session` + `ConfigStore.Config`, then **write `config.json`** (new `ConfigStore` save). Session flags ride the existing close-save path (`UiSessionPersistence`).
- **Shortcut stays Ctrl+,** (finebytes); MFR7 Ctrl+T is not ported.

## MFR7 reference brief

### Sources

- Help: `Help/optionswin.html` (+ `optionswin1.gif` / `optionswin2.gif`); related `resetconfig.html`, `log.html`
- Code: `D:\Devl\mfr7\Core\MFRGui\Forms\Main\Options.cs` (+ `.resx`); open from `Main.cs` (`mniOptions` / `btnOptions`); persist `OptionsForm.LoadConfig`/`SaveConfig` → `mfrconfig.xml`
- finebytes status: **no Options UI** — `ShowOptions` stub (`_CanExecuteUnimplemented`); add-mode + remember-folder in **session**; confirm-replace + log retention in **hand-edit `config.json`**

### Behavior

- Purpose: app-wide prefs (add-from-File-List, restore File List path, optional shell integrate, double-click-to-add, rename-log retention). Not Filter Options.
- MFR7 tabs:
  - **General:** Add files / Add folders / Add folder contents; Save File List last position; Integrate Explorer (registry, admin); Double-click adds to Rename List
  - **Undo & Log:** LogLimit radios (0 / N / unlimited) for `.mfrlog` undo history (default limited to 10)
- Apply: in-memory on OK; XML on exit. Shell checkbox applies immediately. No Apply button.

### UX notes

- Title **Options**; MFR → Options + toolbar; OK/Cancel; Escape = Cancel; ~fixed dialog, center parent.

### Parity gaps / intentional diffs (v1)

| MFR7                         | finebytes v1                                                    |
| ---------------------------- | --------------------------------------------------------------- |
| Add files/folders/contents   | **Skip** — Rename List toolbar/session                          |
| Save File List last position | **Ship** as Remember last folder                                |
| Explorer shell integrate     | **Defer** (admin/registry)                                      |
| Double-click to add          | **Defer** (File List always opens with default app today)       |
| Undo & Log tab               | **Defer** (Undo/Log stubs; session diagnostic logs ≠ `.mfrlog`) |
| —                            | **Ship** Remember window state + Confirm replace on preset load |

## Non-goals

- Explorer context-menu integration
- Double-click-to-add preference
- Undo & Log retention UI / renaming `.mfrlog` model
- Exposing `filters.maxListFileLineLength` or `log.*` templates (hand-edit / CLI `--set` remains)
- Moving add-mode out of Rename List into Options
- Changing Reset Configuration (already Tools → Reset)

## Architecture

```mermaid
flowchart LR
  Menu["MFR Options / Ctrl+,"] --> Show["ShowOptions"]
  Show --> Dlg["OptionsDialog + VM"]
  Dlg -->|OK| Sess["Session remember flags"]
  Dlg -->|OK| Cfg["ConfigStore.Config + Save"]
  Sess --> Close["UiSessionPersistence on close"]
  Cfg --> Json["config.json"]
  Cfg --> Presets["AppliedFilters confirm on load"]
```

Pattern: modal Avalonia `Window` like [ExcludeMasksDialog](../../Mfr.App.Ui/Views/FileList/ExcludeMasksDialog.axaml) / Filter Options — `ShowInTaskbar=False`, `CenterOwner`, `ModalDialogKeyboard`, `ShowDialog<bool?>`, apply draft only on `true`.

Wire via event/hooks from [MainWindowViewModel](../../Mfr.App.Ui/ViewModels/MainWindow/MainWindowViewModel.cs) (same style as `ResetConfigurationRequested`) so Views host the dialog.

## Phases

### P1 — `ConfigStore.Save`

- **Scope:** [`ConfigStore.cs`](../../Mfr.Models/Config/ConfigStore.cs) — add `Save(string? path = null)` that always writes current `Config` via existing `ConfigJsonWriter` (overwrite; create dir). Keep `EnsureDefaultFile` as write-if-missing for startup. Update XML remarks (Options UI can persist).
- **Exit:** unit test round-trips a mutated `ConfirmReplace…` (and optionally another leaf) through Save → Load.
- **Tests:** `Mfr.Tests` ConfigStore / binding area.

### P2 — Options dialog UI + VM

- **Scope:**
  - `Mfr.App.Ui/ViewModels/Options/OptionsDialogViewModel.cs` — draft bools from session + `ConfigStore.Config`; OK commits.
  - `Mfr.App.Ui/Views/Options/OptionsDialog.axaml` (+ code-behind) — three `CompactCheckBox`es, OK/Cancel footer.
  - Labels (MFR7-ish): “Save File List last position”, “Remember window size and position”, “Confirm before replacing Applied Filters when loading a preset”.
- **Exit:** dialog builds; Cancel discards; OK updates memory correctly in VM tests.

### P3 — Wire `ShowOptions`

- **Scope:** Enable `ShowOptionsCommand` (drop `_CanExecuteUnimplemented` for Options only). Host open in `MainWindow.axaml.cs` (or small hooks type). On OK: set session nested flags; set `Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad`; call `ConfigStore.Save()`. Update [`docs/keyboard-shortcuts.md`](../keyboard-shortcuts.md) (Options no longer stub). Touch [`MfrConfig`](../../Mfr.Models/Config/MfrConfig.cs) remarks (“Options dialog”). Amend presets-ui out-of-scope bullet / link this plan. Optional one-liner in [`docs/debts.md`](../debts.md) for deferred Options items (shell, double-click, Undo/Log tab).
- **Exit:** menu/toolbar/Ctrl+, opens dialog; confirm flag affects preset load without hand-editing JSON; remember flags survive restart via session save.

### P4 — Tests

- **Scope:** VM tests for load/commit/cancel; headless smoke that Options command is enabled and dialog can open (follow `mfr-ui-headless-tests` / existing MainWindow smoke). Session persistence coverage for the two remember flags if gaps exist.
- **Exit:** `just test` green; Options removed from “stubs” wording in shortcuts doc.

## Key files

| Area            | Path                                                               |
| --------------- | ------------------------------------------------------------------ |
| Stub today      | `MainWindowViewModel.ShowOptions`, `MainWindow.axaml` menu/toolbar |
| Config          | `MfrConfig` / `ConfigStore` / `ConfigJsonWriter`                   |
| Session         | `SessionState`, `UiSessionPersistence`                             |
| Consumer        | `AppliedFiltersView.Presets.cs` (`NeedsConfirmReplaceOnLoad`)      |
| Sibling pattern | `ExcludeMasksDialog*`, `FilterOptionsDialog*`                      |
