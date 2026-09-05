# StripParentheses

Removes **one kind** of bracket pair: round `()`, square `[]`, curly `{}`, or angle `<>`.
Either delete **only the delimiters** or **the whole bracketed region** (delimiters + inside),
depending on options. Matched pairs are stripped innermost-first (MFR7 parity); unmatched
open or close characters are left alone.

## Options

- **`type`** (string (enum))
  - Bracket style: `Round`, `Square`, `Curly`, or `Angle`. (This is the `type` field **inside** `options`, not the
    filter’s top-level `type`.)
- **`removeContents`** (bool)
  - If `true`, remove opening + closing + everything between. If `false`, remove only the opening and closing
    characters of matched pairs (content stays).

## Examples

| Options                                      | Before      | After  | Comment                                |
| -------------------------------------------- | ----------- | ------ | -------------------------------------- |
| `type`: `Round`<br>`removeContents`: `true`  | `a(rem)b`   | `ab`   |                                        |
| `type`: `Round`<br>`removeContents`: `false` | `a(rem)`    | `arem` | Delimiters removed; inner text kept.   |
| `type`: `Square`<br>`removeContents`: `true` | `a[xx]b`    | `ab`   |                                        |
| `type`: `Round`<br>`removeContents`: `true`  | `a(b(c)d)e` | `ae`   | Nested pairs stripped innermost-first. |
| `type`: `Round`<br>`removeContents`: `true`  | `a(b`       | `a(b`  | Unmatched open left alone.             |

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). The `type` under `options` is bracket style (not the filter discriminator).

```json
{
  "type": "StripParentheses",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "type": "Round",
    "removeContents": true
  }
}
```
