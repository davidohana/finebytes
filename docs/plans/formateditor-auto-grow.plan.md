---
name: FormatEditor capped auto-grow
overview: FormatEditors wrap and grow with content up to a max height, then scroll.
todos:
  - id: auto-grow
    content: "Capped wrap auto-grow (AcceptsReturn true: 64→168; false: wrap-for-display 26→168; Enter still blocked when false)"
    status: completed
  - id: tests
    content: "Headless tests for layout and auto-grow caps"
    status: completed
isProject: false
---

# FormatEditor — capped auto-grow

## UX

1. **Capped auto-grow** (all FormatEditors): measure AvaloniaEdit document height; set `Height` between min and `MaxHeight` (~168). Vertical scroll after the cap. Multiline min is 64; single-line min is 26 with **wrap-for-display** (Enter still follows `AcceptsReturn`).

## Files

- `FormatEditor.axaml` / `.axaml.cs` — wrap + auto-grow
- `FormatEditorViewTests.cs` — headless coverage
