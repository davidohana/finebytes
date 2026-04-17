# UppercaseInitials

Detects **initials-style** patterns: one or more single letters separated by dots (for example `u.s.a`, `d.j`, `e.x.a.m.p.l.e`). Only the letters inside those patterns are uppercased; the rest of the segment is left as-is.

This filter has **no options object**—only `enabled` and `target`.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `UppercaseInitials`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | File name part or other supported target (see [LettersCase](LettersCase.md)). |

## Examples

- Input: `track u.s.a mix` → Output: `track U.S.A mix`
- Input: `no.dots.here` → may match long dot-separated single letters if the pattern matches; ordinary words without the initials pattern stay unchanged.

**Example preset fragment**

```json
{
  "type": "UppercaseInitials",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" }
}
```
