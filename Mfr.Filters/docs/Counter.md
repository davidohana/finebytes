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

- `start`: `1`, `step`: `1`, `width`: `3`, `padChar`: `"0"`, `position`: `Replace` — first file `001`, second `002`, …
- `position`: `Append`, `separator`: `"_"`, `width`: `2` — e.g. `song` → `song_01` (depending on indices).
- `resetPerFolder`: `true` — each folder counts from `start` again.

For templates with `<file-name>`-style tokens, see [Formatter](Formatter.md).

```json
{
  "start": 1,
  "step": 1,
  "width": 3,
  "padChar": "0",
  "position": "Prepend",
  "separator": " - ",
  "resetPerFolder": false
}
```
