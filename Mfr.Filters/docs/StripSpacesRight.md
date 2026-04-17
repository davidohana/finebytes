# StripSpacesRight

Removes the **word separator** character from the **end** of the segment only (trim end). The separator is from [SpaceCharacter](SpaceCharacter.md) (default: space).

This filter has **no options**—only `enabled` and `target`.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `StripSpacesRight`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |

## Examples

- Input: `hello  ` → `hello`

**Example preset fragment**

```json
{
  "type": "StripSpacesRight",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" }
}
```
