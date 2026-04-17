# MFR filter guides

These notes describe the rename filters in this assembly. In preset JSON, each filter has a `type` field and a `target` that selects which part of the file name is processed.

**Order matters.** Some filters only affect *later* filters (for example `SpaceCharacter` sets the word separator; `SentenceEndCharacters` sets sentence-ending punctuation). Put those *before* the filters that should use the new settings.

## Filters by group

### Case

- [CapitalizeAfter](CapitalizeAfter.md)
- [CasingList](CasingList.md)
- [LettersCase](LettersCase.md)
- [SentenceEndCharacters](SentenceEndCharacters.md)
- [UppercaseInitials](UppercaseInitials.md)

### Formatting

- [Counter](Counter.md)
- [Formatter](Formatter.md)

### Misc

- [FixLeadingZeros](FixLeadingZeros.md)
- [StripParentheses](StripParentheses.md)

### Replace

- [Cleaner](Cleaner.md)
- [Replacer](Replacer.md)
- [ReplaceList](ReplaceList.md)

### Space

- [RemoveSpaces](RemoveSpaces.md)
- [ShrinkSpaces](ShrinkSpaces.md)
- [SpaceCharacter](SpaceCharacter.md)
- [StripSpacesLeft](StripSpacesLeft.md)
- [StripSpacesRight](StripSpacesRight.md)

### Trimming

- [ExtractLeft](ExtractLeft.md)
- [ExtractRight](ExtractRight.md)
- [ShrinkDuplicateCharacters](ShrinkDuplicateCharacters.md)
- [TrimBetween](TrimBetween.md)
- [TrimLeft](TrimLeft.md)
- [TrimRight](TrimRight.md)
