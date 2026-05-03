# ExtractRight

Keeps only the **last** `count` characters of the segment; the leading part is removed.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | Number of characters to keep from the right (clamped 0…length). |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `count`: `3` | `abcdef` | `def` | |
| `count`: `0` | `abc` | *(empty)* | |
| `count`: `100`<br>(segment shorter than count) | `ab` | `ab` | |

See [TrimRight](TrimRight.md) to **drop** a fixed number of characters from the right instead.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "ExtractRight",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "count": 3
  }
}
```
