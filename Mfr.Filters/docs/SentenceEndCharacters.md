# SentenceEndCharacters

**Does not change the text.** It updates the rename item’s **sentence-end character set** for the rest of the filter chain. [LettersCase](LettersCase.md) in **sentence** mode and [CasingList](CasingList.md) with **uppercase sentence initial** read this set.

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `characters` | string | `".!?"` | Each character can end a sentence for sentence-style rules. Characters equal to the current [word separator](SpaceCharacter.md) are ignored when building the set. If empty, only the **start** of the segment gets a capital (for sentence case / sentence initials), not “after punctuation.” |

## Examples

This filter does **not** change the segment text; it only updates settings for later filters.

| Options | Before | After |
|---------|--------|-------|
| `characters`: `".!?"` (default) | `hello` | `hello` |
| `characters`: `"-.!"` | `hello` | `hello` |
| `characters`: `""` | `hello` | `hello` |

Place **before** [LettersCase](LettersCase.md) (sentence mode) or [CasingList](CasingList.md) (sentence initials). The **effect** shows up in those filters, not in the segment here.
