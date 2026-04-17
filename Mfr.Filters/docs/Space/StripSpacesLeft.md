# StripSpacesLeft

Removes the **word separator** from the **start** of the segment only. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object. Examples match [`StripSpacesLeftFilterTests`](../../../Mfr.Tests/Models/Filters/Space/StripSpacesLeftFilterTests.cs).

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| (none), default space separator | `   New_York__.jpg` | `New_York__.jpg` | Leading spaces only. |
| (none), default space separator | `  a b ` | `a b ` | Trailing spaces remain. |
| (none), default space separator | `    ` | *(empty)* | All characters were separators. |
| Chain: [SpaceCharacter](SpaceCharacter.md) `spaceCharacter`: `"_"` then StripSpacesLeft | `__New_York__.jpg` | `New_York__.jpg` | Leading underscores stripped when `_` is the separator. |
