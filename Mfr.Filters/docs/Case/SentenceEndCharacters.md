# SentenceEndCharacters

**Does not change the segment text.** It updates the rename item’s **sentence-end character set** for the rest of the filter chain. [LettersCase](LettersCase.md) in **sentence** mode and [CasingList](CasingList.md) with **uppercase sentence initial** read this set.

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `characters` | string | `".!?"` | Each character can end a sentence for sentence-style rules. Characters equal to the current [word separator](../Space/SpaceCharacter.md) are ignored when building the set. If empty, only the **start** of the segment gets a capital (for sentence case / sentence initials), not “after punctuation.” |

## Examples

This filter alone does not change segment text (see **Comment** on the first two rows). The last row shows the **effect on a following** [LettersCase](LettersCase.md) `SentenceCase` step (from [`SentenceEndCharactersFilterTests`](../../../Mfr.Tests/Models/Filters/Case/SentenceEndCharactersFilterTests.cs)).

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `characters`: `":;"` | `hello: world` | `hello: world` | This filter alone does not change text. |
| `characters`: `"-.!"` | `hello` | `hello` | Same: segment unchanged by this step. |
| Chain: this filter `characters`: `"-.!"` then `LettersCase` `SentenceCase` | `a - b. c` | `A - B. C` | Later `SentenceCase` uses `-`, `.`, `!` as sentence boundaries. |

Place **before** [LettersCase](LettersCase.md) (sentence mode) or [CasingList](CasingList.md) (sentence initials).
