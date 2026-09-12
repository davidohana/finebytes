# Status bar outcomes plan

Parent: Rename List / main shell feedback ([rename-list-go.plan.md](rename-list-go.plan.md), [rename-list-ui.plan.md](rename-list-ui.plan.md)).

## Status

- [x] P1 — Sticky rich status channel + brushes + Preview Errors red
- [x] P2 — High-signal Rename List outcomes + colored cell errors
- [x] P3 — Preset load/save only
- [x] P4 — File List CopyPath only
- [x] P5 — Selected count panel

## Decisions (locked)

- **High-signal only** — Because status is sticky, publish operation text only for infrequent / outcome-heavy actions. **In scope:** Rename List add (incl. skips/zero/exception), GO / stop / commit errors, export success, locate failure, refresh **when load errors exist**, preset load/save, File List CopyPath. **Out of scope (no sticky spam):** folder navigate, routine remove/clear/reorder, interactive filter add/remove, auto-sort apply, plain refresh with no load errors.
- **Rich sticky status channel** — Collapse Rename List `LastAddError` / `LastLocateError` / `LastGoStatus` into one `LastStatusMessage` of type `StyledTextDisplay` (not `string`). Same property type on `FileListViewModel` and `AppliedFiltersViewModel` (presets / CopyPath). `MainWindowViewModel` watches producers and stores the display in `_lastStatusHint`.
- **Stays until replaced** — No timed clear (`StatusHintClearMilliseconds` / `_ClearStatusHintAfterDelayAsync` removed). Last message remains until another **in-scope** operation publishes a new one.
- **Overlay priority** — Rename List **cell hint** overlays while a cell is focused/selected; on clear, **restore `_lastStatusHint`**. Priority: cell overlay > sticky last status > empty.
- **No chrome hover hints** — Toolbar/menu already have `ToolTip` / `AppTips`. Do **not** mirror those into the status bar. Keep Avalonia tooltips as-is.
- **Severity colors in the left hint** — `StyledTextRun.ForegroundResourceKey`:
  - **Error** → `StatusBarErrorForegroundBrush` (GO commit errors, locate miss, add exception, load-error cell hints).
  - **Warning** → `StatusBarWarningForegroundBrush` (partial success, skips, stopped GO, refresh with load errors, “no items added”).
  - **Neutral** → default (pure success, normal cell values).
  - Mixed messages use multiple runs.
- **Shared helpers** — `StatusBarText`: `Neutral` / `Warning` / `Error` / `Combine(...)`. Brushes in theme (light + dark).
- **Success stays uncolored** — No green success tint.
- **Cell hints** — Load-error / preview-error marker runs use error brush; column name bold/neutral.
- **Preview Errors count panel** — Red when `PreviewErrorCount > 0`.
- **Dialogs stay for decisions/detail** — Unchanged.
- **Export** — Neutral success sticky + reveal-in-Explorer; failures stay dialog-only.
- **Clipboard** — File List `CopyPath` only.
- **Undo Last** — Out of scope; debts note when Undo ships.
- **Selection** — `Selected: {n}` for focused pane when `n > 0`.

## MFR7 reference brief

- **Sources:** `statusbar.html`; `renamelist.html`; `hints.txt` + `StatusMessage`; GO outcome via status.
- **Behavior to match:** Left hint + counts; Preview Errors red when ≥1; Rename List cell value in status bar.
- **finebytes extension:** Colored runs; sticky until next high-signal message; selection count.
- **Parity gaps:** No `hints.txt` / chrome status-bar hot hints (tooltips cover chrome); no navigate/filter/sort spam; no Undo Log; no green success.

## Non-goals

- Chrome/toolbar/menu status-bar hover hints when a tooltip already exists.
- Sticky status for navigate, remove/clear, filter chain edits, auto-sort, or clean refresh.
- Undo Last until Undo exists.
- Rename List clipboard commands.
- Status text while a progress dialog is open.
- Replacing confirm dialogs.
- Per-control Filter Editor hints.
- Green success accents; status-bar icons.
- New count panels beyond Selected + existing four.
- Auto-clear / fade after a timeout.

## Phases

### P1 — Sticky rich status channel + brushes + Preview Errors red

- Add error/warning brushes + `StatusBarText` helper.
- Replace timed transient clear with sticky `_lastStatusHint`; migrate Rename List three `Last*` → `LastStatusMessage`; update tests to assert stickiness.
- Preview Errors count uses error brush when count > 0.
- **Exit:** Existing GO/add/locate paths are rich + sticky; GO errors colored; Preview Errors panel red when ≥1.

### P2 — High-signal Rename List outcomes + colored cell errors

- **Add:** Success (neutral); skips / zero-add (warning); exception (error).
- **Export:** Neutral success.
- **Refresh:** Status **only if** load errors — warning + “select a cell” hint; silent when clean.
- **GO / Stop:** Existing outcomes + stopped warning; mixed error runs when commit failures.
- **Locate:** Keep as error (already).
- **Not in this phase:** remove / clear / reorder messages.
- **Cell hints:** Color load-error / preview-error marker.
- **Exit:** Tests for plain text + `ForegroundResourceKey` on error/warning runs; clean refresh does not publish status.

### P3 — Preset load/save only

- Successful load/save → neutral sticky (`Loaded preset "…"`, `Saved preset "…"`). Hard failures stay dialog-only.
- **No** interactive filter Append/Remove status.
- Wire Applied Filters `LastStatusMessage` in MainWindow.
- **Exit:** Preset load/save tests; loading a preset does not emit per-filter messages.

### P4 — File List CopyPath only

- CopyPath success → neutral; failure → error.
- **No** navigate / “Listed N in path” messages.
- Wire File List `LastStatusMessage`.
- **Exit:** CopyPath tests only.

### P5 — Selected count panel

- Focused-pane `Selected: N` when > 0.
- **Exit:** Multi-select updates panel; empty hides it.
