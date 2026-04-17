# RemoveSpaces

Removes **every** occurrence of the current **word separator** character from the segment. The separator is set by [SpaceCharacter](SpaceCharacter.md); if that filter has not run, it is usually ordinary space.

No `options` object. Examples match [`RemoveSpacesFilterTests`](../../Mfr.Tests/Models/Filters/Space/RemoveSpacesFilterTests.cs).

## Examples

| Options | Before | After |
|---------|--------|-------|
| (none), default space separator | `a b` | `ab` |
| (none), default space separator (only U+0020 removed; tabs/newlines kept) | `a \t\r\nb` | `a\t\r\nb` |

After [SpaceCharacter](SpaceCharacter.md) with `_` as separator, each `_` is removed the same way.
