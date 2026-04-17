# SentenceEndCharacters

**Does not change the segment text.** It updates the rename item’s **sentence-end character set** for the rest of the filter chain. [LettersCase](LettersCase.md) in **sentence** mode and [CasingList](CasingList.md) with **uppercase sentence initial** read this set.

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `characters` | string | `".!?"` | Each character can end a sentence for sentence-style rules. Characters equal to the current [word separator](../Space/SpaceCharacter.md) are ignored when building the set. If empty, only the **start** of the segment gets a capital (for sentence case / sentence initials), not “after punctuation.” |

## Examples

The first two rows are no-ops on the segment (see **Comment**). The last row chains into [LettersCase](LettersCase.md) `SentenceCase`.

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `characters`: `":;"` | `hello: world` | `hello: world` | No-op on text; only updates rename state. |
| `characters`: `"-.!"` | `hello` | `hello` | Same. |
| [SentenceEndCharacters](SentenceEndCharacters.md)<br>`characters`: `"-.!"`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `a - b. c` | `A - B. C` | |

Place **before** [LettersCase](LettersCase.md) (sentence mode) or [CasingList](CasingList.md) (sentence initials).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "SentenceEndCharacters",
  "target": {
    "family": "FileName",
    "fileNamePart": "Prefix"
  },
  "options": {
    "characters": ".!?"
  }
}
```
