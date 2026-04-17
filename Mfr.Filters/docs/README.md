# MFR filter guides

These notes describe the rename filters in this assembly. In preset JSON, each filter has a `type` field and a `target` that selects which part of the file name is processed.

**Order matters.** Some filters only affect *later* filters (for example `SpaceCharacter` sets the word separator; `SentenceEndCharacters` sets sentence-ending punctuation). Put those *before* the filters that should use the new settings.

## Filters

### [Case filters](case-filters.md)

- [LettersCase](case-filters.md#letterscase)
- [CapitalizeAfter](case-filters.md#capitalizeafter)
- [UppercaseInitials](case-filters.md#uppercaseinitials)
- [CasingList](case-filters.md#casinglist)
- [SentenceEndCharacters](case-filters.md#sentenceendcharacters)

### [Formatting filters](formatting-filters.md)

- [Formatter](formatting-filters.md#formatter)
- [Counter](formatting-filters.md#counter)

### [Misc filters](misc-filters.md)

- [StripParentheses](misc-filters.md#stripparentheses)
- [FixLeadingZeros](misc-filters.md#fixleadingzeros)

### [Replace filters](replace-filters.md)

- [Replacer](replace-filters.md#replacer)
- [ReplaceList](replace-filters.md#replacelist)
- [Cleaner](replace-filters.md#cleaner)

### [Space filters](space-filters.md)

- [SpaceCharacter](space-filters.md#spacecharacter)
- [RemoveSpaces](space-filters.md#removespaces)
- [ShrinkSpaces](space-filters.md#shrinkspaces)
- [StripSpacesLeft](space-filters.md#stripspacesleft)
- [StripSpacesRight](space-filters.md#stripspacesright)

### [Trimming filters](trimming-filters.md)

- [TrimLeft](trimming-filters.md#trimleft)
- [TrimRight](trimming-filters.md#trimright)
- [ExtractLeft](trimming-filters.md#extractleft)
- [ExtractRight](trimming-filters.md#extractright)
- [TrimBetween](trimming-filters.md#trimbetween)
- [ShrinkDuplicateCharacters](trimming-filters.md#shrinkduplicatecharacters)
