# Cleaner

Replaces **invalid file-name characters** (optional) and any **custom** characters you list with a single **replacement** string (often one safe character or empty).

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `Cleaner`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `removeIllegalChars` | bool | If `true`, characters invalid in file names on the current OS are replaced. |
| `customCharsToRemove` | string | Extra characters to replace (can be empty). |
| `replacement` | string | Substitute for each removed character (can be empty to delete). |

If there is nothing to remove (no illegal pass and no custom chars), the segment is unchanged.

## Examples

**Only OS-invalid characters**

- `removeIllegalChars`: `true`, `customCharsToRemove`: `""`  
- On Windows, characters like `*` or `:` in the segment become `replacement`.

**Custom set**

- `customCharsToRemove`: "*:?"`, `replacement`: `"-"`  
- Each `*`, `:`, or `?` becomes `-`.

**Example preset fragment**

```json
{
  "type": "Cleaner",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "removeIllegalChars": true,
    "customCharsToRemove": "",
    "replacement": "_"
  }
}
```

Run **Cleaner** early in the chain if later filters assume a safe file name.
