# SentenceEndCharacters

**Does not change the segment text.** It updates the rename item’s **sentence-end character set** for the rest of the filter chain. [LettersCase](LettersCase.md) in **sentence** mode and [CasingList](CasingList.md) with **uppercase sentence initial** read this set.

## Options

- **`characters`** (string, default `".!?"` on the options record / omitted JSON key)
  - Each character can end a sentence for sentence-style rules. Characters equal to the current [word
    separator](../Space/SpaceCharacter.md) are ignored when building the set. If empty, only the **start** of the
    segment gets a capital (for sentence case / sentence initials), not “after punctuation.”
  - **Add-to-list** (parameterless filter) uses MFR7’s `"-.!"`. Until this filter runs, rename items use
    `".!?"` (same as the options-record default).

## Examples

The first two rows are no-ops on the segment (see **Comment**). The last row chains into [LettersCase](LettersCase.md) `SentenceCase`.

- `characters`: `":;"` — `hello: world` → `hello: world` — No-op on text; only updates rename state.
- `characters`: `"-.!"` — `hello` → `hello` — Same.
- [SentenceEndCharacters](SentenceEndCharacters.md); `characters`: `"-.!"`; [LettersCase](LettersCase.md);
  `mode`: `SentenceCase`
  - Before: `a - b. c`
  - After: `A - B. C`

Place **before** [LettersCase](LettersCase.md) (sentence mode) or [CasingList](CasingList.md) (sentence initials).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "SentenceEndCharacters",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "characters": ".!?"
  }
}
```
