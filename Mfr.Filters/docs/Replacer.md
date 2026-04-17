# Replacer

Finds a **search** pattern in the target segment and replaces matches with **replacement** text. Mode controls how `find` is interpreted.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `Replacer`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `find` | string | Search pattern (meaning depends on `mode`). |
| `replacement` | string | Replacement text (can include formatter-style tokens where the pipeline supports them in replacements). |
| `mode` | string (enum) | `Literal`, `Wildcard`, or `Regex`. |
| `caseSensitive` | bool | Match case when searching. |
| `replaceAll` | bool | If `true`, replace every match; if `false`, only the **first** match is replaced. |
| `wholeWord` | bool | Restrict matches to whole “words” (word boundaries). |

### Modes (`mode`)

| Value | `find` meaning |
|--------|----------------|
| `Literal` | Exact substring. |
| `Wildcard` | `*` = any run of characters, `?` = one character (translated to regex internally). |
| `Regex` | .NET regular expression. |

## Examples

**Literal: spaces to underscores**

- `find`: `" "`, `replacement`: `"_"`, `mode`: `Literal`, `caseSensitive`: `true`, `replaceAll`: `true`  
- `my song` → `my_song`

**Wildcard**

- `find`: `"f*o"`, `mode`: `Wildcard` — matches `foo`, `fxxo`, etc., per wildcard rules.

**Example preset fragment**

```json
{
  "type": "Replacer",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "find": " ",
    "replacement": "_",
    "mode": "Literal",
    "caseSensitive": true,
    "replaceAll": true,
    "wholeWord": false
  }
}
```

For many rules from a file, use [ReplaceList](ReplaceList.md).
