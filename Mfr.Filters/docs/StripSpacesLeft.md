# StripSpacesLeft

Removes the **word separator** character from the **beginning** of the segment only (trim start). The separator is from [SpaceCharacter](SpaceCharacter.md) (default: space).

This filter has **no options**—only `enabled` and `target`.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `StripSpacesLeft`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |

## Examples

- Input: `  hello` → `hello`

**Example preset fragment**

```json
{
  "type": "StripSpacesLeft",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" }
}
```
