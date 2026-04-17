# ExtractLeft

Keeps only the **first** `count` characters of the segment; the rest is removed.

Examples match [`ExtractLeftFilterTests`](../../../Mfr.Tests/Models/Filters/Trimming/ExtractLeftFilterTests.cs).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | Number of characters to keep from the left (clamped 0…length). |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `count`: `3` | `abcdef` | `abc` | Keep first three characters. |
| `count`: `0` | `abc` | *(empty)* | Zero length extraction. |
| `count`: `100` (segment shorter) | `ab` | `ab` | Count clamped to string length. |

See [TrimLeft](TrimLeft.md) to **drop** a fixed number of characters from the left instead.
