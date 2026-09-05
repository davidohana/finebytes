# NameList

Replaces the target field with the **name-list line** matching the item's rename-list position (zero-based). Optional **prefix** and **suffix** format strings wrap that line and may include formatter tokens. An empty list is a no-op.

Line `k` applies to the item whose `RenameListIndex` is `k`. If the item index is outside the list, apply fails with a user-facing error.

## Options

| Property  | Type     | Description                                                                              |
| --------- | -------- | ---------------------------------------------------------------------------------------- |
| `entries` | string[] | Names in rename-list index order. Blank strings are empty names. Empty array is a no-op. |
| `prefix`  | string   | Format string prepended to the list entry (tokens allowed).                              |
| `suffix`  | string   | Format string appended after the list entry (tokens allowed).                            |

## Editor text format

The Filter Configuration pane edits `entries` as **one name per line**. Interior blank lines are empty names. A trailing newline after the last non-empty line does not add an extra entry. Comment-like text (`//`, `# `) is a name, not a comment. Each line is limited to 1000 characters.

**Example editor text**

```text
Alpha
Beta
Gamma
```

## Examples

- `entries`: `["Alpha", "Beta", "Gamma"]`
  - Before (index 1): `old1`
  - After: `Beta`
- `entries`: `["First", "", "Second"]`
  - Before (index 1): `b`
  - After: *(empty)*
  - Comment: Blank lines are empty names and still occupy an index.
- `entries`: `["One"]`; `prefix`:
  `"<counter:initial=10,step=1,padding=none,length=2,resetScope=global>_"`; `suffix`: `"_end"`
  - Before (index 0): `x`
  - After: `10_One_end`
- `entries`: `[]`
  - Before: `old`
  - After: `old`
  - Comment: Empty list is a no-op.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "NameList",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "entries": ["Alpha", "Beta", "Gamma"],
    "prefix": "",
    "suffix": ""
  }
}
```
