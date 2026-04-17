# MFR filter guides

These notes describe the rename filters in this assembly. In preset JSON, each filter has a `type` field and a `target` that selects which part of the file name is processed.

**Order matters.** Some filters only affect *later* filters (for example `SpaceCharacter` sets the word separator; `SentenceEndCharacters` sets sentence-ending punctuation). Put those *before* the filters that should use the new settings.

## Filters

- [CapitalizeAfter](CapitalizeAfter.md)
- [CasingList](CasingList.md)
- [Cleaner](Cleaner.md)
- [Counter](Counter.md)
- [ExtractLeft](ExtractLeft.md)
- [ExtractRight](ExtractRight.md)
- [FixLeadingZeros](FixLeadingZeros.md)
- [Formatter](Formatter.md)
- [LettersCase](LettersCase.md)
- [RemoveSpaces](RemoveSpaces.md)
- [Replacer](Replacer.md)
- [ReplaceList](ReplaceList.md)
- [SentenceEndCharacters](SentenceEndCharacters.md)
- [ShrinkDuplicateCharacters](ShrinkDuplicateCharacters.md)
- [ShrinkSpaces](ShrinkSpaces.md)
- [SpaceCharacter](SpaceCharacter.md)
- [StripParentheses](StripParentheses.md)
- [StripSpacesLeft](StripSpacesLeft.md)
- [StripSpacesRight](StripSpacesRight.md)
- [TrimBetween](TrimBetween.md)
- [TrimLeft](TrimLeft.md)
- [TrimRight](TrimRight.md)
- [UppercaseInitials](UppercaseInitials.md)
