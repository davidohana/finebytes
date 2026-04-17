# ShrinkSpaces

Collapses **each run** of the current **word separator** into a **single** character. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object. Examples match [`ShrinkSpacesFilterTests`](../../../Mfr.Tests/Models/Filters/Space/ShrinkSpacesFilterTests.cs).

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| (none), default space separator | `a   b  c` | `a b c` | |
| (none), default space separator | `a  \t b` | `a \t b` | Tab is not the configured separator; spaces around it collapse separately. |

Often used with [SpaceCharacter](SpaceCharacter.md) to normalize messy separators.
