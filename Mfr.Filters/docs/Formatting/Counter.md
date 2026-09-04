# Counter

Computes a numeric value from each file’s **global** or **per-folder** index and **prepends**, **appends**, or **replaces** the target segment with the formatted number.

## Options

- **`start`** (int) — First counter value for index `0`.
- **`step`** (int) — `value = start + step * n`.
- **`leadingZerosMode`** (string (enum)) — `None`, `Automatic`, or `Custom` — see **Leading zeros** below.
- **`customLength`** (int) — Digit width when `leadingZerosMode` is `Custom` (minimum `1`). Ignored for other modes.
- **`position`** (string (enum)) — `Replace`, `Prepend`, or `Append` — see **Position** below.
- **`separator`** (string) — Between counter and original segment for `Prepend` / `Append`.
- **`resetPerFolder`** (bool) — If `true`, `n` is the file’s index **within its folder**; if `false`, **global** index.

### Leading zeros (`leadingZerosMode`)

| Value       | Result                                                                                          |
| ----------- | ----------------------------------------------------------------------------------------------- |
| `None`      | No padding.                                                                                     |
| `Automatic` | Left-pad so every value in the active list scope (global or per-folder) shares one digit width. |
| `Custom`    | Left-pad to `customLength` digits.                                                              |

Padding always uses digit `0`. Negative values keep the sign before the padded digits (e.g. `-005`).

### Position (`position`)

| Value     | Result                                      |
| --------- | ------------------------------------------- |
| `Replace` | Segment becomes only the formatted counter. |
| `Prepend` | `formatted + separator + originalSegment`   |
| `Append`  | `originalSegment + separator + formatted`   |

## Examples

Assume **global** index as in each row unless `resetPerFolder` is noted.

- `start`: `1`; `step`: `1`; `leadingZerosMode`: `Custom`; `customLength`: `3`; `position`: `Replace`;
  `resetPerFolder`: `false`; global index: `4`
  - Before: `old`
  - After: `005`
- `start`: `0`; `step`: `1`; `leadingZerosMode`: `None`; `position`: `Prepend`; `separator`: `"_"`; global index:
  `2`
  - Before: `name`
  - After: `2_name`
- `start`: `0`; `step`: `1`; `leadingZerosMode`: `None`; `position`: `Append`; `separator`: `"-"`; global index: `1`
  - Before: `name`
  - After: `name-1`
- `start`: `10`; `step`: `5`; `leadingZerosMode`: `None`; `position`: `Replace`; `resetPerFolder`: `true`; in-folder
  index: `2`
  - Before: `x`
  - After: `20`
  - Comment: Uses in-folder index, not global `n`.
- `start`: `1`; `step`: `1`; `leadingZerosMode`: `Automatic`; `position`: `Replace`; rename-list total: `100`; global
  index: `0`
  - Before: `x`
  - After: `001`
  - Comment: Width fits values `1`…`100`.
- `start`: `-5`; `step`: `1`; `leadingZerosMode`: `Custom`; `customLength`: `3`; `position`: `Replace`; global
  index: `0`
  - Before: `x`
  - After: `-005`

For templates with `<file-name>`-style tokens, see [Formatter](Formatter.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "Counter",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "start": 1,
    "step": 1,
    "leadingZerosMode": "Custom",
    "customLength": 3,
    "position": "Prepend",
    "separator": "_",
    "resetPerFolder": false
  }
}
```
