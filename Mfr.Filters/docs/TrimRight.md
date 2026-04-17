# TrimRight

Removes a fixed number of characters from the **right** end of the segment. The count is clamped to the segment length.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | How many characters to remove from the end. |

## Examples

| Options | Before | After |
|---------|--------|-------|
| `count`: `2` | `ABCDEF` | `ABCD` |

To **keep** only the last N characters, use [ExtractRight](ExtractRight.md).
