# RemoveSpaces

Removes **every** occurrence of the current **word separator** character from the segment. The separator is set by [SpaceCharacter](SpaceCharacter.md); if that filter has not run, it is usually ordinary space.

No `options` object.

## Examples

- (no options); default word separator: `a b` → `ab`
- (no options); default word separator
  - Before: `a \t\r\nb`
  - After: `a\t\r\nb`
  - Comment: Only the word-separator char (space) removed, not `\t`/`\r`/`\n`.

After [SpaceCharacter](SpaceCharacter.md) with `_` as separator, each `_` is removed the same way.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). No `options` property.

```json
{
  "type": "RemoveSpaces",
  "target": {
    "targetType": "FilePrefix"
  }
}
```
