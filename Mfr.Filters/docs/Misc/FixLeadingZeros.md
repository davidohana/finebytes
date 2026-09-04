# FixLeadingZeros

Finds **runs of digits** in the segment and rewrites them so their length matches the target **width** by adjusting **leading zeros**. If `width` is `0` or negative, the filter leaves the segment unchanged.

## Options

- **`width`** (int, required) — Desired minimum digit count after normalization.
- **`removeExtraZeros`** (bool, required)
  - If `true`, strip leading zeros from the match first, then pad to `width` when shorter.
- **`maxCount`** (int, default `0`) — Maximum number of digit groups to change; `0` = all matches.
- **`wholeWordOnly`** (bool, default `true`)
  - If `true`, skip digit groups that have a letter immediately before or after.

## Examples

- `width`: `0`; `removeExtraZeros`: `true` — `track12` → `track12` — Non-positive `width` → no change.
- `width`: `4`; `removeExtraZeros`: `false`; `wholeWordOnly`: `false` — `track9` → `track0009`
- `width`: `3`; `removeExtraZeros`: `true`; `wholeWordOnly`: `false` — `x0007` → `x007`
- `width`: `3`; `removeExtraZeros`: `false`; `wholeWordOnly`: `true`
  - Before: `doc1_12`
  - After: `doc1_012`
  - Comment: `1` in `doc1` is not a whole-word digit group.
- `width`: `3`; `removeExtraZeros`: `false`; `maxCount`: `1`
  - Before: `05-Opus 40`
  - After: `005-Opus 40`
  - Comment: Only first digit run affected.
- `width`: `3`; `removeExtraZeros`: `false`; `maxCount`: `2` — `05-Opus 40 (1)` → `005-Opus 040 (1)`

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "FixLeadingZeros",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "width": 3,
    "removeExtraZeros": true,
    "maxCount": 0,
    "wholeWordOnly": true
  }
}
```
