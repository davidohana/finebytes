# TrimRight

Removes a fixed number of characters from the **right** end of the segment. The count is clamped to the segment length.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `TrimRight`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | How many characters to remove from the end. |

## Examples

- `count`: `2`, input `ABCDEF` → `ABCD`

**Example preset fragment**

```json
{
  "type": "TrimRight",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "count": 2
  }
}
```

To **keep** only the last N characters, use [ExtractRight](ExtractRight.md).
