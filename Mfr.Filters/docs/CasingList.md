# CasingList

Loads a **casing list** text file: **one word per line** (after trim). Comments and blank lines are ignored (lines starting with `//`, `\\`, or `# `). For each **word** in the target segment (split by the current [word separator](SpaceCharacter.md), default space), the filter looks up the word **case-insensitively**. If found, the word is replaced by the **exact spelling** from the file. Words not in the list are **unchanged**.

Optional **sentence-initial** uppercasing uses [SentenceEndCharacters](SentenceEndCharacters.md) and the word separator; place that filter **before** this one when you need custom sentence boundaries.

Examples match [`CasingListFilterTests`](../../Mfr.Tests/Models/Filters/Case/CasingListFilterTests.cs) (list file: `and`, `or`, `with`, `RMX` or `and`, `us`, `them` as noted).

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

| Options | Before | After |
|---------|--------|-------|
| List: `and`, `or`, `with`, `RMX`; `uppercaseSentenceInitial`: `false` | `03 - WiTH Or Without You Rmx` | `03 - with or Without You RMX` |
| Chain: [SentenceEndCharacters](SentenceEndCharacters.md) `characters`: `"-.!"` then list as above; `uppercaseSentenceInitial`: `true` | `03 - WiTH Or Without You Rmx` | `03 - With or Without You RMX` |
| Chain: [SpaceCharacter](SpaceCharacter.md) `spaceCharacter`: `"_"`, `replaceSpaces`: `true`; list: `and`, `us`, `them`; `uppercaseSentenceInitial`: `true` | `US_AND_THEM` | `Us_and_them` |

Put [SpaceCharacter](SpaceCharacter.md) first if words are separated by `_` or another character.
