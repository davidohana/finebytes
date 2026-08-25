# ReplaceList

Loads a **replace list file** and applies **search/replace pairs** in file order—like multiple [Replacer](Replacer.md) steps sharing the same mode and flags. Replacement lines may include formatter tokens (for example `<counter:…>`) where supported.

## Options

| Property        | Type          | Description                                                                |
| --------------- | ------------- | -------------------------------------------------------------------------- |
| `filePath`      | string        | Path to the replace-list file.                                             |
| `mode`          | string (enum) | `Literal`, `Wildcard`, or `Regex` — applies to **every** pair in the file. |
| `caseSensitive` | bool          | Matching flag for all pairs.                                               |
| `replaceAll`    | bool          | Replace all matches per pair.                                              |
| `wholeWord`     | bool          | Whole-word restriction for all pairs.                                      |

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

- `filePath`: pairs `a`→`b`, then `.`→`_`; `mode`: `Literal`; `replaceAll`: `true`
  - Before: `a.a`
  - After: `b_b`
  - Comment: Order matters: `a`→`b` first, then `.`→`_`.
- `filePath`: `S:x` / `R:<EMPTY>`; `mode`: `Literal`; `replaceAll`: `true` — `abxcx` → `abc`
- `filePath`: `S:f*o` / `R:X`; `mode`: `Wildcard`; `replaceAll`: `true` — `foo` → `X`
- `filePath`: `a`→`b`, `.`→`_`,
  `[0-9]+`→`<counter:initial=10,step=1,padding=none,length=2,resetScope=global>`; `mode`: `Regex`; `caseSensitive`:
  `false`; `replaceAll`: `true`; global index: `0`
  - Before: `01.-.Blue.Train`
  - After: `10_-_Blue_Trbin`
  - Comment: Regex replaces digit runs; yields `Trbin`.
- (same file as row above); global index: `1` — `02.-.A.Moment's.Notice` → `11_-_b_Moment's_Notice`

The list is loaded at filter **setup**; reload the preset or app after editing the file.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). Set `filePath` to your replace-list file.

```json
{
  "type": "ReplaceList",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "filePath": "C:/Music/MFR/replace-list.txt",
    "mode": "Literal",
    "caseSensitive": true,
    "replaceAll": true,
    "wholeWord": false
  }
}
```
