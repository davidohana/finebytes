# StripSpacesRight

Removes the **word separator** from the **end** of the segment only. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object.

## Examples

- (no options); default word separator: `New_York__   ` → `New_York__`
- (no options); default word separator: ` a b` → `  a b`
- (no options); default word separator: `    ` → _(empty)_
- [SpaceCharacter](SpaceCharacter.md); `spaceCharacter`: `"_"`; then StripSpacesRight: `__New_York__` → `__New_York`

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). No `options` property.

```json
{
  "type": "StripSpacesRight",
  "target": {
    "targetType": "FilePrefix"
  }
}
```
