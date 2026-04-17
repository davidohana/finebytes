# Replacer

Finds a **search** pattern in the target segment and replaces matches with **replacement** text. `mode` controls how `find` is interpreted.

Examples match [`ReplacerFilterTests`](../../../Mfr.Tests/Models/Filters/Replace/ReplacerFilterTests.cs).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `find` | string | Search pattern (meaning depends on `mode`). |
| `replacement` | string | Replacement text. |
| `mode` | string (enum) | `Literal`, `Wildcard`, or `Regex` — see **Modes**. |
| `caseSensitive` | bool | Match case when searching. |
| `replaceAll` | bool | If `true`, replace every match; if `false`, only the **first** match. |
| `wholeWord` | bool | Restrict matches to whole words (word boundaries). |

### Modes (`mode`)

| Value | `find` meaning |
|--------|----------------|
| `Literal` | Exact substring. |
| `Wildcard` | `*` = any run of characters, `?` = one character. |
| `Regex` | .NET regular expression. |

## Examples

| Options | Before | After |
|---------|--------|-------|
| `find`: `"a"`, `replacement`: `"X"`, `mode`: `Literal`, `caseSensitive`: `true`, `replaceAll`: `true` | `aba` | `XbX` |
| Same + `replaceAll`: `false` | `aba` | `Xba` |
| `find`: `"f*o"`, `replacement`: `"X"`, `mode`: `Wildcard`, `replaceAll`: `true` | `foo` | `X` |
| `find`: `"f?o"`, `replacement`: `"X"`, `mode`: `Wildcard`, `replaceAll`: `true` | `foo` or `fao` | `X` |
| `find`: `@"\d+"`, `replacement`: `"N"`, `mode`: `Regex`, `replaceAll`: `true` | `a12bc34` | `aNbcN` |
| Same + `replaceAll`: `false` | `a12bc34` | `aNbc34` |
| `find`: `"a"`, `replacement`: `"X"`, `mode`: `Literal`, `caseSensitive`: `false`, `replaceAll`: `true` | `AbA` | `XbX` |
| `find`: `"cat"`, `replacement`: `"dog"`, `mode`: `Literal`, `wholeWord`: `true`, `replaceAll`: `true` | `cat` | `dog` |
| Same | `category` | `category` (substring not replaced) |
| Same | `a cat b` | `a dog b` |
| `find`: `"CAT"`, `replacement`: `"dog"`, `caseSensitive`: `false`, `wholeWord`: `true` | `Category` | `Category` |

For many rules from a file, use [ReplaceList](ReplaceList.md).
