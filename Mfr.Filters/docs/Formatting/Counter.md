# Counter

Computes a numeric value from each file’s **global** or **per-folder** index and **prepends**, **appends**, or **replaces** the target segment with the formatted number.

Examples match [`CounterFilterTests`](../../../Mfr.Tests/Models/Filters/Formatting/CounterFilterTests.cs) (padded track numbers, `name_02`, per-folder reset).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `start` | int | First counter value for index `0`. |
| `step` | int | `value = start + step * n`. |
| `width` | int | Minimum width; left-pad numeric string (see `padChar`). Use `0` for no padding. |
| `padChar` | string | `"0"` → pad with `0`; `"1"` → pad with space; any other non-empty string uses its **first** character; empty uses `0`. |
| `position` | string (enum) | `Replace`, `Prepend`, or `Append` — see **Position** below. |
| `separator` | string | Between counter and original segment for `Prepend` / `Append`. |
| `resetPerFolder` | bool | If `true`, `n` is the file’s index **within its folder**; if `false`, **global** index. |

### Position (`position`)

| Value | Result |
|--------|--------|
| `Replace` | Segment becomes only the formatted counter. |
| `Prepend` | `formatted + separator + originalSegment` |
| `Append` | `originalSegment + separator + formatted` |

## Examples

Assume **global** index as in each row unless `resetPerFolder` is noted.

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `start`: `1`, `step`: `1`, `width`: `3`, `padChar`: `"0"`, `position`: `Replace`, `resetPerFolder`: `false` (global index `4`) | `old` | `005` | Replace mode: segment is only the padded number (`1 + 4` → `005`). |
| `start`: `0`, `step`: `1`, `width`: `0`, `padChar`: `"0"`, `position`: `Prepend`, `separator`: `"_"`, global index `2` | `name` | `2_name` | Prepend with `_` separator; no width padding when `width` is `0`. |
| `start`: `0`, `step`: `1`, `width`: `0`, `padChar`: `"0"`, `position`: `Append`, `separator`: `"-"`, global index `1` | `name` | `name-1` | Append counter after hyphen. |
| `start`: `10`, `step`: `5`, `width`: `0`, `position`: `Replace`, `resetPerFolder`: `true` (in-folder index `2`) | `x` | `20` | Per-folder index `2`: `10 + 5×2 = 20`. |

For templates with `<file-name>`-style tokens, see [Formatter](Formatter.md).
