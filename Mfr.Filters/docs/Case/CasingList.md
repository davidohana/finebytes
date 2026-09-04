# CasingList

Applies a **casing list** stored in the filter options as a **word array**. For each **word** in the target segment (split by the current [word separator](../Space/SpaceCharacter.md), default space), the filter looks up the word **case-insensitively**. If found, the word is replaced by the **exact spelling** from the list. Words not in the list are **unchanged**. An empty list is a no-op.

Optional **sentence-initial** uppercasing uses [SentenceEndCharacters](SentenceEndCharacters.md) and the word separator; place that filter **before** this one when you need custom sentence boundaries.

## Options

- **`words`** (`string[]`) — Words to apply by exact spelling. Duplicates: **last wins**.
- **`uppercaseSentenceInitial`** (bool, default `false`)
  - If `true`, after list application, uppercases the first letter at the start of the segment and after configured
    sentence ends.

## Editor text format

The Filter Configuration pane edits `words` as one-word-per-line text. Blank lines and `//`, `\\`, or `# ` comments are ignored when converting that text into the stored array. Each word is limited to 1000 characters (same cap as name-list / replace-list files). Spaces inside a word are invalid.

## Examples

- `words`: `["and", "or", "with", "RMX"]`; `uppercaseSentenceInitial`: `false`
  - Before: `03 - WiTH Or Without You Rmx`
  - After: `03 - with or Without You RMX`
- [SentenceEndCharacters](SentenceEndCharacters.md); `characters`: `"-.!"`; same `words` as above;
  `uppercaseSentenceInitial`: `true`
  - Before: `03 - WiTH Or Without You Rmx`
  - After: `03 - With or Without You RMX`
- [SpaceCharacter](../Space/SpaceCharacter.md); `spaceCharacter`: `"_"`; `replacements`: `[" "]`;
  `words`: `["and", "us", "them"]`; `uppercaseSentenceInitial`: `true`
  - Before: `US_AND_THEM`
  - After: `Us_and_them`
  - Comment: Underscore word boundaries + casing list + sentence initial.

Put [SpaceCharacter](../Space/SpaceCharacter.md) first if words are separated by `_` or another character.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "CasingList",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "words": ["and", "or", "with", "RMX"],
    "uppercaseSentenceInitial": false
  }
}
```
