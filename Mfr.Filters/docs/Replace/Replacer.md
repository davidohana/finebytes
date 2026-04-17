# Replacer

Finds a **search** pattern in the target segment and replaces matches with **replacement** text. `mode` controls how `find` is interpreted.

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
| `find`: `"a"`<br>`replacement`: `"X"`<br>`mode`: `Literal`<br>`caseSensitive`: `true`<br>`replaceAll`: `true` | `aba` | `XbX` | |
| `find`: `"a"`<br>`replacement`: `"X"`<br>`mode`: `Literal`<br>`caseSensitive`: `true`<br>`replaceAll`: `false` | `aba` | `Xba` | First match only. |
| `find`: `"f*o"`<br>`replacement`: `"X"`<br>`mode`: `Wildcard`<br>`replaceAll`: `true` | `foo` | `X` | |
| `find`: `"f?o"`<br>`replacement`: `"X"`<br>`mode`: `Wildcard`<br>`replaceAll`: `true` | `foo` or `fao` | `X` | |
| `find`: `@"\d+"`<br>`replacement`: `"N"`<br>`mode`: `Regex`<br>`replaceAll`: `true` | `a12bc34` | `aNbcN` | |
| `find`: `@"\d+"`<br>`replacement`: `"N"`<br>`mode`: `Regex`<br>`replaceAll`: `false` | `a12bc34` | `aNbc34` | First digit run only. |
| `find`: `"a"`<br>`replacement`: `"X"`<br>`mode`: `Literal`<br>`caseSensitive`: `false`<br>`replaceAll`: `true` | `AbA` | `XbX` | |
| `find`: `"cat"`<br>`replacement`: `"dog"`<br>`mode`: `Literal`<br>`wholeWord`: `true`<br>`replaceAll`: `true` | `cat` | `dog` | |
| (same as row above) | `category` | `category` | `cat` is a substring of `category`, not a whole word. |
| (same as row above) | `a cat b` | `a dog b` | |
| `find`: `"CAT"`<br>`replacement`: `"dog"`<br>`mode`: `Literal`<br>`caseSensitive`: `false`<br>`wholeWord`: `true`<br>`replaceAll`: `true` | `Category` | `Category` | No standalone word `cat` in `Category` (substring doesn’t count). |

For many rules from a file, use [ReplaceList](ReplaceList.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "Replacer",
  "target": {
    "family": "FileName",
    "fileNamePart": "Prefix"
  },
  "options": {
    "find": "a",
    "replacement": "X",
    "mode": "Literal",
    "caseSensitive": true,
    "replaceAll": true,
    "wholeWord": false
  }
}
```
