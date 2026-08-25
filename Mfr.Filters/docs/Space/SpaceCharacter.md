# SpaceCharacter

Sets the **word separator** character for the rest of the rename pass and optionally **replaces** common stand-ins (normal spaces, underscores, `%20`, custom text) with that character. Later filters ([ShrinkSpaces](ShrinkSpaces.md), [RemoveSpaces](RemoveSpaces.md), [SeparateCapitalizedText](SeparateCapitalizedText.md), strip-space filters, and case/casing-list word splitting) use `WordSeparator` (default is ordinary space until this filter runs).

## Options

- **`spaceCharacter`** (string or char)
  - **Single** character that becomes the word separator (first character used if a longer string is sent).
- **`replaceSpaces`** (bool) — Replace U+0020 SPACE with `spaceCharacter`.
- **`replaceUnderscores`** (bool) — Replace `_` with `spaceCharacter`.
- **`replacePercent20`** (bool) — Replace the literal text `%20` with `spaceCharacter`.
- **`customText`** (string) — If non-empty, every occurrence of this substring is replaced with `spaceCharacter`.

Replacements are applied in order: `%20`, then space, then underscore, then custom text.

## Examples

- `spaceCharacter`: `"_"`; `replacePercent20`: `true`; other flags: `false`
  - Before: `Gone%20With%20The%20Wind`
  - After: `Gone_With_The_Wind`
- `spaceCharacter`: space; `replaceSpaces`: `true`; `replaceUnderscores`: `true`; `replacePercent20`:
  `true`
  - Before: `a_b c%20d`
  - After: `a b c d`
- `spaceCharacter`: `"-"`; `customText`: `"++"`; other flags: `false` — `a++b` → `a-b`
- `spaceCharacter`: `"_"`; `replacePercent20`: `true`; [LettersCase](../Case/LettersCase.md); `mode`:
  `TitleCase`; `skipWords`: `["the"]`
  - Before: `gone%20with%20the%20wind`
  - After: `Gone_With_the_Wind`
- `spaceCharacter`: `"_"`; `replacePercent20`: `true`; other flags: `false`
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
    "replaceSpaces": false,
    "replaceUnderscores": false,
    "replacePercent20": true,
    "customText": ""
  }
}
```
