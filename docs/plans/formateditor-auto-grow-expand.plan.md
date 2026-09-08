---
name: FormatEditor capped auto-grow + expand
overview: Multiline FormatEditors grow with wrapped content up to a max height, then scroll; an expand button opens a taller dialog bound to the same Text.
todos:
  - id: auto-grow
    content: "Capped auto-grow for AcceptsReturn=true (MinHeight 64 → MaxHeight ~168)"
    status: completed
  - id: expand-dialog
    content: "Expand button + FormatEditorExpandDialog (live Text sync, OK closes)"
    status: completed
  - id: tests
    content: "Headless tests for expand sync, layout, and auto-grow caps"
    status: completed
isProject: false
---

# FormatEditor — capped auto-grow + expand

## UX

1. **Capped auto-grow** (`AcceptsReturn=true`): measure AvaloniaEdit document height; set `Height` between `MinHeight` (64) and `MaxHeight` (~168). Vertical scroll after the cap.
1. **Expand dialog**: tool button next to Insert/Edit opens a ~560×320 window with a tall FormatEditor (host `AcceptsReturn`, fixed ~280 height, `ShowExpandButton=false`). Two-way `Text` sync; Insert/Edit available; OK closes (text already synced).
1. **Single-line hosts**: no auto-grow; still show Expand. Dialog uses host `AcceptsReturn=false` with larger fixed height (word wrap enabled in dialog for comfort; Enter still rejected).

## Files

- `FormatEditor.axaml` / `.axaml.cs` — Expand button, `ShowExpandButton`, auto-grow
- `FormatEditorExpandDialog.axaml` / `.axaml.cs` — new
- `Icons.axaml` — maximize/expand geometry
- `FormatEditorViewTests.cs` — headless coverage
