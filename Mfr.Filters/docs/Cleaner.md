# Cleaner

Replaces **invalid file-name characters** (optional) and any **custom** characters you list with a single **replacement** string (often one safe character or empty).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `removeIllegalChars` | bool | If `true`, characters invalid in file names on the current OS are replaced. |
| `customCharsToRemove` | string | Extra characters to replace (can be empty). |
| `replacement` | string | Substitute for each removed character (can be empty to delete). |

If there is nothing to remove, the segment is unchanged.

## Examples

- `removeIllegalChars`: `true`, `customCharsToRemove`: `""` — on Windows, `*` and `:` in the segment become `replacement`.
- `customCharsToRemove`: "*:?", `replacement`: `"-"` — each listed character becomes `-`.

Run **Cleaner** early if later filters assume a safe file name.

```json
{ "removeIllegalChars": true, "customCharsToRemove": "", "replacement": "_" }
```
