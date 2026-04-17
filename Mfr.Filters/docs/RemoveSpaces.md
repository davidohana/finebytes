# RemoveSpaces

Removes **every** occurrence of the current **word separator** character from the segment. The separator is set by [SpaceCharacter](SpaceCharacter.md); if that filter has not run, it is usually ordinary space.

No `options` object.

## Examples

- Default separator: `a b c` → `abc`
- After [SpaceCharacter](SpaceCharacter.md) with `_`: `a_b_c` → `abc`
