# ShrinkSpaces

Collapses **each run** of the current **word separator** into a **single** character. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

This filter has **no options**—only `enabled` and `target`.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `ShrinkSpaces`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |

## Examples

**Spaces**

- Input: `hello    world` → `hello world`

**Underscores (after SpaceCharacter)**

- With separator `_`: `a___b` → `a_b`

**Example preset fragment**

```json
{
  "type": "ShrinkSpaces",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" }
}
```

Often used together with [SpaceCharacter](SpaceCharacter.md) to normalize messy separators.
