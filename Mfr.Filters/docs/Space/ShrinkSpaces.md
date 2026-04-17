# ShrinkSpaces

Collapses **each run** of the current **word separator** into a **single** character. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| (no options)<br>default word separator | `a   b  c` | `a b c` | |
| (no options)<br>default word separator | `a  \t b` | `a \t b` | Tab is not the configured separator; spaces around it collapse separately. |

Often used with [SpaceCharacter](SpaceCharacter.md) to normalize messy separators.
