# App chrome theme rename (`FileList*` → shared)

Status: **shipped** (2026-09-06). Spawned from FormatEditor highlight review (FormatEditor background should use the same surface brush as filter TextBoxes; that brush was misnamed `FileListRowBrush`). Follow-up: remaining `FileList*` leftovers promoted into `AppChrome*` and `Themes/FileList.axaml` removed.

## Problem

Historically, `Themes/FileList.axaml` defined a Light/Dark palette named for File List. Keys were **app-wide chrome** used by Filter Configuration, FormatEditor, dialogs, Applied Filters, Filter Palette, Rename List, grid headers/glyphs, etc. Names implied File List ownership and made “use the right brush” harder than it should be.

Related cosmetic leftover from FormatEditor work: AvaloniaEdit `format-string-field` still used Fluent `SystemControlBackgroundAltHighBrush` instead of the shared row/surface brush — fixed with `AppChromeSurfaceBrush`.

## Goal

One clearly named **shared chrome** dictionary for surfaces used everywhere.

No visual redesign — same hex values, new keys. No legacy aliases / dual keys.

## Shipped naming

Prefix: **`AppChrome`**. All shared brushes live in [`Themes/AppChrome.axaml`](../../Mfr.App.Ui/Themes/AppChrome.axaml); fonts/sizes from [`AppChromeFonts`](../../Mfr.App.Ui/Views/GridColumnSizing/AppChromeFonts.cs). Included from `App.axaml`.

| Old key                         | New key                          | Role                                                  |
| ------------------------------- | -------------------------------- | ----------------------------------------------------- |
| `FileListRowBrush`              | `AppChromeSurfaceBrush`          | Primary editable / row surface (white / `#202020`)    |
| `FileListAltRowBrush`           | `AppChromeAltSurfaceBrush`       | Striped / muted fill (also used for read-only fields) |
| `FileListAddressBarBrush`       | `AppChromePanelBrush`            | Panel chrome (address bar, shuttle panels)            |
| `FileListHeaderBrush`           | `AppChromeHeaderBrush`           | Grid / rail header fill                               |
| `FileListHeaderForegroundBrush` | `AppChromeHeaderForegroundBrush` | Header text                                           |
| `FileListForegroundBrush`       | `AppChromeForegroundBrush`       | Default text                                          |
| `FileListHoverBrush`            | `AppChromeHoverBrush`            | Hover fill                                            |
| `FileListSelectionBrush`        | `AppChromeSelectionBrush`        | Selection fill                                        |
| `FileListSelectionBorderBrush`  | `AppChromeSelectionBorderBrush`  | Selection border                                      |
| `FileListSeparatorBrush`        | `AppChromeSeparatorBrush`        | Grid / list separators                                |
| `FileListAddressBarBorderBrush` | `AppChromeBorderBrush`           | Generic control border                                |
| `FileListBreadcrumbMutedBrush`  | `AppChromeMutedForegroundBrush`  | Secondary / muted text                                |
| `FileListSortGlyph*`            | `AppChromeSortGlyph*`            | Sort glyph chrome                                     |
| `FileListSortGlyphFontSize`     | `AppChromeSortGlyphFontSize`     | Sort / preview glyph size                             |
| `FileListPreviewGlyph*`         | `AppChromePreviewGlyph*`         | Preview glyph chrome                                  |
| `FileListFont`                  | `AppChromeFont`                  | UI sans (from `AppChromeFonts`)                       |
| `FileListFontSize`              | `AppChromeFontSize`              | Default UI font size                                  |
| `RenameListFixedWidthFont`      | `AppChromeFixedWidthFont`        | Mono (Rename List + format-string fields)             |

`AppChromeFonts` C# members match: `AppChromeFamily` / `AppChromeFixedWidthFamily`. Measurement contexts: `GridColumnTextFontContext.AppChrome` / `AppChromeFixedWidth`.

## References

- Trigger: FormatEditor highlight review (“Match TextEditor background to filter TextBox row brush”)
- Palette: [`Mfr.App.Ui/Themes/AppChrome.axaml`](../../Mfr.App.Ui/Themes/AppChrome.axaml)
- Fonts: [`Mfr.App.Ui/Views/GridColumnSizing/AppChromeFonts.cs`](../../Mfr.App.Ui/Views/GridColumnSizing/AppChromeFonts.cs)
- FormatEditor ship note: [`formateditor-syntax-highlight.plan.md`](formateditor-syntax-highlight.plan.md)
