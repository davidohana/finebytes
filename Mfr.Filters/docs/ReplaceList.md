# ReplaceList

Loads a **replace list file** and applies **search/replace pairs** in file order—like multiple [Replacer](Replacer.md) steps sharing the same mode and flags. Replacement lines may include formatter tokens (for example `<counter:…>`) where supported.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `filePath` | string | Path to the replace-list file. |
| `mode` | string (enum) | `Literal`, `Wildcard`, or `Regex` — applies to **every** pair in the file. |
| `caseSensitive` | bool | Matching flag for all pairs. |
| `replaceAll` | bool | Replace all matches per pair. |
| `wholeWord` | bool | Whole-word restriction for all pairs. |

## Replace-list file format

- Each entry is two lines: `S:` + search, then `R:` + replacement.
- Comment lines: `//`, `\\`, or `# ` (hash + space) after optional whitespace.
- Empty lines ignored.
- Search and replacement (after the prefix) must be non-empty; use `<EMPTY>` on the `R:` line to remove the match.
- Each `S:`/`R:` line at most 1000 characters.
- At least one pair required.

**Example file**

```text
S:a
R:b

S:\.
R:_
```

## Examples

| Options | Before | After |
|---------|--------|-------|
| `mode`: `Literal`, `replaceAll`: `true`, file with pairs above (order: `a`→`b`, then `.`→`_`) | `a.a` | `b_b` |
| File: `S:x` / `R:<EMPTY>`, `mode`: `Literal`, `replaceAll`: `true` | `abxcx` | `abc` |

The list is loaded at filter **setup**; reload the preset or app after editing the file.
