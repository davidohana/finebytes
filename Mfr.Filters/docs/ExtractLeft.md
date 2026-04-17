# ExtractLeft

Keeps only the **first** `count` characters of the segment; the rest is removed.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `count` | int | Number of characters to keep from the left (clamped 0…length). |

## Examples

- `count`: `4` — `ABCDEF` → `ABCD`
- `count`: `0` — empty string

See [TrimLeft](TrimLeft.md) to **drop** a fixed number of characters from the left instead.

```json
{ "count": 4 }
```
