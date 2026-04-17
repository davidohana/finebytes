# SentenceEndCharacters

**Does not change the text.** It updates the rename item’s **sentence-end character set** for the rest of the filter chain. [LettersCase](LettersCase.md) in **sentence** mode and [CasingList](CasingList.md) with **uppercase sentence initial** read this set.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `SentenceEndCharacters`. |
| `enabled` | bool | When `false`, the filter does nothing (sentence ends stay at their previous value for this item). |
| `target` | object | Still required for serialization; text is unchanged regardless of target. |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `characters` | string | `".!?"` | Each character in this string can end a sentence for sentence-style rules. Characters equal to the current [word separator](SpaceCharacter.md) are ignored when building the set. If the string is empty, only the **start** of the segment gets a capital (for sentence case / sentence initials), not “after punctuation.” |

## Examples

**Default**

- If you omit this filter, behavior uses `.!?` as sentence ends (until another filter sets them).

**Custom ends**

- `characters`: `"-.!"` — treat `-`, `.`, and `!` as sentence boundaries (for the next filter that cares).

**No mid-sentence capitals from punctuation**

- `characters`: `""` — sentence-style rules only affect the first letter of the segment.

**Example preset fragment**

```json
{
  "type": "SentenceEndCharacters",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "characters": "-.!"
  }
}
```

Place this filter **before** [LettersCase](LettersCase.md) (sentence mode) or [CasingList](CasingList.md) (with sentence initials).
