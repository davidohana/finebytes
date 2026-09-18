# CasingList

Applies a **casing list** stored in the filter options as a **word array**. For each **word** in the target segment (split by the current [word separator](../Space/SpaceCharacter.md), default space), the filter looks up the word **case-insensitively**. If found, the word is replaced by the **exact spelling** from the list. Words not in the list are **unchanged**. An empty list skips word remapping (sentence-initial uppercasing still runs when enabled).

Optional **sentence-initial** uppercasing uses [SentenceEndCharacters](SentenceEndCharacters.md) and the word separator; place that filter **before** this one when you need custom sentence boundaries.

## Options

- **`words`** (`string[]`) — Words to apply by exact spelling. Duplicates: **last wins**.
  - Add-to-list and title-bar Reset still start with an **empty** list. The Filter Configuration
    **Load defaults** button replaces the Words box with the curated factory list (`DefaultWords`:
    title-case exceptions, multilingual particles, and common rename/media acronyms). That same
    list is used by the Beautify Names sample preset; it is **not** merged with existing text.
- **`uppercaseSentenceInitial`** (bool, default `false`)
  - If `true`, after list application, uppercases sentence starts with the same boundary rules as
    [LettersCase](LettersCase.md) **SentenceCase** (first letter, scanning past leading non-letters; after
    sentence-end characters when followed by the word separator). Does **not** lowercase the rest of the text.

## Editor text format

The Filter Configuration pane edits `words` as **space-separated** text (e.g. `and or with RMX`). Each word is limited to the configured maximum (default 2000 characters; same cap as name-list lines and replace-list search/replacement). Use **Load defaults** to fill the box from the factory list described above.

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
    "targetType": "FileName"
  },
  "options": {
    "words": ["and", "or", "with", "RMX"],
    "uppercaseSentenceInitial": false
  }
}
```
