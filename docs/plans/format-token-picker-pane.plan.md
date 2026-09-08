---
name: Format token picker pane
overview: "Collapsible FormatTokenPickerPane beside format-capable filter editors (default open): shared Insert catalog + Edit targeting the last-focused FormatEditor; per-field chrome hidden under the pane; collapse persisted in session.json."
todos:
  - id: focus-scope
    content: LastActive FormatEditor tracking + active-field visual cue
    status: completed
  - id: picker-pane
    content: FormatTokenPickerPane + FormatTokenPicker wired into format-capable editors
    status: completed
  - id: slim-chrome
    content: Hide per-field Insert/Edit under pane; Edit on collapse rail; standalone chrome kept
    status: completed
  - id: tests-docs
    content: Headless pane tests + this plan doc
    status: completed
  - id: persist-collapse
    content: Persist IsExpanded in session.json (filterEditor.formatTokenPickerExpanded)
    status: completed
isProject: false
---

# Format token picker pane

Replaces duplicated per-field Insert flyouts with a shared collapsible picker strip inside each format-capable filter editor. Design notes from the Cursor recommendation; MFR7 per-field Insert+Edit chrome is intentionally departed from under the pane.

## Shape

- [`FormatTokenPickerPane`](../../Mfr.App.Ui/Views/FormatEditor/FormatTokenPickerPane.cs) — options via child `Content`, picker on the right (default **open**); ControlTheme in [`Themes/FormatTokenPickerPane.axaml`](../../Mfr.App.Ui/Themes/FormatTokenPickerPane.axaml).
- [`FormatTokenPicker`](../../Mfr.App.Ui/Views/FormatEditor/FormatTokenPicker.axaml) — shared search + catalog tree (also used by the standalone Insert flyout); data context is [`FormatTokenPickerViewModel`](../../Mfr.App.Ui/ViewModels/FormatEditor/FormatTokenPickerViewModel.cs).
- Insert + Edit on the pane target the **last-focused** descendant [`FormatEditor`](../../Mfr.App.Ui/Views/FormatEditor/FormatEditor.axaml) (defaults to the **first** registered field); active field gets `format-editor-active-target` cue. Pane Edit is a click handler on the active field (not the picker VM).
- Under a pane, `ShowInsertButton` / `ShowEditButton` are cleared on register; **Edit stays on the collapse rail** when the catalog is hidden.
- Standalone / nested `FormatEditor` (no pane) keeps local Insert+Edit.
- Collapse state is **shared** across format-capable editors via [`FilterEditorViewModel.FormatTokenPickerExpanded`](../../Mfr.App.Ui/ViewModels/FilterEditors/FilterEditorViewModel.cs) (persisted as `filterEditor.formatTokenPickerExpanded` in `session.json`). Option editors two-way bind [`FormatTokenPickerPane.IsExpanded`](../../Mfr.App.Ui/Views/FormatEditor/FormatTokenPickerPane.cs); the pane VM writes through on change. Missing section → expanded.

## Wired editors

Formatter, Inserter, Name List, PathMover, Audio Tag Setter, ID3v2 Field Setter.

## Out of scope for a first cut

These stay deferred so the pane stays an explicit per-editor wrap, not a new Filter Configuration shell or a second inventing of format UX.

- **Main-window global dock / `FilterEditorView` auto-injection.** Do not put one shared token dock on the main window, and do not teach `FilterEditorView` to scan option content for `FormatEditor` children and inject the pane. Each format-capable options view wraps itself in `FormatTokenPickerPane` explicitly. A shell-level dock would need global layout modes, cross-editor focus rules when Applied selection swaps content, and “does this editor need tokens?” heuristics — all avoided by opt-in wrapping.
- **Autocomplete-on-`<` or inline expanders.** Typing `<` must not open chip/autocomplete UI, and token param editors stay modal dialogs (not inline expanders under the caret). Both were already cut in F6 as overkill / low ROI ([`formatter-formateditor-ux.plan.md`](formatter-formateditor-ux.plan.md)); the picker pane does not reopen that design. Insert remains catalog browse/search; customize remains Edit / right-click afterward.
- **Migrating Genre / plain-language fields into `FormatEditor`.** Audio Tag Setter **Genre** stays an editable ComboBox (ID3v1 suggestions); other non-template fields stay plain `TextBox` / combo controls. The pane only targets registered `FormatEditor`s — when focus is on Genre or plain language, Insert/Edit stay disabled (“Click a format field”). Converting those rows to `FormatEditor` just to share Insert would drop genre suggestions and blur free-text vs format-string UX; that is a separate product call, not part of this pane.

## Tests

[`FormatTokenPickerPaneViewTests`](../../Mfr.Tests/Ui/FormatEditor/FormatTokenPickerPaneViewTests.cs) — hide chrome, last-focus insert, collapsed Edit affordance, standalone chrome, session restore/share via Filter Configuration.
[`FilterEditorViewModelTests`](../../Mfr.Tests/Ui/FilterEditors/FilterEditorViewModelTests.cs) — ApplySession / write-through / options-editor sync for `FormatTokenPickerExpanded`.
