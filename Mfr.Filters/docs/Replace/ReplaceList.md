# ReplaceList

Applies **search/replace pairs** embedded in the filter options, in list order—like multiple [Replacer](Replacer.md) steps sharing the same mode and flags. Replacement values may include formatter tokens (for example `<counter:…>`) where supported. An empty list is a no-op.

## Options

| Property        | Type          | Description                                                    |
| --------------- | ------------- | -------------------------------------------------------------- |
| `entries`       | object[]      | `{ "search", "replacement" }` pairs applied in order.          |
| `mode`          | string (enum) | `Literal`, `Wildcard`, or `Regex` — applies to **every** pair. |
| `caseSensitive` | bool          | Matching flag for all pairs.                                   |
| `replaceAll`    | bool          | Replace all matches per pair.                                  |
| `wholeWord`     | bool          | Whole-word restriction for all pairs.                          |

## Editor text format

The Filter Configuration pane edits `entries` as **line-separated** pairs. Each non-empty line is either a lone `search` (empty replacement / strip) or **whitespace-separated** `search` then `replacement`. Lines with more than two tokens are ignored while typing. Search and replacement must not contain whitespace; each is limited to 1000 characters.

**Example editor text**

```text
a b
. _
x
```

## Examples

- `entries`: `a`→`b`, then `.`→`_`; `mode`: `Literal`; `replaceAll`: `true`
  - Before: `a.a`
  - After: `b_b`
  - Comment: Order matters: `a`→`b` first, then `.`→`_`.
- `entries`: `x`→`""`; `mode`: `Literal`; `replaceAll`: `true` — `abxcx` → `abc`
- `entries`: `f*o`→`X`; `mode`: `Wildcard`; `replaceAll`: `true` — `foo` → `X`
- `entries`: `a`→`b`, `\.`→`_`,
  `[0-9]+`→`<counter:initial=10,step=1,padding=none,length=2,resetScope=global>`; `mode`: `Regex`; `caseSensitive`:
  `false`; `replaceAll`: `true`; global index: `0`
  - Before: `01.-.Blue.Train`
  - After: `10_-_Blue_Trbin`
  - Comment: Regex replaces digit runs; yields `Trbin`.
- (same entries as row above); global index: `1` — `02.-.A.Moment's.Notice` → `11_-_b_Moment's_Notice`

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "ReplaceList",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "entries": [
      { "search": "a", "replacement": "b" },
      { "search": ".", "replacement": "_" }
    ],
    "mode": "Literal",
    "caseSensitive": true,
    "replaceAll": true,
    "wholeWord": true
  }
}
```
