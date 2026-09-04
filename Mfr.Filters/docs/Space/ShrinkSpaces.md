# ShrinkSpaces

Collapses **each run of two or more** of the current **word separator** into a **single** character (MFR7 `{2,}`). The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object.

## Examples

- (no options); default word separator: `a   b  c` → `a b c`
- (no options); default word separator
  - Before: `a  \t b`
  - After: `a \t b`
  - Comment: Tab is not the configured separator; spaces around it collapse separately.

Often used with [SpaceCharacter](SpaceCharacter.md) to normalize messy separators.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). No `options` property.

```json
{
  "type": "ShrinkSpaces",
  "target": {
    "targetType": "FilePrefix"
  }
}
```
