# Counter

Computes a numeric value from each file’s **global** or **per-folder** index and **prepends**, **appends**, or **replaces** the target segment with the formatted number.

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

Assume **global** index `0` for the first row unless `resetPerFolder` is described.

| Options | Before | After |
|---------|--------|-------|
| `start`: `1`, `step`: `1`, `width`: `3`, `padChar`: `"0"`, `position`: `Replace`, `resetPerFolder`: `false` (index `0`) | `track` | `001` |
| Same + index `1` | `track` | `002` |
| `position`: `Prepend`, `separator`: `" - "`, `start`: `1`, `step`: `1`, `width`: `2`, `padChar`: `"0"`, index `0` | `song` | `01 - song` |
| `position`: `Append`, `separator`: `"_"`, `width`: `2`, `padChar`: `"0"`, `start`: `1`, `step`: `1`, index `0` | `song` | `song_01` |

For templates with `<file-name>`-style tokens, see [Formatter](Formatter.md).
