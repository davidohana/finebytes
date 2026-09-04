# ShrinkDuplicateCharacters

Collapses **runs of two or more** of the same chosen character into a **single** occurrence (for example `---` → `-`; MFR7 `{2,}`).

## Options

- **`character`** (string or char)
  - The character to deduplicate; typically one character (first character wins if a longer string is provided).
  - Empty / null character (`\0`) is a no-op (MFR7 empty editor).

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
