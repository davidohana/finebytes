# SpaceCharacter

Sets the **word separator** character for the rest of the rename pass and optionally **replaces** common stand-ins (normal spaces, underscores, `%20`, custom text) with that character. Later filters ([ShrinkSpaces](ShrinkSpaces.md), [RemoveSpaces](RemoveSpaces.md), strip-space filters, and case/casing-list word splitting) use `WordSeparator` (default is ordinary space until this filter runs).

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `SpaceCharacter`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

| Property | Type | Description |
|----------|------|-------------|
| `spaceCharacter` | string or char | **Single** character that becomes the word separator (first character used if a longer string is sent). |
| `replaceSpaces` | bool | Replace U+0020 SPACE with `spaceCharacter`. |
| `replaceUnderscores` | bool | Replace `_` with `spaceCharacter`. |
| `replacePercent20` | bool | Replace the literal text `%20` with `spaceCharacter`. |
| `customText` | string | If non-empty, every occurrence of this substring is replaced with `spaceCharacter`. |

Replacements are applied in a fixed order: `%20`, then space, then underscore, then custom text (each pass uses the current string).

## Examples

**Underscores as words**

- `spaceCharacter`: `"_"`, `replaceSpaces`: `true`  
- Input: `my song` → `my_song`

**Only set separator (no substitution)**

- Turn off all replacements: segment may stay unchanged but `WordSeparator` is still set to `spaceCharacter` for later filters.

**Example preset fragment**

```json
{
  "type": "SpaceCharacter",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "spaceCharacter": "_",
    "replaceSpaces": true,
    "replaceUnderscores": false,
    "replacePercent20": false,
    "customText": ""
  }
}
```

Put this filter **before** any filter that should respect a non-space word boundary.
