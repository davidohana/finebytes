# CasingList

Loads a **casing list** text file: **one word per line** (after trim). Comments and blank lines are ignored (lines starting with `//`, `\\`, or `# `). For each **word** in the target segment (split by the current [word separator](../Space/SpaceCharacter.md), default space), the filter looks up the word **case-insensitively**. If found, the word is replaced by the **exact spelling** from the file. Words not in the list are **unchanged**.

Optional **sentence-initial** uppercasing uses [SentenceEndCharacters](SentenceEndCharacters.md) and the word separator; place that filter **before** this one when you need custom sentence boundaries.

Example list files use words like `and`, `or`, `with`, `RMX` or `and`, `us`, `them` as noted in the rows below.

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `filePath` | string | (required) | Path to the casing-list file. |
| `uppercaseSentenceInitial` | bool | `false` | If `true`, after list application, uppercases the first letter at the start of the segment and after configured sentence ends. |

## List file format

- One word per line; spaces inside a line are invalid.
- Duplicate words (same letters, different casing): **last line wins** in the file.
- At least one non-comment word is required.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `filePath`: list (`and`, `or`, `with`, `RMX`)<br>`uppercaseSentenceInitial`: `false` | `03 - WiTH Or Without You Rmx` | `03 - with or Without You RMX` | |
| [SentenceEndCharacters](SentenceEndCharacters.md)<br>`characters`: `"-.!"`<br>`filePath`: same list as above<br>`uppercaseSentenceInitial`: `true` | `03 - WiTH Or Without You Rmx` | `03 - With or Without You RMX` | |
| [SpaceCharacter](../Space/SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>`replaceSpaces`: `true`<br>`filePath`: list (`and`, `us`, `them`)<br>`uppercaseSentenceInitial`: `true` | `US_AND_THEM` | `Us_and_them` | Underscore word boundaries + casing list + sentence initial. |

Put [SpaceCharacter](../Space/SpaceCharacter.md) first if words are separated by `_` or another character.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). Set `filePath` to your casing-list file.

```json
{
  "type": "CasingList",
  "target": {
    "family": "FileName",
    "fileNamePart": "Prefix"
  },
  "options": {
    "filePath": "C:/Music/MFR/casing-list.txt",
    "uppercaseSentenceInitial": false
  }
}
```
