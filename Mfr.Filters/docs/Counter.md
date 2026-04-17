# Counter

Computes a numeric value from each file’s **global** or **per-folder** index and **prepends**, **appends**, or **replaces** the target segment with the formatted number.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `Counter`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `start` | int | First counter value for index `0`. |
| `step` | int | Added to the counter for each step in index (`value = start + step * n`). |
| `width` | int | Minimum width; the numeric string is left-padded to at least this length (see `padChar`). Use `0` for no padding. |
| `padChar` | string | Padding selector: `"0"` → pad with `0`; `"1"` → pad with space; any other non-empty string uses its **first** character; empty uses `0`. |
| `position` | string (enum) | `Replace` (output is only the number), `Prepend`, or `Append`. |
| `separator` | string | Inserted between the counter and the original segment for `Prepend` / `Append` (for example `" - "`). |
| `resetPerFolder` | bool | If `true`, `n` is the file’s index **within its folder**; if `false`, `n` is the **global** index across all files. |

### Position (`position`)

| Value | Result |
|--------|--------|
| `Replace` | Segment becomes only the formatted counter. |
| `Prepend` | `formatted + separator + originalSegment` |
| `Append` | `originalSegment + separator + formatted` |

## Examples

**Padded track numbers (replace)**

- `start`: `1`, `step`: `1`, `width`: `3`, `padChar`: `"0"`, `position`: `Replace`  
- First file: `001`, second: `002`, …

**Append with separator**

- `position`: `Append`, `separator`: `"_"`, `width`: `2`, …  
- Segment `song` → `song_01` (depending on indices and start/step).

**Per-folder numbering**

- `resetPerFolder`: `true` — each folder’s files count from `start` again.

**Example preset fragment**

```json
{
  "type": "Counter",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "start": 1,
    "step": 1,
    "width": 3,
    "padChar": "0",
    "position": "Prepend",
    "separator": " - ",
    "resetPerFolder": false
  }
}
```

For templates mixing text and `<file-name>`-style tokens, see [Formatter](Formatter.md).
