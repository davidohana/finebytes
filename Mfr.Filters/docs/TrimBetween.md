# TrimBetween

Deletes an **inclusive** range of character positions. Each end is a **1-based** index counted from the **left** or **right** side of the string. If the start lies after the end, the two positions are **swapped** before removal.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `start` | object | `{ "value": int, "anchor": "Left" \| "Right" }` — 1-based from that anchor. |
| `end` | object | Same shape as `start`. |

**Anchors:** `Left` — position `1` is the first character; `Right` — position `1` is the last character. Indices are clamped to the string.

## Examples

- Segment `ABCDEF`, `start` `{ "value": 3, "anchor": "Left" }`, `end` `{ "value": 5, "anchor": "Left" }` — removes positions 3–5 → `ABF`.

```json
{
  "start": { "value": 3, "anchor": "Left" },
  "end": { "value": 5, "anchor": "Left" }
}
```
