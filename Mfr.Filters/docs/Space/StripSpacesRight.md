# StripSpacesRight

Removes the **word separator** from the **end** of the segment only. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object. Examples match [`StripSpacesRightFilterTests`](../../../Mfr.Tests/Models/Filters/Space/StripSpacesRightFilterTests.cs).

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| (none), default space separator | `New_York__   ` | `New_York__` | Trailing spaces only. |
| (none), default space separator | `  a b ` | `  a b` | Leading spaces remain. |
| (none), default space separator | `    ` | *(empty)* | All characters were separators. |
| Chain: [SpaceCharacter](SpaceCharacter.md) `spaceCharacter`: `"_"` then StripSpacesRight | `__New_York__` | `__New_York` | Trailing underscores stripped. |
