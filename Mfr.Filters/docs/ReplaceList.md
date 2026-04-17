# ReplaceList

Loads a **replace list file** and applies **search/replace pairs** in file order—like multiple [Replacer](Replacer.md) steps sharing the same mode and flags. Replacement lines may include formatter tokens (for example `<counter:…>`) where supported.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `ReplaceList`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `filePath` | string | Path to the replace-list file. |
| `mode` | string (enum) | `Literal`, `Wildcard`, or `Regex` — applies to **every** pair in the file. |
| `caseSensitive` | bool | Matching flag for all pairs. |
| `replaceAll` | bool | Replace all matches per pair. |
| `wholeWord` | bool | Whole-word restriction for all pairs. |

### Replace-list file format

- Non-empty lines that are not comments: each entry is **two** lines: `S:` + search, then `R:` + replacement.
- Comment lines: start with `//`, `\\`, or `# ` (hash + space) after optional whitespace.
- Empty lines are ignored.
- Search and replacement (after the prefix) must be non-empty; use `<EMPTY>` on the `R:` line to remove the match.
- Each `S:`/`R:` line max length 1000 characters.
- At least one pair is required.

**Example file**

```text
S:a
R:b

S:\.
R:_
```

## Examples

**Sequential application**

- First pair maps `a` → `b`, second maps `.` → `_` on the **current** string after the first step.

**Strip matches**

- `R:<EMPTY>` removes the search text.

**Example preset fragment**

```json
{
  "type": "ReplaceList",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "filePath": "C:\\MFR\\my-replacements.txt",
    "mode": "Literal",
    "caseSensitive": true,
    "replaceAll": true,
    "wholeWord": false
  }
}
```

The list is **read once** at filter setup; edit the file and reload the preset (or restart) to pick up changes, depending on your app.
