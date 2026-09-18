---
name: Help filter screenshots
overview: "Capture Filter Configuration option-panel PNGs for every filter help page that references help/images/{Type}.png, then wire checklist + HTML dimensions."
todos:
  - id: decide-optionless
    content: "Lock: keep or drop <figure> for 6 optionless filters"
    status: completed
  - id: decide-framing
    content: "Lock: crop to options body vs full Filter Configuration pane"
    status: completed
  - id: capture-simple
    content: "Capture simple/default editors (space/case/trim/misc/attrs)"
    status: completed
  - id: capture-complex
    content: "Capture list/formatter/audio editors with light illustrative defaults"
    status: completed
  - id: ship-assets
    content: "Write PNGs under help/images/, fix img width/height, tick SCREENSHOTS.md"
    status: completed
isProject: false
---

# Help filter screenshots — capture plan

Parent: optional assets called out in [`help/SCREENSHOTS.md`](../../help/SCREENSHOTS.md) and [`docs/plans/in-app-help-about.plan.md`](in-app-help-about.plan.md). HTML already ships `<img>` tags; `help/images/` has only `.gitkeep`.

## Decisions (locked)

- **Framing:** options body only (no Filter Configuration title bar).
- **Optionless filters:** drop the `<figure>` (and checklist rows) for the six no-options filters.

## Scope (what is missing)

| Bucket                                                       |                 Count | Notes                                   |
| ------------------------------------------------------------ | --------------------: | --------------------------------------- |
| Filter help pages with `<img src="../../images/{Type}.png">` |                **36** | All under `help/filters/**`             |
| Files present in `help/images/`                              |                 **0** | Checkerboard placeholder via `help.css` |
| Non-filter help (`ui/`, `guide/`, `tokens/`, …)              | **0** screenshot refs | Out of scope for this pass              |

Only filter option shots are requested today. UI/guide pages do not reference images yet.

## What each shot is

Per [`SCREENSHOTS.md`](../../help/SCREENSHOTS.md):

1. Run UI → add filter → select it so **Filter Configuration** shows its type-specific editor.
1. Capture the **options body** (preferred) or the full Filter Configuration pane.
1. Save as `help/images/{Type}.png` (catalog type name).
1. Target width ~**400–600px**; HTML currently hard-codes `width="480" height="120"` — update `height` (and width if needed) to match real crops.

**Not** in frame: main window chrome, File List, Rename List, Filter Palette, Filter Chain list (except incidental edge if cropping is loose).

**Not** Filter Options dialog (`Filters → Filter options` / Apply To / scope) — that is a different help topic (`help/ui/filteropts.html`) with no image yet.

## Framing decision (needs lock)

| Option                                                                     | Pros                                                              | Cons                                   |
| -------------------------------------------------------------------------- | ----------------------------------------------------------------- | -------------------------------------- |
| **A. Options body only** (recommended)                                     | Matches MFR7 “dialog options” idea; readable in help; less chrome | Need careful crop per filter           |
| **B. Full Filter Configuration pane** (title bar + `?`/`↺`/`📌` + options) | Shows where options live in MFR8                                  | Title chrome repeats 36×; taller files |

Default recommendation: **A**, include Visual Trim Helper when it is part of the editor (count / Trim Between).

## Content decision (defaults vs demo values)

| Class                 | Filters                          | Capture state                                                          |
| --------------------- | -------------------------------- | ---------------------------------------------------------------------- |
| **Defaults OK**       | Most simple editors              | Fresh add from palette; no edits                                       |
| **Light demo values** | List / multiline / format string | Fill 1–3 representative lines so the control is not an empty box       |
| **Collapsed chrome**  | Editors with Format Token Picker | Keep picker **collapsed** (default) so the shot stays about the filter |

Proposed demo fills (only when empty looks useless):

| Type                                  | Demo content                                                                                           |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `Replacer`                            | Find `_`, replace with ` ` (or similar short pair)                                                     |
| `ReplaceList`                         | Two lines, e.g. `a => b` and `Blue Train => Blue_Train`                                                |
| `CasingList` / `NameList`             | 2–3 short entries matching help examples                                                               |
| `Formatter` / `Inserter`              | One short format string from the help page (e.g. `<file-name>`)                                        |
| `Cleaner`                             | Leave defaults if checkboxes already illustrate; else defaults                                         |
| `AudioTagSetter` / `Id3v2FieldSetter` | Defaults with at least one field active if defaults are all off                                        |
| Count / Trim Between                  | Defaults + Visual Trim Helper visible; no Rename List sample required unless helper looks broken empty |

## Optionless filters (needs lock)

These help pages say “This filter has no options” but still reference a PNG. Factory returns `null` → Filter Configuration shows **title bar only** (empty body):

| Type                      | Help page                                         |
| ------------------------- | ------------------------------------------------- |
| `RemoveSpaces`            | `help/filters/space/RemoveSpaces.html`            |
| `ShrinkSpaces`            | `help/filters/space/ShrinkSpaces.html`            |
| `SeparateCapitalizedText` | `help/filters/space/SeparateCapitalizedText.html` |
| `StripSpacesLeft`         | `help/filters/space/StripSpacesLeft.html`         |
| `StripSpacesRight`        | `help/filters/space/StripSpacesRight.html`        |
| `UppercaseInitials`       | `help/filters/case/UppercaseInitials.html`        |

