# TrimRight

Removes a fixed number of characters from the **right** end of the segment. The count is clamped to the segment length.

Examples match [`TrimRightFilterTests`](../../../Mfr.Tests/Models/Filters/Trimming/TrimRightFilterTests.cs).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | How many characters to remove from the end. |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `count`: `2` | `abcd` | `ab` | |
| `count`: `0` | `ab` | `ab` | |
| `count`: `10` (segment shorter) | `hi` | *(empty)* | Count past length clears the segment. |

To **keep** only the last N characters, use [ExtractRight](ExtractRight.md).
