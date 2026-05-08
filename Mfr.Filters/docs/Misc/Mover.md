# Mover

Moves items to a destination folder by combining a required static **root** with an optional dynamic **sub-folder** template. It applies to **files and folders** in the rename list the same way (folder rows store the folder name with an empty extension).

The preview **Parent Directory** is replaced with `RootFolder` + `\` + resolved `SubFolder`. Only the preview path changes here; actual folder creation and movement happen on commit. Source folders are never deleted.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `rootFolder` | string | Required. Absolute destination path. All items move under this root. |
| `subFolder` | string | Optional. Template appended below `rootFolder`. Use `\` to build nested levels. Supports formatter tokens (`<file-name>`, `<parent-folder>`, `<now:format>`, `<counter:…>`). Use `""` when none; items then land directly in `rootFolder`. |

## Examples

| Options | Before (directory) | After (directory) | Comment |
|---------|-------------------|------------------|---------|
| `rootFolder`: `C:\Dest` | `C:\Source` | `C:\Dest` | No sub-folder; flat move. |
| `rootFolder`: `C:\Music`<br>`subFolder`: `Albums` | `C:\Downloads` | `C:\Music\Albums` | Static sub-folder. |
| `rootFolder`: `C:\Music`<br>`subFolder`: `<parent-folder>` | `C:\Downloads\Junkies` | `C:\Music\Junkies` | Parent folder name resolved from original path. |
| `rootFolder`: `C:\Music\New`<br>`subFolder`: `<parent-folder>\<file-name>` | `C:\Downloads\Cowboy Junkies` (file: `03 - Blue Moon`) | `C:\Music\New\Cowboy Junkies\03 - Blue Moon` | Two-level dynamic sub-folder. |
| `rootFolder`: `C:\Archive`<br>`subFolder`: `<now:yyyy>` | `C:\Inbox` | `C:\Archive\2026` | Year-based folder using the `now` token. |

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "Mover",
  "options": {
    "rootFolder": "C:\\Music\\New",
    "subFolder": "<parent-folder>\\<file-name>"
  }
}
```
