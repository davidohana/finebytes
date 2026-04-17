# ShrinkDuplicateCharacters

Collapses **runs** of the same chosen character into a **single** occurrence (for example `---` → `-`).

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `ShrinkDuplicateCharacters`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `character` | string or char | The character to deduplicate; typically one character (first character wins if a longer string is provided). |

## Examples

- `character`: `"-"`, input `a---b` → `a-b`
- `character`: `"."`, input `foo...bar` → `foo.bar`

**Example preset fragment**

```json
{
  "type": "ShrinkDuplicateCharacters",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "character": "-"
  }
}
```
