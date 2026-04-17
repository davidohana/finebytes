# FixLeadingZeros

Finds **runs of digits** in the segment and rewrites them so their length matches the target **width** by adjusting **leading zeros**. If `width` is `0` or negative, the filter leaves the segment unchanged.

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `width` | int | (required) | Desired minimum digit count after normalization. |
| `removeExtraZeros` | bool | (required) | If `true`, strip leading zeros from the match first, then pad to `width` when shorter. |
| `maxCount` | int | `0` | Maximum number of digit groups to change; `0` = all matches. |
| `wholeWordOnly` | bool | `false` | If `true`, skip digit groups that have a letter immediately before or after. |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `width`: `0`<br>`removeExtraZeros`: `true` | `track12` | `track12` | Non-positive `width` → no change. |
| `width`: `4`<br>`removeExtraZeros`: `false` | `track9` | `track0009` | |
| `width`: `3`<br>`removeExtraZeros`: `true` | `x0007` | `x007` | |
| `width`: `3`<br>`removeExtraZeros`: `false`<br>`wholeWordOnly`: `true` | `doc1_12` | `doc1_012` | `1` in `doc1` is not a whole-word digit group. |
| `width`: `3`<br>`removeExtraZeros`: `false`<br>`maxCount`: `1` | `05-Opus 40` | `005-Opus 40` | Only first digit run affected. |
| `width`: `3`<br>`removeExtraZeros`: `false`<br>`maxCount`: `2` | `05-Opus 40 (1)` | `005-Opus 040 (1)` | |

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "FixLeadingZeros",
  "target": {
    "family": "FileName",
    "fileNamePart": "Prefix"
  },
  "options": {
    "width": 3,
    "removeExtraZeros": true,
    "maxCount": 0,
    "wholeWordOnly": false
  }
}
```
