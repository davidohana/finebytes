# CasingList

Loads a **casing list** text file: **one word per line** (after trim). Comments and blank lines are ignored (lines starting with `//`, `\\`, or `# `). For each **word** in the target segment (split by the current [word separator](SpaceCharacter.md), default space), the filter looks up the word **case-insensitively**. If found, the word is replaced by the **exact spelling** from the file. Words not in the list are **unchanged**.

Optional **sentence-initial** uppercasing uses [SentenceEndCharacters](SentenceEndCharacters.md) and the word separator; place that filter **before** this one when you need custom sentence boundaries.

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

Assume list file lines: `and`, `or`, `RMX` (canonical casing).

| Options | Before | After |
|---------|--------|-------|
| `filePath` → list as above, `uppercaseSentenceInitial`: `false` | `WiTH Or Rmx` | `with or RMX` |
| `uppercaseSentenceInitial`: `true`, chain: [SentenceEndCharacters](SentenceEndCharacters.md) `characters`: `"-.!"` then this filter (same list) | `03 - WiTH Or Without You Rmx` | `03 - With or Without You RMX` |

Put [SpaceCharacter](SpaceCharacter.md) first if words are split by `_` instead of spaces.
