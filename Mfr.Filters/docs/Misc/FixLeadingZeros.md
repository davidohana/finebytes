# FixLeadingZeros

Finds **runs of digits** in the segment and rewrites them so their length matches the target **width** by adjusting **leading zeros**. If `width` is `0` or negative, the filter leaves the segment unchanged.

Examples match [`FixLeadingZerosFilterTests`](../../../Mfr.Tests/Models/Filters/Misc/FixLeadingZerosFilterTests.cs) (track numbers, `doc` + chapter, classical track titles).

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
| `width`: `0`, `removeExtraZeros`: `true` | `track12` | `track12` | Non-positive width leaves segment unchanged. |
| `width`: `4`, `removeExtraZeros`: `false` | `track9` | `track0009` | Pad digit run to four digits. |
| `width`: `3`, `removeExtraZeros`: `true` | `x0007` | `x007` | Strip extra leading zeros, then pad to width. |
| `width`: `3`, `removeExtraZeros`: `false`, `wholeWordOnly`: `true` | `doc1_12` | `doc1_012` | Only `12` is a “whole word” digit group; `1` in `doc1` skipped. |
| `width`: `3`, `removeExtraZeros`: `false`, `maxCount`: `1` | `05-Opus 40` | `005-Opus 40` | Only first digit group normalized. |
| `width`: `3`, `removeExtraZeros`: `false`, `maxCount`: `2` | `05-Opus 40 (1)` | `005-Opus 040 (1)` | First two digit groups get width `3`. |
