# ReplaceList

Loads a **replace list file** and applies **search/replace pairs** in file order—like multiple [Replacer](Replacer.md) steps sharing the same mode and flags. Replacement lines may include formatter tokens (for example `<counter:…>`) where supported.

Examples match [`ReplaceListFilterTests`](../../../Mfr.Tests/Models/Filters/Replace/ReplaceListFilterTests.cs) (ordered pairs, strip with `<EMPTY>`, regex + counter on track-style titles).

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

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `filePath`: pairs `a`→`b`, then `.`→`_`<br>`mode`: `Literal`<br>`replaceAll`: `true` | `a.a` | `b_b` | Order matters: `a`→`b` first, then `.`→`_`. |
| `filePath`: `S:x` / `R:<EMPTY>`<br>`mode`: `Literal`<br>`replaceAll`: `true` | `abxcx` | `abc` | |
| `filePath`: `S:f*o` / `R:X`<br>`mode`: `Wildcard`<br>`replaceAll`: `true` | `foo` | `X` | |
| `filePath`: `a`→`b`, `.`→`_`, `[0-9]+`→`<counter:10,1,0,2,0>`<br>`mode`: `Regex`<br>`caseSensitive`: `false`<br>`replaceAll`: `true`<br>global index: `0` | `01.-.Blue.Train` | `10_-_Blue_Trbin` | Regex replaces digit runs; yields `Trbin` (see test). |
| (same file as row above)<br>global index: `1` | `02.-.A.Moment's.Notice` | `11_-_b_Moment's_Notice` | |

The list is loaded at filter **setup**; reload the preset or app after editing the file.
