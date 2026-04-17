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

Assume **global** index as in each row unless `resetPerFolder` is noted.

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `start`: `1`<br>`step`: `1`<br>`width`: `3`<br>`padChar`: `"0"`<br>`position`: `Replace`<br>`resetPerFolder`: `false`<br>global index: `4` | `old` | `005` | |
| `start`: `0`<br>`step`: `1`<br>`width`: `0`<br>`padChar`: `"0"`<br>`position`: `Prepend`<br>`separator`: `"_"`<br>global index: `2` | `name` | `2_name` | |
| `start`: `0`<br>`step`: `1`<br>`width`: `0`<br>`padChar`: `"0"`<br>`position`: `Append`<br>`separator`: `"-"`<br>global index: `1` | `name` | `name-1` | |
| `start`: `10`<br>`step`: `5`<br>`width`: `0`<br>`position`: `Replace`<br>`resetPerFolder`: `true`<br>in-folder index: `2` | `x` | `20` | Uses in-folder index, not global `n`. |

For templates with `<file-name>`-style tokens, see [Formatter](Formatter.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "Counter",
  "target": {
    "family": "FileName",
    "fileNamePart": "Prefix"
  },
  "options": {
    "start": 1,
    "step": 1,
    "width": 3,
    "padChar": "0",
    "position": "Prepend",
    "separator": "_",
    "resetPerFolder": false
  }
}
```
