# SpaceCharacter

Sets the **word separator** character for the rest of the rename pass and optionally **replaces** common stand-ins (normal spaces, underscores, `%20`, custom text) with that character. Later filters ([ShrinkSpaces](ShrinkSpaces.md), [RemoveSpaces](RemoveSpaces.md), [SeparateCapitalizedText](SeparateCapitalizedText.md), strip-space filters, and case/casing-list word splitting) use `WordSeparator` (default is ordinary space until this filter runs).

## Options

- **`spaceCharacter`** (string or char)
  - **Single** character that becomes the word separator (first character used if a longer string is sent).
- **`replacements`** (array of strings)
  - Each listed substring is replaced with `spaceCharacter`, in array order.
  - Built-in choices map to `"%20"`, `" "` (U+0020 SPACE), and `"_"`.
  - Any other string is a custom replacement (for example `"++"`).

## Examples

- `spaceCharacter`: `"_"`; `replacements`: `["%20"]`
  - Before: `Gone%20With%20The%20Wind`
  - After: `Gone_With_The_Wind`
- `spaceCharacter`: space; `replacements`: `["%20", " ", "_"]`
  - Before: `a_b c%20d`
  - After: `a b c d`
- `spaceCharacter`: `"-"`; `replacements`: `["++"]` — `a++b` → `a-b`
- `spaceCharacter`: `"_"`; `replacements`: `["%20"]`; [LettersCase](../Case/LettersCase.md); `mode`:
  `Capitalize`; `capitalizeSkipWords`: `["the"]`
  - Before: `gone%20with%20the%20wind`
  - After: `Gone_With_the_Wind`
- `spaceCharacter`: `"_"`; `replacements`: `["%20"]`
  - Before: `my song`
  - After: `my song`
  - Comment: Text unchanged; `WordSeparator` still set to `_` for later filters.

Put this filter **before** any filter that should use a non-space word boundary.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "SpaceCharacter",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "spaceCharacter": "_",
    "replacements": ["%20"]
  }
}
```
