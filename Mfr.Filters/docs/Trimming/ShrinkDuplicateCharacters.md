# ShrinkDuplicateCharacters

Collapses **runs** of the same chosen character into a **single** occurrence (for example `---` → `-`).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `character` | string or char | The character to deduplicate; typically one character (first character wins if a longer string is provided). |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `character`: `"-"` | `I am Kloot --- To You` | `I am Kloot - To You` | |
| `character`: `"-"` | `a--b---c` | `a-b-c` | |
| `character`: `">"` | `a>>b>>>c` | `a>b>c` | |

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
