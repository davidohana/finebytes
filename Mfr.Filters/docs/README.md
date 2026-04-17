# MFR filter guides

These pages document **per-filter `options`** (and behavior). Common preset fields are described once here.

## Preset shape

Each filter in a preset has:

- `type` — discriminator string (same name as the filter, e.g. `LettersCase`).
- `enabled` — if `false`, the filter is skipped.
- `target` — what is transformed (for file names: `{ "family": "FileName", "fileNamePart": "Prefix" | "Extension" | "Full" }` — **Prefix** = name without extension, **Extension** = extension including the dot, **Full** = full file name).
- `options` — optional object; filters with no settings omit it.

Property names use **camelCase**; enum values usually match the C# names (e.g. `TitleCase`, `Literal`).

Each filter page uses an **Examples** table with four columns: **Options**, **Before**, **After**, and **Comment**. Use **Comment** only when the row is non-obvious (edge case, chain order, or why the result differs from a first guess); otherwise leave it empty. In **Options**, put each option property on its **own line** (HTML `<br>` in the markdown source).

**Order matters.** Some filters only affect *later* filters (for example `SpaceCharacter` sets the word separator; `SentenceEndCharacters` sets sentence-ending punctuation). Put those *before* the filters that should use the new settings.

## Filters by group

### Case

- [CapitalizeAfter](Case/CapitalizeAfter.md)
- [CasingList](Case/CasingList.md)
- [LettersCase](Case/LettersCase.md)
- [SentenceEndCharacters](Case/SentenceEndCharacters.md)
- [UppercaseInitials](Case/UppercaseInitials.md)

### Formatting

- [Counter](Formatting/Counter.md)
- [Formatter](Formatting/Formatter.md)

### Misc

- [FixLeadingZeros](Misc/FixLeadingZeros.md)
- [StripParentheses](Misc/StripParentheses.md)

### Replace

- [Cleaner](Replace/Cleaner.md)
- [Replacer](Replace/Replacer.md)
- [ReplaceList](Replace/ReplaceList.md)

### Space

- [RemoveSpaces](Space/RemoveSpaces.md)
- [SeparateCapitalizedText](Space/SeparateCapitalizedText.md)
- [ShrinkSpaces](Space/ShrinkSpaces.md)
- [SpaceCharacter](Space/SpaceCharacter.md)
- [StripSpacesLeft](Space/StripSpacesLeft.md)
- [StripSpacesRight](Space/StripSpacesRight.md)

### Trimming

- [ExtractLeft](Trimming/ExtractLeft.md)
- [ExtractRight](Trimming/ExtractRight.md)
- [ShrinkDuplicateCharacters](Trimming/ShrinkDuplicateCharacters.md)
- [TrimBetween](Trimming/TrimBetween.md)
- [TrimLeft](Trimming/TrimLeft.md)
- [TrimRight](Trimming/TrimRight.md)
