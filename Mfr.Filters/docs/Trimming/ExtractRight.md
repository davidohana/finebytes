# ExtractRight

Keeps only the **last** `count` characters of the segment; the leading part is removed.

Examples match [`ExtractRightFilterTests`](../../../Mfr.Tests/Models/Filters/Trimming/ExtractRightFilterTests.cs).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | Number of characters to keep from the right (clamped 0…length). |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `count`: `3` | `abcdef` | `def` | Keep last three characters. |
| `count`: `0` | `abc` | *(empty)* | Zero length extraction. |
| `count`: `100` (segment shorter) | `ab` | `ab` | Count clamped to string length. |

See [TrimRight](TrimRight.md) to **drop** a fixed number of characters from the right instead.
