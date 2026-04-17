# ExtractLeft

Keeps only the **first** `count` characters of the segment; the rest is removed.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `ExtractLeft`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | Number of characters to keep from the left (clamped 0…length). |

## Examples

- `count`: `4`, input `ABCDEF` → `ABCD`
- `count`: `0` → empty string

**Example preset fragment**

```json
{
  "type": "ExtractLeft",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "count": 4
  }
}
```

See [TrimLeft](TrimLeft.md) to **drop** a fixed number of characters from the left instead.
