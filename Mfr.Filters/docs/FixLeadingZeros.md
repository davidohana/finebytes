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

| Options | Before | After |
|---------|--------|-------|
| `width`: `2`, `removeExtraZeros`: `false`, `maxCount`: `0`, `wholeWordOnly`: `false` | `9` | `09` |
| `width`: `2`, `removeExtraZeros`: `false` | `09` | `09` |
| `width`: `2`, `removeExtraZeros`: `true`, `maxCount`: `0`, `wholeWordOnly`: `false` | `0009` | `09` |
