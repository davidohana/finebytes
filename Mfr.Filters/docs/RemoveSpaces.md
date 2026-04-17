# RemoveSpaces

Removes **every** occurrence of the current **word separator** character from the segment (no placeholder). The separator is the one set by [SpaceCharacter](SpaceCharacter.md); if that filter has not run, it is usually ordinary space.

This filter has **no options**—only `enabled` and `target`.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `RemoveSpaces`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |

## Examples

**Default separator (space)**

- Input: `a b c` → `abc`

**After SpaceCharacter (`_`)**

- Input: `a_b_c` → `abc`

**Example preset fragment**

```json
{
  "type": "RemoveSpaces",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" }
}
```
