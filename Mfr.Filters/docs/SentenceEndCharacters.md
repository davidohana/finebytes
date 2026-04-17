# SentenceEndCharacters

**Does not change the segment text.** It updates the rename item’s **sentence-end character set** for the rest of the filter chain. [LettersCase](LettersCase.md) in **sentence** mode and [CasingList](CasingList.md) with **uppercase sentence initial** read this set.

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `characters` | string | `".!?"` | Each character can end a sentence for sentence-style rules. Characters equal to the current [word separator](SpaceCharacter.md) are ignored when building the set. If empty, only the **start** of the segment gets a capital (for sentence case / sentence initials), not “after punctuation.” |

## Examples

Segment text is unchanged by this filter alone; the table shows **Before = After** for the segment. The last row shows the **effect on a following** [LettersCase](LettersCase.md) `SentenceCase` step (from [`SentenceEndCharactersFilterTests`](../../Mfr.Tests/Models/Filters/Case/SentenceEndCharactersFilterTests.cs)).

| Options | Before | After |
|---------|--------|-------|
| `characters`: `":;"` | `hello: world` | `hello: world` |
| `characters`: `"-.!"` | `hello` | `hello` |
| Chain: this filter `characters`: `"-.!"` then `LettersCase` `SentenceCase` | `a - b. c` | `A - B. C` |

Place **before** [LettersCase](LettersCase.md) (sentence mode) or [CasingList](CasingList.md) (sentence initials).
