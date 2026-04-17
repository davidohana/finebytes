# ShrinkSpaces

Collapses **each run** of the current **word separator** into a **single** character. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object.

## Examples

| Options | Before | After |
|---------|--------|-------|
| (none), default space separator | `hello    world` | `hello world` |
| (none), after [SpaceCharacter](SpaceCharacter.md) with `_` | `a___b` | `a_b` |

Often used with [SpaceCharacter](SpaceCharacter.md) to normalize messy separators.
