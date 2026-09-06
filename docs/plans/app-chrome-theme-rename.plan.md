# App chrome theme rename (`FileList*` → shared)

Status: **handover** — not started. Spawned from FormatEditor highlight review (FormatEditor background should use the same surface brush as filter TextBoxes; that brush is misnamed `FileListRowBrush`).

## Problem

[`Themes/FileList.axaml`](../../Mfr.App.Ui/Themes/FileList.axaml) defines a Light/Dark palette historically named for File List. Most keys are now the **app-wide chrome** used by Filter Configuration, FormatEditor, dialogs, Applied Filters, Filter Palette, Rename List, etc. Names imply File List ownership and make “use the right brush” harder than it should be.

Related cosmetic leftover from FormatEditor work: AvaloniaEdit `format-string-field` still uses Fluent `SystemControlBackgroundAltHighBrush` instead of the shared row/surface brush — fix that in the same pass once the key has a honest name (or immediately with the new key).

## Goal

One clearly named **shared chrome** dictionary for surfaces used everywhere; keep **File List–specific** keys under `FileList*` (or migrate only if they stay File List–only).

No visual redesign — same hex values, new keys. No legacy aliases / dual keys (project persistence policy mindset: one current name).

## Chosen naming

Prefix: **`AppChrome`** (reads as “window chrome / content surface,” not a feature pane).

| Current key                     | New key                         | Role                                                  |
| ------------------------------- | ------------------------------- | ----------------------------------------------------- |
| `FileListRowBrush`              | `AppChromeSurfaceBrush`         | Primary editable / row surface (white / `#202020`)    |
| `FileListAltRowBrush`           | `AppChromeAltSurfaceBrush`      | Striped / muted fill (also used for read-only fields) |
| `FileListForegroundBrush`       | `AppChromeForegroundBrush`      | Default text                                          |
| `FileListHoverBrush`            | `AppChromeHoverBrush`           | Hover fill                                            |
| `FileListSelectionBrush`        | `AppChromeSelectionBrush`       | Selection fill                                        |
| `FileListSelectionBorderBrush`  | `AppChromeSelectionBorderBrush` | Selection border                                      |
| `FileListSeparatorBrush`        | `AppChromeSeparatorBrush`       | Grid / list separators                                |
| `FileListAddressBarBorderBrush` | `AppChromeBorderBrush`          | Generic control border (no longer “address bar”)      |
| `FileListBreadcrumbMutedBrush`  | `AppChromeMutedForegroundBrush` | Secondary / muted text                                |
| `FileListFont`                  | `AppChromeFont`                 | UI sans (from `GridFonts`)                            |
| `FileListFontSize`              | `AppChromeFontSize`             | Default UI font size                                  |

**Stay `FileList*`** (feature-specific; File List / address bar / glyphs):

| Keep                                                    |
| ------------------------------------------------------- |
| `FileListHeaderBrush` / `FileListHeaderForegroundBrush` |
| `FileListAddressBarBrush` (bar fill itself)             |
| `FileListSortGlyph*`                                    |
| `FileListPreviewGlyph*`                                 |

**Also rename while touching fonts** (same ownership lie):

| Current                    | New                       |
| -------------------------- | ------------------------- |
| `RenameListFixedWidthFont` | `AppChromeFixedWidthFont` |

(Used by Rename List *and* FormatEditor / format-string fields.)

## Non-goals

- Changing Light/Dark colors or inventing a new visual language
- Moving brushes into Fluent system resources
- Dual-key compatibility (`FileListRowBrush` → alias of new key)
- Renaming `Themes/FileList.axaml` file in the same PR if churn is painful — optional follow-up: split `AppChrome.axaml` + slim `FileList.axaml` for leftovers
- FormatEditor highlight behavior

## Implementation steps

1. **Inventory** — `rg FileList(Row|AltRow|Foreground|Hover|Selection|Separator|AddressBarBorder|BreadcrumbMuted)|FileListFont|RenameListFixedWidthFont` across `Mfr.App.Ui` + `Mfr.Tests`; confirm no C# string keys beyond `GridFonts`.
1. **Define new keys** — add `AppChrome*` (and fixed-width font) in theme dictionaries / `GridFonts.AddResources`; delete old keys in the same commit (no aliases).
1. **Replace call sites** — all AXAML `DynamicResource` / `StaticResource` + any test assertions on resource keys / `FontFamily` identity.
1. **FormatEditor background** — set `AvaloniaEdit|TextEditor.format-string-field` `Background` to `AppChromeSurfaceBrush` (closes the review leftover).
1. **Optional file split** — `Themes/AppChrome.axaml` for shared keys; `FileList.axaml` keeps header/address/glyph only; update `App.axaml` `ResourceInclude`.
1. **Verify** — `just format` / `just lint`; smoke File List, Rename List, Filter Configuration, FormatEditor, one dialog in Light + Dark.
1. **Docs** — short note in this plan as **shipped**; optional one-line in `docs/mfr-folder-layering.md` or UI README if theme ownership is documented there; bullet in [`docs/debts.md`](../debts.md) only if deferred mid-work.

## Tests

- No new unit tests required (resource rename).
- Headless facts that assert `GridFonts.*Family` or resource keys: update names.
- Construct/show FormatEditor + Filter editor smoke already in suite — must stay green.

## Risk / cost

- **Churn:** many AXAML files; mechanical find-replace; low behavior risk if colors unchanged.
- **Missed key:** compile won’t catch bad `DynamicResource` names — rely on `rg` + visual smoke.
- **Rank:** medium cost-to-value — naming clarity only; do as a focused PR, not mixed with feature work.

## Out of scope / do not

- Do not keep `FileListRowBrush` as a forever alias.
- Do not rename only `RowBrush` and leave `Foreground` / borders as `FileList*`.
- Do not fold FormatToken / RenameList preview-error reds into this pass.

## References

- Trigger: FormatEditor highlight review (“Match TextEditor background to filter TextBox row brush”)
- Palette today: [`Mfr.App.Ui/Themes/FileList.axaml`](../../Mfr.App.Ui/Themes/FileList.axaml)
- Fonts: [`Mfr.App.Ui/Views/GridColumnSizing/GridFonts.cs`](../../Mfr.App.Ui/Views/GridColumnSizing/GridFonts.cs)
- FormatEditor ship note: [`formateditor-syntax-highlight.plan.md`](formateditor-syntax-highlight.plan.md)
