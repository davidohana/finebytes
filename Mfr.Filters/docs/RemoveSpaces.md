# RemoveSpaces

Removes **every** occurrence of the current **word separator** character from the segment. The separator is set by [SpaceCharacter](SpaceCharacter.md); if that filter has not run, it is usually ordinary space.

No `options` object.

## Examples

| Options | Before | After |
|---------|--------|-------|
| (none), default space separator | `a b c` | `abc` |
| (none), after [SpaceCharacter](SpaceCharacter.md) with `_` as separator | `a_b_c` | `abc` |
