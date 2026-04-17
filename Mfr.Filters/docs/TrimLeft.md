# TrimLeft

Removes a fixed number of characters from the **left** end of the segment. The count is clamped to the segment length.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `TrimLeft`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | How many characters to remove from the start (minimum 0; values beyond length are treated as length). |

## Examples

- `count`: `3`, input `ABCDEF` → `DEF`
- `count`: `0` → unchanged
- `count`: `100` on a 5-character string → empty string

**Example preset fragment**

```json
{
  "type": "TrimLeft",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "count": 3
  }
}
```

To **keep** a prefix of length N instead of **dropping** N characters, use [ExtractLeft](ExtractLeft.md).
