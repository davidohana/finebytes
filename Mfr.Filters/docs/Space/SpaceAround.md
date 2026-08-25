# SpaceAround

Inserts the **word separator** before and after each character from the configured list when it is missing on that side. The separator is [SpaceCharacter](SpaceCharacter.md)’s configured character, defaulting to U+0020 SPACE.

With **`onlyWhenNeighboringAreLettersOrDigits`** **true**, a separator is added **before** the trigger only when the character to the left is a Unicode letter or digit, and **after** the trigger only when the character to the right is a Unicode letter or digit (each side is evaluated independently).

## Examples

- `aroundChars`: `"-"`; `onlyWhenNeighboringAreLettersOrDigits`: `true`
  - Before: `Aimee Mann-Stupid Thing`
  - After: `Aimee Mann - Stupid Thing`
- `aroundChars`: `"-"`; `onlyWhenNeighboringAreLettersOrDigits`: `true`
  - Before: `Aimee Mann- Stupid Thing`
  - After: `Aimee Mann - Stupid Thing`
  - Comment: Space already after hyphen on one side.
- `aroundChars`: `"-"`; `onlyWhenNeighboringAreLettersOrDigits`: `true`
  - Before: `Aimee Mann - Stupid Thing`
  - After: `Aimee Mann - Stupid Thing`
  - Comment: Already normalized.
- `aroundChars`: `"-"`; `onlyWhenNeighboringAreLettersOrDigits`: `true`
  - Before: `Aimee Mann--Stupid Thing`
  - After: `Aimee Mann -- Stupid Thing`
  - Comment: Each `-` is handled separately: no space between the two hyphens; space after the run before `S`.

## Sample preset (JSON)

```json
{
  "type": "SpaceAround",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "aroundChars": "-",
    "onlyWhenNeighboringAreLettersOrDigits": true
  }
}
```
