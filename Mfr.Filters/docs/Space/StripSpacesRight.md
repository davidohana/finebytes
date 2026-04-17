# StripSpacesRight

Removes the **word separator** from the **end** of the segment only. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| (no options)<br>default word separator | `New_York__   ` | `New_York__` | |
| (no options)<br>default word separator | `  a b ` | `  a b` | |
| (no options)<br>default word separator | `    ` | *(empty)* | |
| [SpaceCharacter](SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>then StripSpacesRight | `__New_York__` | `__New_York` | |
