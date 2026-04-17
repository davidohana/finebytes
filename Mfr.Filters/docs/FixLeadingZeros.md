# FixLeadingZeros

Finds **runs of digits** in the segment (`\d+` matching) and rewrites them so their **length** matches your target **width** by adjusting **leading zeros**. Other text is unchanged.

If `width` is `0` or negative, the filter returns the segment unchanged.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `FixLeadingZeros`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `width` | int | (required) | Desired minimum digit count after normalization. |
| `removeExtraZeros` | bool | (required) | If `true`, strip leading zeros from the match first, then pad to `width` (unless already long enough). |
| `maxCount` | int | `0` | Maximum number of digit groups to change; `0` means **all** matches. |
| `wholeWordOnly` | bool | `false` | If `true`, skip digit groups that touch a letter on either side (so `track01` might not change the `01` if it is adjacent to letters—depends on positions). |

## Examples

**Width 2, pad**

- Input: `9` → `09`; input: `09` → `09`.

**Remove extra zeros first**

- `removeExtraZeros`: `true`, `width`: `2`  
- Input: `0009` → trimmed then padded → `09` (per implementation rules).

**Limit replacements**

- `maxCount`: `1` — only the **first** digit sequence in the segment is normalized.

**Example preset fragment**

```json
{
  "type": "FixLeadingZeros",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "width": 2,
    "removeExtraZeros": true,
    "maxCount": 0,
    "wholeWordOnly": true
  }
}
```
