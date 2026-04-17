# TrimLeft

Removes a fixed number of characters from the **left** end of the segment. The count is clamped to the segment length.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | How many characters to remove from the start (minimum 0; values beyond length are treated as length). |

## Examples

| Options | Before | After |
|---------|--------|-------|
| `count`: `3` | `ABCDEF` | `DEF` |
| `count`: `0` | `ABCDEF` | `ABCDEF` |
| `count`: `10` (segment length `6`) | `ABCDEF` | *(empty)* |

To **keep** a prefix of length N instead of **dropping** N characters, use [ExtractLeft](ExtractLeft.md).
