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

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `find`: `"a"`, `replacement`: `"X"`, `mode`: `Literal`, `caseSensitive`: `true`, `replaceAll`: `true` | `aba` | `XbX` | Every `a` replaced. |
| Same + `replaceAll`: `false` | `aba` | `Xba` | First `a` only. |
| `find`: `"f*o"`, `replacement`: `"X"`, `mode`: `Wildcard`, `replaceAll`: `true` | `foo` | `X` | `*` matches `oo`. |
| `find`: `"f?o"`, `replacement`: `"X"`, `mode`: `Wildcard`, `replaceAll`: `true` | `foo` or `fao` | `X` | `?` is one character. |
| `find`: `@"\d+"`, `replacement`: `"N"`, `mode`: `Regex`, `replaceAll`: `true` | `a12bc34` | `aNbcN` | Each digit run becomes `N`. |
| Same + `replaceAll`: `false` | `a12bc34` | `aNbc34` | First digit run only. |
| `find`: `"a"`, `replacement`: `"X"`, `mode`: `Literal`, `caseSensitive`: `false`, `replaceAll`: `true` | `AbA` | `XbX` | Case-insensitive `a`. |
| `find`: `"cat"`, `replacement`: `"dog"`, `mode`: `Literal`, `wholeWord`: `true`, `replaceAll`: `true` | `cat` | `dog` | Whole word `cat`. |
| Same | `category` | `category` | `cat` inside `category` is not a whole word. |
| Same | `a cat b` | `a dog b` | Middle word replaced. |
| `find`: `"CAT"`, `replacement`: `"dog"`, `caseSensitive`: `false`, `wholeWord`: `true` | `Category` | `Category` | `Cat` is not whole word `cat` (case-insensitive). |

For many rules from a file, use [ReplaceList](ReplaceList.md).
