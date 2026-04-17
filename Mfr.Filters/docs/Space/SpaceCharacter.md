# SpaceCharacter

Sets the **word separator** character for the rest of the rename pass and optionally **replaces** common stand-ins (normal spaces, underscores, `%20`, custom text) with that character. Later filters ([ShrinkSpaces](ShrinkSpaces.md), [RemoveSpaces](RemoveSpaces.md), strip-space filters, and case/casing-list word splitting) use `WordSeparator` (default is ordinary space until this filter runs).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `spaceCharacter` | string or char | **Single** character that becomes the word separator (first character used if a longer string is sent). |
| `replaceSpaces` | bool | Replace U+0020 SPACE with `spaceCharacter`. |
| `replaceUnderscores` | bool | Replace `_` with `spaceCharacter`. |
| `replacePercent20` | bool | Replace the literal text `%20` with `spaceCharacter`. |
| `customText` | string | If non-empty, every occurrence of this substring is replaced with `spaceCharacter`. |

Replacements are applied in order: `%20`, then space, then underscore, then custom text.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `spaceCharacter`: `"_"`<br>`replacePercent20`: `true`<br>other flags: `false` | `Gone%20With%20The%20Wind` | `Gone_With_The_Wind` | |
| `spaceCharacter`: space<br>`replaceSpaces`: `true`<br>`replaceUnderscores`: `true`<br>`replacePercent20`: `true` | `a_b c%20d` | `a b c d` | |
| `spaceCharacter`: `"-"`<br>`customText`: `"++"`<br>other flags: `false` | `a++b` | `a-b` | |
| `spaceCharacter`: `"_"`<br>`replacePercent20`: `true`<br>[LettersCase](../Case/LettersCase.md)<br>`mode`: `TitleCase`<br>`skipWords`: `["the"]` | `gone%20with%20the%20wind` | `Gone_With_the_Wind` | |
| `spaceCharacter`: `"_"`<br>`replacePercent20`: `true`<br>other flags: `false` | `my song` | `my song` | Text unchanged; `WordSeparator` still set to `_` for later filters. |

Put this filter **before** any filter that should use a non-space word boundary.
