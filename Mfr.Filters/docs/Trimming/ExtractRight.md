# ExtractRight

Keeps only the **last** `count` characters of the segment; the leading part is removed.

Examples match [`ExtractRightFilterTests`](../../../Mfr.Tests/Models/Filters/Trimming/ExtractRightFilterTests.cs).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | Number of characters to keep from the right (clamped 0…length). |

## Examples

| Options | Before | After |
|---------|--------|-------|
| `count`: `3` | `abcdef` | `def` |
| `count`: `0` | `abc` | *(empty)* |
| `count`: `100` (segment shorter) | `ab` | `ab` |

See [TrimRight](TrimRight.md) to **drop** a fixed number of characters from the right instead.
