# StripSpacesLeft

Removes the **word separator** from the **start** of the segment only. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object.

## Examples

- (no options); default word separator: `   New_York__.jpg` → `New_York__.jpg`
- (no options); default word separator: ` a b` → `a b `
- (no options); default word separator: `    ` → _(empty)_
- [SpaceCharacter](SpaceCharacter.md); `spaceCharacter`: `"_"`; then StripSpacesLeft
  - Before: `__New_York__.jpg`
  - After: `New_York__.jpg`

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). No `options` property.

```json
{
  "type": "StripSpacesLeft",
  "target": {
    "targetType": "FilePrefix"
  }
}
```
