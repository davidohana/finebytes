# ShrinkDuplicateCharacters

Collapses **runs** of the same chosen character into a **single** occurrence (for example `---` → `-`).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `character` | string or char | The character to deduplicate; typically one character (first character wins if a longer string is provided). |

## Examples

| Options | Before | After |
|---------|--------|-------|
| `character`: `"-"` | `a---b` | `a-b` |
| `character`: `"."` | `foo...bar` | `foo.bar` |
