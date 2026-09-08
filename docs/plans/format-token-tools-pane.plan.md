---
name: Format token tools pane
overview: "Collapsible FormatTokenToolsHost beside format-capable filter editors (default open): shared Insert catalog + Edit targeting the last-focused FormatEditor; per-field chrome hidden under the host."
todos:
  - id: focus-scope
    content: LastActive FormatEditor tracking + active-field visual cue
    status: completed
  - id: tools-pane
    content: FormatTokenToolsHost + FormatTokenInsertPicker wired into format-capable editors
    status: completed
  - id: slim-chrome
    content: Hide per-field Insert/Edit under host; Edit on collapse rail; standalone chrome kept
    status: completed
  - id: tests-docs
    content: Headless host tests + this plan doc
    status: completed
isProject: false
---

# Format token tools pane

Replaces duplicated per-field Insert flyouts with a shared collapsible tools strip inside each format-capable filter editor. Design notes from the Cursor recommendation; MFR7 per-field Insert+Edit chrome is intentionally departed from under the host.

## Shape

- [`FormatTokenToolsHost`](../../Mfr.App.Ui/Views/FormatEditor/FormatTokenToolsHost.cs) — options via child `Content`, tools on the right (default **open**); ControlTheme in [`Themes/FormatTokenToolsHost.axaml`](../../Mfr.App.Ui/Themes/FormatTokenToolsHost.axaml).
- [`FormatTokenInsertPicker`](../../Mfr.App.Ui/Views/FormatEditor/FormatTokenInsertPicker.axaml) — shared search + catalog tree (also used by the standalone Insert flyout); data context is [`FormatTokenInsertPickerViewModel`](../../Mfr.App.Ui/ViewModels/FormatEditor/FormatTokenInsertPickerViewModel.cs).
- Insert + Edit on the pane target the **last-focused** descendant [`FormatEditor`](../../Mfr.App.Ui/Views/FormatEditor/FormatEditor.axaml) (defaults to the **first** registered field); active field gets `format-editor-active-target` cue. Host Edit is a click handler on the active field (not the picker VM).
- Under a host, `ShowInsertButton` / `ShowEditButton` are cleared on register; **Edit stays on the collapse rail** when the catalog is hidden.
- Standalone / nested `FormatEditor` (no host) keeps local Insert+Edit.
- Collapse state is **not** persisted in session (v1).

## Wired editors

Formatter, Inserter, Name List, PathMover, Audio Tag Setter, ID3v2 Field Setter.

## Tests

[`FormatTokenToolsHostViewTests`](../../Mfr.Tests/Ui/FormatEditor/FormatTokenToolsHostViewTests.cs) — hide chrome, last-focus insert, collapsed Edit affordance, standalone chrome.
