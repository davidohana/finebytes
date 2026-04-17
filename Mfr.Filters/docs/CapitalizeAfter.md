# CapitalizeAfter

Uppercases the **single character immediately after** any character that appears in your configured list. Other characters are unchanged. If the list is empty, the segment is unchanged.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `CapitalizeAfter`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | Typically `{ "family": "FileName", "fileNamePart": "Prefix" }` (see [LettersCase](LettersCase.md)). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `capitalizeAfterChars` | string | `",!()[]{};-"` | Every character in this string is a trigger: the **next** character in the segment is uppercased (default is comma, `!`, parentheses, brackets, `;`, `-`). |

## Examples

**Default trigger set**

- After `(`: `hello (world)` → `hello (World)`

**Custom triggers (hyphen only)**

- `capitalizeAfterChars`: `"-"`  
- Input: `hello-world` → `hello-World` (only the letter after the first `-` is forced up; behavior is per trigger position in one pass).

**Empty list**

- `capitalizeAfterChars`: `""` → input unchanged.

**Example preset fragment**

```json
{
  "type": "CapitalizeAfter",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "capitalizeAfterChars": "(-["
  }
}
```
