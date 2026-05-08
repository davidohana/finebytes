# SpaceAfter

Inserts the **word separator** after each character from the configured list when it is missing. The separator is [SpaceCharacter](SpaceCharacter.md)’s configured character, defaulting to U+0020 SPACE.

With **`onlyWhenNextIsLetterOrDigit`** **true**, insertion happens only when the next character in the text is a Unicode letter or digit (so punctuation-only runs such as `!!` are unchanged after the first `!`).

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `afterChars`: `",;!"`<br>`onlyWhenNextIsLetterOrDigit`: `true` | `one,two,three` | `one, two, three` | |
| `afterChars`: `",;!"`<br>`onlyWhenNextIsLetterOrDigit`: `true` | `one, two,three` | `one, two, three` | |
| `afterChars`: `",;!"`<br>`onlyWhenNextIsLetterOrDigit`: `true` | `Blaaa!blaaa!!` | `Blaaa! blaaa!!` | Second `!` is followed by `!`, not a letter/digit. |
| `afterChars`: `","`<br>`onlyWhenNextIsLetterOrDigit`: `false` | `a,b` | `a, b` | Inserts even when the next character is not a letter/digit. |
| [SpaceCharacter](SpaceCharacter.md)<br>`spaceCharacter`: `"_"` (prefix target; no replacements required)<br>then SpaceAfter<br>`afterChars`: `","`<br>`onlyWhenNextIsLetterOrDigit`: `false` | `x,y` | `x,_y` | Separator is `_`; insertion is immediately after the comma, before the original next character. |

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
