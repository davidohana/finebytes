# SpaceAfter

Inserts the **word separator** after each character from the configured list when it is missing. The separator is [SpaceCharacter](SpaceCharacter.md)’s configured character, defaulting to U+0020 SPACE.

With **`onlyWhenNextIsLetterOrDigit`** **true**, insertion happens only when the next character in the text is a Unicode letter or digit (so punctuation-only runs such as `!!` are unchanged after the first `!`).

## Examples

- `afterChars`: `",;!"`; `onlyWhenNextIsLetterOrDigit`: `true` — `one,two,three` → `one, two, three`
- `afterChars`: `",;!"`; `onlyWhenNextIsLetterOrDigit`: `true` — `one, two,three` → `one, two, three`
- `afterChars`: `",;!"`; `onlyWhenNextIsLetterOrDigit`: `true`
  - Before: `Blaaa!blaaa!!`
  - After: `Blaaa! blaaa!!`
  - Comment: Second `!` is followed by `!`, not a letter/digit.
- `afterChars`: `","`; `onlyWhenNextIsLetterOrDigit`: `false`
  - Before: `a,b`
  - After: `a, b`
  - Comment: Inserts even when the next character is not a letter/digit.
- [SpaceCharacter](SpaceCharacter.md); `spaceCharacter`: `"_"` (prefix target; no replacements required);
  then SpaceAfter; `afterChars`: `","`; `onlyWhenNextIsLetterOrDigit`: `false`
  - Before: `x,y`
  - After: `x,_y`
  - Comment: Separator is `_`; insertion is immediately after the comma, before the original next character.

## Sample preset (JSON)

```json
{
  "type": "SpaceAfter",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "afterChars": ",;!",
    "onlyWhenNextIsLetterOrDigit": true
  }
}
```
