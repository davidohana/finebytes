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

- `find`: `" "`, `replacement`: `"_"`, `mode`: `Literal`, `replaceAll`: `true` — `my song` → `my_song`
- `mode`: `Wildcard`, `find`: `"f*o"` — matches `foo`, `fxxo`, etc., per rules.

For many rules from a file, use [ReplaceList](ReplaceList.md).

```json
{
  "find": " ",
  "replacement": "_",
  "mode": "Literal",
  "caseSensitive": true,
  "replaceAll": true,
  "wholeWord": false
}
```
