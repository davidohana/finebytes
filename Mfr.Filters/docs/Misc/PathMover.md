# PathMover

Moves items to a destination folder by combining a required static **root** with an optional dynamic **sub-folder** template. It applies to **files and folders** in the rename list the same way (folder rows store the folder name with an empty extension).

The preview **Parent Directory** is replaced with `RootFolder` + `\` + resolved `SubFolder`. Only the preview path changes here; actual folder creation and movement happen on commit. Source folders are never deleted.

## Options

- **`rootFolder`** (string) — Required. Absolute destination path. All items move under this root.
- **`subFolder`** (string)
  - Optional. Template appended below `rootFolder`. Use `\` to build nested levels. Supports formatter tokens
    (`<file-name>`, `<parent-folder>`, `<now:format>`, `<counter:…>`). Use `""` when none; items then land directly in
    `rootFolder`. Must resolve under `rootFolder` (Windows drive or UNC absolutes, and other rooted leftovers after
    stripping a leading `\`, are rejected so `Path.Combine` cannot discard the root).

## Examples

- `rootFolder`: `C:\Dest` — `C:\Source` → `C:\Dest` — No sub-folder; flat move.
- `rootFolder`: `C:\Music`; `subFolder`: `Albums` — `C:\Downloads` → `C:\Music\Albums` — Static sub-folder.
- `rootFolder`: `C:\Music`; `subFolder`: `<parent-folder>`
  - Before: `C:\Downloads\Junkies`
  - After: `C:\Music\Junkies`
  - Comment: Parent folder name resolved from original path.
- `rootFolder`: `C:\Music\New`; `subFolder`: `<parent-folder>\<file-name>`
  - Before: `C:\Downloads\Cowboy Junkies` (file: `03 - Blue Moon`)
  - After: `C:\Music\New\Cowboy Junkies\03 - Blue Moon`
  - Comment: Two-level dynamic sub-folder.
- `rootFolder`: `C:\Archive`; `subFolder`: `<now:yyyy>`
  - Before: `C:\Inbox`
  - After: `C:\Archive\2026`
  - Comment: Year-based folder using the `now` token.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "PathMover",
  "options": {
    "rootFolder": "C:\\Music\\New",
    "subFolder": "<parent-folder>\\<file-name>"
  }
}
```
