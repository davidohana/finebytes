---
name: Options dialog tabs
overview: Declutter Options with UI / Rename / Misc tabs, chrome-sized tab captions, OK/Cancel drafting unchanged, plus headless tests and help screenshot.
todos:
  - id: axaml-tabs
    content: Refactor OptionsDialog.axaml to UI/Rename/Misc TabControl; fixed height; chrome-sized tab captions
    status: completed
  - id: headless-tests
    content: Update OptionsDialogTests for UI/Rename/Misc tab selection and fieldset coverage
    status: completed
  - id: help-screenshot
    content: Update optionswin.html for three tabs; recapture options.png
    status: completed
---

# Options dialog tabs

## Approach

Three tabs grouped by concern (not MFR7’s General / Undo split):

| Tab | Fieldsets |
| --- | --- |
| **UI** | Session, Confirmations, File List (folder/hidden) |
| **Rename** | Rename List (including File List double-click), Undo & Rename Log |
| **Misc** | Location |

Tab captions use 11px (slightly under body chrome) via `TextBlock` headers — Fluent TabItem typography otherwise renders large.

Do **not** add a ScrollViewer or persist the selected tab. ViewModel draft/Commit path stays as-is — layout-only.

```mermaid
flowchart TB
  Options[Options dialog]
  Options --> Tabs[TabControl]
  Tabs --> Ui[UI tab]
  Tabs --> Rename[Rename tab]
  Tabs --> Misc[Misc tab]
  Ui --> S[Session]
  Ui --> C[Confirmations]
  Ui --> F[File List]
  Rename --> R[Rename List]
  Rename --> Log[Undo and Rename Log]
  Misc --> L[Location]
  Options --> Footer[OK Cancel footer]
```

## Files

- [`Mfr.App.Ui/Views/Options/OptionsDialog.axaml`](../../Mfr.App.Ui/Views/Options/OptionsDialog.axaml) — tabs, fixed height, `options-tabs` / `TabItem` font styles
- [`Mfr.Tests/Ui/Options/OptionsDialogTests.cs`](../../Mfr.Tests/Ui/Options/OptionsDialogTests.cs)
- [`help/ui/optionswin.html`](../../help/ui/optionswin.html) — anchors `#additems` (UI), `#rename`, `#log`, `#misc`
- [`help/images/ui/options.png`](../../help/images/ui/options.png) — UI tab representative shot

## Out of scope

- New options, Explorer integration, log-level UI, or prefs schema changes.
- Persisting last-selected Options tab.
