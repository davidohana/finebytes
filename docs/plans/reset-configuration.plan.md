---
name: Reset Configuration menu
overview: "Add top-level Tools → Reset Configuration (MFR7 behavior): confirm, delete the AppData files that replaced mfrconfig.xml, skip session save, restart the process."
todos:
  - id: confirm-dialog
    content: Add ConfirmMessageDialog (OK/Cancel) + headless tests
    status: completed
  - id: delete-apis
    content: Add delete helpers for config.json, session.json, filter-defaults.json
    status: completed
  - id: menu-flow
    content: "Top-level Tools → Reset Configuration: confirm, delete, suppress session save, restart"
    status: completed
  - id: docs-tests
    content: docs/plans + README note; store/UI tests without live process restart
    status: completed
isProject: false
---

# Reset Configuration menu

## MFR7 reference

- Menu (legacy): **MFR → Tools → Reset Configuration**
- Finebytes placement: **Tools** is a **top-level** menu bar item (sibling of MFR / File List / …), not nested under MFR
- Flow: OK/Cancel confirm → delete persisted files → suppress session save → restart process → shut down
- Named presets (`presets.json`) are left alone

## Files deleted

| Delete                 | Role                                                                     |
| ---------------------- | ------------------------------------------------------------------------ |
| `config.json`          | Process options (`ConfigStore`)                                          |
| `session.json`         | Window / File List / Rename List / Filter Editor chrome (`SessionStore`) |
| `filter-defaults.json` | Per-type add defaults (`FilterDefaultsStore`)                            |

## Implementation

- [`ConfirmMessageDialog`](../../Mfr.App.Ui/Views/ConfirmMessageDialog.axaml) — OK/Cancel; `ShowDialog<bool>`
- Shared footer chrome: [`Themes/MessageDialog.axaml`](../../Mfr.App.Ui/Themes/MessageDialog.axaml) (`message-dialog-footer`)
- [`PersistedConfigurationReset`](../../Mfr.Engine/Config/PersistedConfigurationReset.cs) — deletes the three AppData files
- [`MainWindow`](../../Mfr.App.Ui/Views/MainWindow.axaml): **Tools → Reset Configuration**; orchestration in code-behind
- [`MainWindowViewModel.ResetConfigurationCommand`](../../Mfr.App.Ui/ViewModels/MainWindowViewModel.cs) + `SuppressSessionSaveOnClose`
- Headless seams: [`ResetConfigurationHooks`](../../Mfr.App.Ui/Views/ResetConfigurationHooks.cs)
