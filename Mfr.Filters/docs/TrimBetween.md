# TrimBetween

Deletes an **inclusive** range of character positions. Each end is given as a **1-based** index counted from the **left** or **right** side of the string. If the start lies after the end, the two positions are **swapped** before removal.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `TrimBetween`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `start` | object | Start of range: `{ "value": int, "anchor": "Left" \| "Right" }`. `value` is 1-based from that anchor. |
| `end` | object | End of range: same shape as `start`. |

**Anchors**

- `Left`: position `1` is the first character, `2` the second, …
- `Right`: position `1` is the last character, `2` the second-to-last, …

Indices are clamped to valid positions inside the string.

## Examples

**Delete from the left**

- Segment `ABCDEF` (length 6), `start`: `{ "value": 3, "anchor": "Left" }`, `end`: `{ "value": 5, "anchor": "Left" }` — removes characters at positions 3–5 → `ABF`.

**Using right anchor**

- Remove the last three characters: `start` and `end` both `{ "value": 1, "anchor": "Right" }` through `{ "value": 3, "anchor": "Right" }` depending on desired span (exact indices depend on clamping).

**Example preset fragment**

```json
{
  "type": "TrimBetween",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "start": { "value": 3, "anchor": "Left" },
    "end": { "value": 5, "anchor": "Left" }
  }
}
```
