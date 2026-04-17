# SentenceEndCharacters

**Does not change the text.** It updates the rename item’s **sentence-end character set** for the rest of the filter chain. [LettersCase](LettersCase.md) in **sentence** mode and [CasingList](CasingList.md) with **uppercase sentence initial** read this set.

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `characters` | string | `".!?"` | Each character can end a sentence for sentence-style rules. Characters equal to the current [word separator](SpaceCharacter.md) are ignored when building the set. If empty, only the **start** of the segment gets a capital (for sentence case / sentence initials), not “after punctuation.” |

## Examples

- Omit this filter → `.!?` used until another filter sets sentence ends.
- `characters`: `"-.!"` — treat `-`, `.`, and `!` as boundaries for the next filter.
- `characters`: `""` — only the first letter of the segment is capitalized by sentence-style rules.

Place this filter **before** [LettersCase](LettersCase.md) (sentence mode) or [CasingList](CasingList.md) (with sentence initials).

```json
{ "characters": "-.!" }
```