| Option                                                          | Recommendation                           |
| --------------------------------------------------------------- | ---------------------------------------- |
| **1. Drop `<figure>`** from those 6 HTML pages + checklist rows | Cleanest; avoids empty/meaningless shots |
| **2. Capture title-only pane**                                  | Keeps HTML shape; weak UX                |
| **3. Capture palette row / chain step only**                    | Inconsistent with other pages            |

**Recommend 1** unless you want parity with MFR7 empty-dialog GIFs.

## Capture inventory (30 with editors + 6 optionless)

### Case (5)

| File                        | Editor         | Complexity          |
| --------------------------- | -------------- | ------------------- |
| `CapitalizeAfter.png`       | Character list | Simple              |
| `CasingList.png`            | Multiline list | Demo lines          |
| `LettersCase.png`           | Mode radios    | Simple              |
| `SentenceEndCharacters.png` | Character list | Simple              |
| `UppercaseInitials.png`     | **None**       | Optionless decision |

### Space (8)

| File                                                                                                                     | Editor                     | Complexity          |
| ------------------------------------------------------------------------------------------------------------------------ | -------------------------- | ------------------- |
| `SpaceCharacter.png`                                                                                                     | Define + Replace fieldsets | Simple (defaults)   |
| `SpaceAfter.png` / `SpaceAround.png`                                                                                     | Shared SpaceTrigger editor | Simple              |
| `RemoveSpaces.png` / `ShrinkSpaces.png` / `SeparateCapitalizedText.png` / `StripSpacesLeft.png` / `StripSpacesRight.png` | **None**                   | Optionless decision |

### Trimming (6)

| File                                                                      | Editor                      | Complexity      |
| ------------------------------------------------------------------------- | --------------------------- | --------------- |
| `TrimLeft.png` / `TrimRight.png` / `ExtractLeft.png` / `ExtractRight.png` | Count + Visual Trim Helper  | Medium (taller) |
| `TrimBetween.png`                                                         | Ranges + Visual Trim Helper | Medium (taller) |
| `ShrinkDuplicateCharacters.png`                                           | Character field             | Simple          |

### Replace (3)

| File              | Editor                       | Complexity  |
| ----------------- | ---------------------------- | ----------- |
| `Cleaner.png`     | Checkbox set                 | Simple      |
| `Replacer.png`    | Find/replace + match options | Demo values |
| `ReplaceList.png` | Multiline + match options    | Demo lines  |

### Formatting (5)

| File             | Editor                                 | Complexity  |
| ---------------- | -------------------------------------- | ----------- |
| `Counter.png`    | Counter options                        | Simple      |
| `Formatter.png`  | Format string (+ collapsed token pane) | Demo string |
| `Inserter.png`   | Insert options + format                | Demo string |
| `NameList.png`   | Multiline names                        | Demo lines  |
| `TokenMover.png` | Token move options                     | Simple      |

### Audio (3)

| File                   | Editor          | Complexity                                             |
| ---------------------- | --------------- | ------------------------------------------------------ |
| `TagRemover.png`       | Tag checkboxes  | Medium                                                 |
| `AudioTagSetter.png`   | Tall field grid | Tall crop; may exceed 520px CSS max — OK (`max-width`) |
| `Id3v2FieldSetter.png` | Field setter    | Medium–tall                                            |

### Attributes (3)

| File                   | Editor            | Complexity |
| ---------------------- | ----------------- | ---------- |
| `AttributesSetter.png` | Attribute toggles | Simple     |
| `DateTimeSetter.png`   | Date/time fields  | Simple     |
| `TimeShifter.png`      | Shift options     | Simple     |

### Misc (3)

| File                   | Editor        | Complexity |
| ---------------------- | ------------- | ---------- |
| `FixLeadingZeros.png`  | Count/options | Simple     |
| `PathMover.png`        | Path options  | Simple     |
| `StripParentheses.png` | Mode options  | Simple     |

**With editors to capture:** 30. **Optionless:** 6 (drop or empty-pane).

## Environment / look

- Capture on Cloud Linux + Xvfb via `just run-ui` (Avalonia).
- Theme/fonts will be **Linux Avalonia**, not classic Win32 MFR7 GIFs — acceptable for MFR8 docs unless you require Windows-only shots later.
- Prefer a settled window (no tooltips, no open dropdowns, no focus rings if avoidable).

## Capture workflow

1. `just run-ui` on `:99`.
1. For each filter in inventory: add from palette → select in Filter Chain → wait for editor paint.
1. Screenshot → crop to framing choice → save `help/images/{Type}.png`.
1. Spot-check HTML in a browser (or open help file) that the figure no longer shows checkerboard.
1. Adjust each page’s `width` / `height` attributes to the real pixel size (or drop fixed height and rely on CSS `max-width`).
1. Check off rows in [`help/SCREENSHOTS.md`](../../help/SCREENSHOTS.md); remove optionless rows if we drop figures.

No automation harness yet (no headless “export editor to PNG” API). Manual/computer-use capture is the planned method; a later optional follow-up could script Avalonia Headless render of each editor view alone if we want regression-friendly assets.

## Out of scope (this pass)

- Screenshots for `help/ui/*`, guide, tokens, intro.
- Filter Options dialog / Format Editor window / Preset Manager.
- Matching MFR7 GIF pixel-perfect layout.
- Dark theme variants.

## Exit criteria

- Every remaining `<img>` under `help/filters/` resolves to a real PNG (no checkerboard).
- Optionless policy applied consistently (no empty meaningless images unless you chose option 2).
- `SCREENSHOTS.md` checklist matches shipped files.
- `just lint` / help link tests still green (no HTML path breaks).
