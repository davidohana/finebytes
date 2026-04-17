# TrimBetween

Deletes an **inclusive** range of character positions. Each end is a **1-based** index counted from the **left** or **right** side of the string. If the start lies after the end, the two positions are **swapped** before removal.

The first example is the “Portishead” track-title case from [`TrimBetweenFilterTests.Apply_IssueExample`](../../../Mfr.Tests/Models/Filters/Trimming/TrimBetweenFilterTests.cs).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `start` | object | `{ "value": int, "anchor": "Left" \| "Right" }` — 1-based from that anchor. |
| `end` | object | Same shape as `start`. |

**Anchors:** `Left` — position `1` is the first character; `Right` — position `1` is the last character. Indices are clamped to the string.

## Examples

| Options | Before | After |
|---------|--------|-------|
| `start`: `{ "value": 13, "anchor": "Left" }`, `end`: `{ "value": 5, "anchor": "Right" }` | `Portishead - Glory Box` | `Portishead - Box` |
| `start`: `{ "value": 2, "anchor": "Left" }`, `end`: `{ "value": 4, "anchor": "Left" }` | `abcd` | `a` |
| `start`: `{ "value": 3, "anchor": "Right" }`, `end`: `{ "value": 1, "anchor": "Right" }` | `abcd` | `a` |
| `start`: `{ "value": 1, "anchor": "Left" }`, `end`: `{ "value": 1, "anchor": "Right" }` | `anything` | `` (empty) |
