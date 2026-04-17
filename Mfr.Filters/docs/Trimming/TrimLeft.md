# TrimLeft

Removes a fixed number of characters from the **left** end of the segment. The count is clamped to the segment length.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | How many characters to remove from the start (minimum 0; values beyond length are treated as length). |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `count`: `2` | `abcd` | `cd` | |
| `count`: `0` | `ab` | `ab` | |
| `count`: `10`<br>(segment shorter than count) | `hi` | *(empty)* | Count past length clears the segment. |

To **keep** a prefix of length N instead of **dropping** N characters, use [ExtractLeft](ExtractLeft.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "TrimLeft",
  "target": {
    "family": "FileName",
    "fileNamePart": "Prefix"
  },
  "options": {
    "count": 2
  }
}
```
