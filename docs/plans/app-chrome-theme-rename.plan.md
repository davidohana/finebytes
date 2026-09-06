# App chrome theme rename (`FileList*` → shared)

Status: **shipped** (2026-09-06). Spawned from FormatEditor highlight review (FormatEditor background should use the same surface brush as filter TextBoxes; that brush was misnamed `FileListRowBrush`).

## Problem

[`Themes/FileList.axaml`](../../Mfr.App.Ui/Themes/FileList.axaml) defined a Light/Dark palette historically named for File List. Most keys were the **app-wide chrome** used by Filter Configuration, FormatEditor, dialogs, Applied Filters, Filter Palette, Rename List, etc. Names implied File List ownership and made “use the right brush” harder than it should be.

Related cosmetic leftover from FormatEditor work: AvaloniaEdit `format-string-field` still used Fluent `SystemControlBackgroundAltHighBrush` instead of the shared row/surface brush — fixed in this pass with `AppChromeSurfaceBrush`.

## Goal

One clearly named **shared chrome** dictionary for surfaces used everywhere; keep **File List–specific** keys under `FileList*`.

No visual redesign — same hex values, new keys. No legacy aliases / dual keys.

## Shipped naming

Prefix: **`AppChrome`**. Shared brushes/fonts live in [`Themes/AppChrome.axaml`](../../Mfr.App.Ui/Themes/AppChrome.axaml); File List–only leftovers stay in [`Themes/FileList.axaml`](../../Mfr.App.Ui/Themes/FileList.axaml). Both are included from `App.axaml`.

| Old key                         | New key                         | Role                                                  |
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
| `RenameListFixedWidthFont`      | `AppChromeFixedWidthFont`       | Mono (Rename List + format-string fields)             |

**Stay `FileList*`** (feature-specific):

| Keep                                                    |
| ------------------------------------------------------- |
| `FileListHeaderBrush` / `FileListHeaderForegroundBrush` |
| `FileListAddressBarBrush` (bar fill itself)             |
| `FileListSortGlyph*` / `FileListSortGlyphFontSize`      |
| `FileListPreviewGlyph*`                                 |

`GridFonts` C# members match: `AppChromeFamily` / `AppChromeFixedWidthFamily`.

## References

- Trigger: FormatEditor highlight review (“Match TextEditor background to filter TextBox row brush”)
- Palette: [`Mfr.App.Ui/Themes/AppChrome.axaml`](../../Mfr.App.Ui/Themes/AppChrome.axaml), leftovers in [`FileList.axaml`](../../Mfr.App.Ui/Themes/FileList.axaml)
- Fonts: [`Mfr.App.Ui/Views/GridColumnSizing/GridFonts.cs`](../../Mfr.App.Ui/Views/GridColumnSizing/GridFonts.cs)
- FormatEditor ship note: [`formateditor-syntax-highlight.plan.md`](formateditor-syntax-highlight.plan.md)
