# ExtractLeft

Keeps only the **first** `count` characters of the segment; the rest is removed.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | Number of characters to keep from the left (clamped 0…length). |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `count`: `3` | `abcdef` | `abc` | |
| `count`: `0` | `abc` | *(empty)* | |
| `count`: `100`<br>(segment shorter than count) | `ab` | `ab` | |

See [TrimLeft](TrimLeft.md) to **drop** a fixed number of characters from the left instead.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "ExtractLeft",
  "target": {
    "family": "FileName",
    "fileNamePart": "Prefix"
  },
  "options": {
    "count": 3
  }
}
```
