# ShrinkDuplicateCharacters

Collapses **runs of two or more** of the same chosen character into a **single** occurrence (for example `---` → `-`; MFR7 `{2,}`).

## Options

- **`character`** (char)
  - The character whose adjacent runs of length ≥ 2 are collapsed to one.
  - Preset JSON uses a one-character string (for example `"-"`). Longer or empty JSON strings fail to deserialize.
  - `\0` (Filter Configuration empty box) is a no-op (MFR7 empty editor).

## Examples

| Options            | Before                  | After                 | Comment |
| ------------------ | ----------------------- | --------------------- | ------- |
| `character`: `"-"` | `I am Kloot --- To You` | `I am Kloot - To You` |         |
| `character`: `"-"` | `a--b---c`              | `a-b-c`               |         |
| `character`: `">"` | `a>>b>>>c`              | `a>b>c`               |         |

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "ShrinkDuplicateCharacters",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "character": "-"
  }
}
```
