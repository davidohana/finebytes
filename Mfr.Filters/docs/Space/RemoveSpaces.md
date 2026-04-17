# RemoveSpaces

Removes **every** occurrence of the current **word separator** character from the segment. The separator is set by [SpaceCharacter](SpaceCharacter.md); if that filter has not run, it is usually ordinary space.

No `options` object. Examples match [`RemoveSpacesFilterTests`](../../../Mfr.Tests/Models/Filters/Space/RemoveSpacesFilterTests.cs).

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| (no options)<br>default word separator | `a b` | `ab` | |
| (no options)<br>default word separator | `a \t\r\nb` | `a\t\r\nb` | Only the word-separator char (space) removed, not `\t`/`\r`/`\n`. |

After [SpaceCharacter](SpaceCharacter.md) with `_` as separator, each `_` is removed the same way.
