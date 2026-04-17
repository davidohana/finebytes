# ExtractRight

Keeps only the **last** `count` characters of the segment; the leading part is removed.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `ExtractRight`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | Number of characters to keep from the right (clamped 0…length). |

## Examples

- `count`: `3`, input `ABCDEF` → `DEF`

**Example preset fragment**

```json
{
  "type": "ExtractRight",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "count": 3
  }
}
```

See [TrimRight](TrimRight.md) to **drop** a fixed number of characters from the right instead.
