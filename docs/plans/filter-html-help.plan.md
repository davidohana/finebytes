# Filter HTML help (ship beside exe)

Status: **implemented** (screenshots still optional — see `help/SCREENSHOTS.md`).

## Goal

Ship user-facing per-filter Help HTML next to the application (`help/` beside the exe). The Filter Configuration **?** button opens `{Type}.html` via `FilterHelpHost` without depending on an installed MFR7 Help folder.

## Decisions (locked)

| Topic       | Choice                                                                                                       |
| ----------- | ------------------------------------------------------------------------------------------------------------ |
| Location    | Repo-root `help/` (sibling of `Mfr.App.Ui/`), copied to output as `help\…`                                   |
| File names  | Catalog `Type` + `.html` (e.g. `SpaceCharacter.html`) — no MFR7 aliases                                      |
| Host roots  | `DefaultHelpRoots` = only `Path.Combine(AppContext.BaseDirectory, "help")`                                   |
| Content     | User help (purpose → screenshot → options with UI labels → `before >>> after`), not developer markdown dumps |
| Screenshots | `help/images/{Type}.png`; CSS placeholder when missing; `SCREENSHOTS.md` is repo-only (excluded from copy)   |
| Index       | `help/filters.html` with group sections + breadcrumb links from each page                                    |

## Layout

```text
help/
  help.css
  filters.html
  CapitalizeAfter.html … StripParentheses.html   # 36 flat pages
  images/
    .gitkeep
    # optional: {Type}.png
  SCREENSHOTS.md                                  # not copied to output
```

## Code touchpoints

- `FilterCatalogEntry.HelpFileName` — convention `{Type}.html` (set in `FilterCatalog`)
- `Mfr.App.Ui/Services/Help/FilterHelpHost.cs` — app-local root + missing-help message
- `Mfr.App.Ui/Mfr.App.Ui.csproj` — `Content` copy of `..\help\**\*` with `SCREENSHOTS.md` excluded
- Tests: `FilterCatalogHelpTests`, `FilterHelpHostTests` (+ catalog files exist under repo `help/`)

## Authoring rules

1. Source facts from `Mfr.Filters/docs/<Group>/<Type>.md`.
1. Page `<title>` / H1 = catalog **display name** (`FilterPalette`).
1. Option labels match FilterEditors AXAML when an editor exists.
1. No JSON presets; no camelCase API names as primary labels.
1. Shared header + breadcrumb: Filters → group → this filter.

## Display-name exceptions

| Type               | Display name       |
| ------------------ | ------------------ |
| `TagRemover`       | Audio Tag Remover  |
| `FixLeadingZeros`  | Fix Leading 0's    |
| `Id3v2FieldSetter` | ID3v2 Field Setter |
| `DateTimeSetter`   | Date/Time Setter   |

## Follow-ups

- Capture the 36 `images/{Type}.png` screenshots (checklist in `help/SCREENSHOTS.md`).
- When adding a filter: add `{Type}.html`, map entry, index link, screenshot checklist row (see `mfr-implement-filter` skill).

## Related

- F9c in [applied-filter-editors.plan.md](applied-filter-editors.plan.md)
- MFR7 legacy map: [filter-map.md](../../.agents/skills/mfr7-reference/filter-map.md)
