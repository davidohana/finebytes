---
name: Help non-filter screenshots
overview: "Inventory of UI/guide/dialog screenshots still missing from help (filters already done)."
todos:
  - id: p0-shell
    content: "P0: main window + five core panes"
    status: pending
  - id: p1-dialogs
    content: "P1: Options, Filter Options, Presets, Log, Format Editor, field shuttle, sort, Visual Trim"
    status: pending
  - id: p2-howto
    content: "P2: tutorial/howto step shots (optional)"
    status: pending
  - id: wire-html
    content: "Add <figure> refs + expand help/SCREENSHOTS.md (or split ui checklist)"
    status: pending
isProject: false
---

# Help non-filter screenshots — inventory

Filter option bodies are done (`help/images/{Type}.png`, [`help/SCREENSHOTS.md`](../../help/SCREENSHOTS.md)).
No other help pages currently reference images. This list is what still **should** get shots for MFR7-parity shell help.

## Naming

| Kind | Path convention | Example |
| --- | --- | --- |
| Main / pane | `help/images/ui/{stem}.png` | `ui/main-window.png`, `ui/file-list.png` |
| Dialog | `help/images/ui/{stem}.png` | `ui/options.png`, `ui/filter-options.png` |
| Howto step (optional) | `help/images/guide/{stem}.png` | `guide/tutorial-add-files.png` |

Keep filter shots in `help/images/{Type}.png` (unchanged).

## Framing rules (same as filters)

- Prefer **cropped subject** (pane or dialog), not full desktop chrome, unless the page is “main window overview.”
- Preserve aspect: help CSS already uses `height: auto` + `max-width: 640px`.
- Linux/Avalonia look is OK for MFR8 docs.
- Seed a few Rename List rows + one filter in the chain when the shot needs context.

## Priority P0 — shell orientation (do first)

These match [`help/ui/ui.html`](../../help/ui/ui.html) / [`parts.html`](../../help/ui/parts.html) and unblock “what am I looking at?”

| # | Asset | Help page | Capture subject |
| ---: | --- | --- | --- |
| 1 | `ui/main-window.png` | `ui/ui.html`, `ui/parts.html` | Full main window with panes populated (File List + Rename List + palette + chain + Filter Configuration) |
| 2 | `ui/file-list.png` | `ui/fileexp.html` | File List pane (folder + file rows) |
| 3 | `ui/rename-list.png` | `ui/renamelist.html` | Rename List with preview columns showing before/after |
| 4 | `ui/available-filters.png` | `ui/availfilterlist.html` | Available Filters / palette (group tree or list) |
| 5 | `ui/filter-chain.png` | `ui/appliedfilterlist.html` | Filter Chain with 2–3 steps selected/enabled |
| 6 | `ui/filter-configuration.png` | `ui/filterconfigpanel.html` | Filter Configuration **pane chrome** (title + `?`/`↺`/`📌` + an options body) — distinct from per-filter option crops |

## Priority P1 — dialogs & tools

| # | Asset | Help page | Capture subject |
| ---: | --- | --- | --- |
| 7 | `ui/filter-options.png` | `ui/filteropts.html` | Filter Options dialog (Name / Apply To / scope) |
| 8 | `ui/options.png` | `ui/optionswin.html` | Options dialog (representative tab) |
| 9 | `ui/preset-manager.png` | `ui/presetmanager.html` | Preset Manager |
| 10 | `ui/rename-log.png` | `ui/log.html` | Rename Log window with a few entries |
| 11 | `ui/format-editor.png` | `ui/formateditor.html` | Format Editor (format string + token picker useful) |
| 12 | `ui/field-shuttle.png` | `ui/fieldselector.html` | Select Fields / field shuttle |
| 13 | `ui/auto-sort.png` | `ui/sorteditor.html` | Auto-Sort / Sort tab |
| 14 | `ui/visual-trim.png` | `ui/visualtrimmer.html` | Visual Trim Helper with a sample string selected |
| 15 | `ui/status-bar.png` | `ui/statusbar.html` | Status bar strip (Items / Filters / Changes) — can crop from main window |

## Priority P2 — workflows (optional, multi-shot)

Only if we want tutorial/howto pages illustrated. Prefer **reuse** P0/P1 crops with callouts rather than many unique files.

| # | Asset | Help page | Notes |
| ---: | --- | --- | --- |
| 16 | `guide/tutorial-overview.png` | `guide/tutorial.html` | Same as or crop of `main-window` |
| 17 | `guide/apply-go.png` | `guide/applychanges.html` | Toolbar GO / confirm if any |
| 18 | `guide/undo-last.png` | `guide/undolast.html` | Undo affordance or post-undo Rename List |
| 19 | `guide/save-preset.png` | `guide/savepreset.html` | Save Preset dialog (may overlap Preset Manager) |
| 20 | `guide/reset-config.png` | `guide/resetconfig.html` | Only if there is a distinct UI surface |

## Skip (no screenshot needed)

| Area | Why |
| --- | --- |
| `tokens/*fp.html`, `reference/regex.html`, `reference/dateformat.html` | Reference tables / syntax — text is enough |
| `reference/fields.html` | Field catalog; optional later if shuttle shot is unclear |
| `intro/*`, `about/*`, `guide/cml.html`, `guide/console.html`, `guide/faqs.html`, `guide/tips.html` | Prose / CLI; logo on About dialog is app chrome, not help HTML |
| Optionless filter pages | Already decided: no figure |

## Suggested HTML wiring

For each P0/P1 page, add one `<figure class="screenshot">` under the H1 (same pattern as filter help):

```html
<figure class="screenshot">
  <img src="../images/ui/file-list.png" alt="File List" width="…" height="…" />
  <figcaption>File List.</figcaption>
</figure>
```

Extend [`help/SCREENSHOTS.md`](../../help/SCREENSHOTS.md) with a **UI / dialogs** checklist section (or split `help/SCREENSHOTS-UI.md` if the filter list should stay filter-only).

## Capture approach

1. `just run-ui` on Xvfb (or Windows for product-look shots).
2. Seed folder + Rename List rows + one filter chain.
3. Open each dialog; crop to subject; save under `help/images/ui/`.
4. Optional later: headless window/dialog render harness (harder than filter editors — full MainWindow).

## Exit criteria

- Every P0 page has a real PNG (no checkerboard).
- P1 complete or explicitly deferred in the checklist.
- Link/img paths resolve; `height: auto` CSS still in effect.
