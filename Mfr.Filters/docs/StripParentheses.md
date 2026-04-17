# StripParentheses

Removes **one kind** of bracket pair: round `()`, square `[]`, curly `{}`, or angle `<>`. Either delete **only the delimiters** or **the whole bracketed region** (delimiters + inside), depending on options.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `StripParentheses`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `type` | string (enum) | Bracket style to strip: `Round` `()`, `Square` `[]`, `Curly` `{}`, or `Angle` `<>`. (This is `options.type` in JSON, not the filter’s top-level `type`.) |
| `removeContents` | bool | If `true`, remove opening + closing + everything between. If `false`, remove only the opening and closing characters (content stays). |

## Examples

**Round brackets, remove contents**

- `type`: `Round`, `removeContents`: `true`  
- Input: `Song (live)` → `Song ` (trailing space may remain where the brackets were).

**Round brackets, delimiters only**

- `removeContents`: `false`  
- Input: `Song (live)` → `Song live`

**Square brackets**

- `type`: `Square` — targets `[`…`]` regions.

**Example preset fragment**

```json
{
  "type": "StripParentheses",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "type": "Round",
    "removeContents": true
  }
}
```
