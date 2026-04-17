# MFR filter guides

These pages document **per-filter `options`** (and behavior). Common preset fields are described once here.

## Preset shape

Each filter in a preset has:

- `type` — discriminator string (same name as the filter, e.g. `LettersCase`).
- `enabled` — if `false`, the filter is skipped.
- `target` — what is transformed (for file names: `{ "family": "FileName", "fileNamePart": "Prefix" | "Extension" | "Full" }` — **Prefix** = name without extension, **Extension** = extension including the dot, **Full** = full file name).
- `options` — optional object; filters with no settings omit it.

Property names use **camelCase**; enum values usually match the C# names (e.g. `TitleCase`, `Literal`).

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
