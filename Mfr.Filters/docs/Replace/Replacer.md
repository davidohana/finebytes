# Replacer

Finds a **search** pattern in the target segment and replaces matches with **replacement** text. `mode` controls how `find` is interpreted.

## Options

| Property      | Type   | Description                                                                                                       |
| ------------- | ------ | ----------------------------------------------------------------------------------------------------------------- |
| `find`        | string | Search pattern (meaning depends on `match.mode`). Empty → no-op.                                                  |
| `replacement` | string | Replacement text. In `Regex` mode, `$0` / `$1`… are substitutions; in `Literal` / `Wildcard` they are plain text. |
| `match`       | object | Shared match policy (`mode`, `caseSensitive`, `replaceAll`, `wholeWord`) — see **Match**.                         |

### Match (`match`)

| Property        | Type          | Description                                                           |
| --------------- | ------------- | --------------------------------------------------------------------- |
| `mode`          | string (enum) | `Literal`, `Wildcard`, or `Regex` — see **Modes**.                    |
| `caseSensitive` | bool          | Match case when searching.                                            |
| `replaceAll`    | bool          | If `true`, replace every match; if `false`, only the **first** match. |
| `wholeWord`     | bool          | Restrict matches to whole words (word boundaries). Default: `false`.  |

### Modes (`match.mode`)

| Value      | `find` meaning                                    |
| ---------- | ------------------------------------------------- |
| `Literal`  | Exact substring.                                  |
| `Wildcard` | `*` = any run of characters, `?` = one character. |
| `Regex`    | .NET regular expression. Invalid patterns fail at setup. |

## Examples

- `find`: `"a"`; `replacement`: `"X"`; `mode`: `Literal`; `caseSensitive`: `true`; `replaceAll`: `true` — `aba` → `XbX`
- `find`: `"a"`; `replacement`: `"X"`; `mode`: `Literal`; `caseSensitive`: `true`; `replaceAll`: `false`
  - Before: `aba`
  - After: `Xba`
  - Comment: First match only.
- `find`: `"f*o"`; `replacement`: `"X"`; `mode`: `Wildcard`; `replaceAll`: `true` — `foo` → `X`
- `find`: `"f?o"`; `replacement`: `"X"`; `mode`: `Wildcard`; `replaceAll`: `true`: `foo` or `fao` → `X`
- `find`: `@"\d+"`; `replacement`: `"N"`; `mode`: `Regex`; `replaceAll`: `true` — `a12bc34` → `aNbcN`
- `find`: `@"\d+"`; `replacement`: `"N"`; `mode`: `Regex`; `replaceAll`: `false`
  - Before: `a12bc34`
  - After: `aNbc34`
  - Comment: First digit run only.
- `find`: `"a"`; `replacement`: `"X"`; `mode`: `Literal`; `caseSensitive`: `false`; `replaceAll`: `true` — `AbA` → `XbX`
- `find`: `"cat"`; `replacement`: `"dog"`; `mode`: `Literal`; `wholeWord`: `true`; `replaceAll`: `true` — `cat` → `dog`
- (same as row above): `category` → `category` — `cat` is a substring of `category`, not a whole word.
- (same as row above): `a cat b` → `a dog b`
- `find`: `"CAT"`; `replacement`: `"dog"`; `mode`: `Literal`; `caseSensitive`: `false`; `wholeWord`:
  `true`; `replaceAll`: `true`
  - Before: `Category`
  - After: `Category`
  - Comment: No standalone word `cat` in `Category` (substring doesn’t count).
- `find`: `""`; `replacement`: `"X"`; `mode`: `Literal` — input unchanged (empty find is a no-op).
- `find`: `"a"`; `replacement`: `"$1"`; `mode`: `Literal` — `a` → `$1` (`$` is literal outside Regex mode).
- `find`: `@"(a)(b)"`; `replacement`: `"$2$1"`; `mode`: `Regex` — `ab` → `ba`

For many rules in one step, use [ReplaceList](ReplaceList.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "Replacer",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "find": "a",
    "replacement": "X",
    "match": {
      "mode": "Literal",
      "caseSensitive": true,
      "replaceAll": true,
      "wholeWord": false
    }
  }
}
```
