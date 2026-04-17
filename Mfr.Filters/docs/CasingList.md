# CasingList

Loads a **casing list** text file: **one word per line** (after trim). Comments and blank lines are ignored (lines starting with `//`, `\\`, or `# `). For each **word** in the target segment (split by the current [word separator](SpaceCharacter.md), default space), the filter looks up the word **case-insensitively**. If found, the word is replaced by the **exact spelling** from the file. Words not in the list are **unchanged**.

Optional **sentence-initial** uppercasing uses [SentenceEndCharacters](SentenceEndCharacters.md) and the word separator; place that filter **before** this one when you need custom sentence boundaries.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `CasingList`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `filePath` | string | (required) | Absolute or relative path to the casing-list file. |
| `uppercaseSentenceInitial` | bool | `false` | If `true`, after list application, uppercases the first letter at the start of the segment and after configured sentence ends. |

## List file format

- One word per line; spaces inside a line are invalid (must be exactly one word).
- Duplicate words (same letters, different casing): **last line wins** in the file.
- At least one non-comment word is required.

## Examples

**List file**

```text
and
or
RMX
```

**Effect**

- Input: `WiTH Or Rmx` → `with or RMX` (tokens are matched case-insensitively to the list; `Or` matches the line `or`; unknown tokens stay unchanged).

**With sentence initials**

- Add [SentenceEndCharacters](SentenceEndCharacters.md) with `-.!` before this filter, `uppercaseSentenceInitial`: `true`, for behavior like capitalizing after `-` / `.` / `!`.

**Example preset fragment**

```json
{
  "type": "CasingList",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "filePath": "C:\\MFR\\MFRCaser.txt",
    "uppercaseSentenceInitial": true
  }
}
```

**Tips**

- Put [SpaceCharacter](SpaceCharacter.md) first if “words” are separated by `_` or another character.
- Put [SentenceEndCharacters](SentenceEndCharacters.md) before this filter when using `uppercaseSentenceInitial` with custom punctuation.
