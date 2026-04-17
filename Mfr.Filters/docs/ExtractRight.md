# ExtractRight

Keeps only the **last** `count` characters of the segment; the leading part is removed.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | Number of characters to keep from the right (clamped 0…length). |

## Examples

- `count`: `3` — `ABCDEF` → `DEF`

See [TrimRight](TrimRight.md) to **drop** a fixed number of characters from the right instead.

```json
{ "count": 3 }
```
