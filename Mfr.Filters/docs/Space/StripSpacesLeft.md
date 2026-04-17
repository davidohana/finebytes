# StripSpacesLeft

Removes the **word separator** from the **start** of the segment only. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| (no options)<br>default word separator | `   New_York__.jpg` | `New_York__.jpg` | |
| (no options)<br>default word separator | `  a b ` | `a b ` | |
| (no options)<br>default word separator | `    ` | *(empty)* | |
| [SpaceCharacter](SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>then StripSpacesLeft | `__New_York__.jpg` | `New_York__.jpg` | |

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). No `options` property.

```json
{
  "type": "StripSpacesLeft",
  "target": {
    "family": "FileName",
    "fileNamePart": "Prefix"
  }
}
```
