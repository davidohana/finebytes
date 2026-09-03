# LettersCase

Changes letter casing on the target segment. **Capitalize** and **sentence case** use the [word separator](../Space/SpaceCharacter.md) (default space). **Sentence case** uses [SentenceEndCharacters](SentenceEndCharacters.md) (default `.!?` until that filter runs).

## Options

- **`mode`** (enum, required) — One of the **Modes** below.
- **`capitalizeSkipWords`** (array of string, default `[]`)
  - **Capitalize only:** words matched case-insensitively stay lowercase.
- **`weirdUppercaseChancePercent`** (int, default `50`)
  - **Weird case only:** chance each letter is uppercased (clamped 0–100).
- **`weirdFixedPlaces`** (bool, default `false`)
  - **Weird case only:** if `true`, casing depends only on character index (same positions across files); if `false`,
    the per-file index affects randomness.

### Modes (`mode`)

- **`UpperCase`** — All letters uppercase.
- **`LowerCase`** — All letters lowercase.
- **`FirstLetterUp`** — First character uppercased if it is a letter; rest of segment lowercased.
- **`WeirdCase`** — Random/mixed case per `weirdUppercaseChancePercent` / `weirdFixedPlaces`.
- **`Capitalize`** — Each word (between word separators) capitalized; `capitalizeSkipWords` stay lower.
- **`SentenceCase`**
  - Whole segment lowercased, then first letter of segment and after sentence ends (see
    [SentenceEndCharacters](SentenceEndCharacters.md)), when followed by separator(s).
- **`InvertCase`** — Swap upper ↔ lower for letters.

## Examples

- `mode`: `UpperCase` — `hello` → `HELLO`
- `mode`: `LowerCase` — `HELLO` → `hello`
- `mode`: `FirstLetterUp` — `hELLO world` → `Hello world`
- `mode`: `FirstLetterUp` — ` 123_aBC` → ` 123_abc`
- `mode`: `WeirdCase`; `weirdUppercaseChancePercent`: `0` — `AbC XyZ` → `abc xyz`
- `mode`: `WeirdCase`; `weirdUppercaseChancePercent`: `100` — `AbC XyZ` → `ABC XYZ`
- `mode`: `Capitalize`; `capitalizeSkipWords`: `["a","the","for"]` — `a song for the world` → `a Song for the World`
- `mode`: `SentenceCase`; (default [sentence-end characters](SentenceEndCharacters.md))
  - Before: `hello world. next line.`
  - After: `Hello world. Next line.`
- `mode`: `InvertCase` — `Hello` → `hELLO`
- [SentenceEndCharacters](SentenceEndCharacters.md); `characters`: `":;"`; [LettersCase](LettersCase.md);
  `mode`: `SentenceCase`
  - Before: `hello: next; again. no`
  - After: `Hello: Next; Again. no`
- [SentenceEndCharacters](SentenceEndCharacters.md); `characters`: `""`; [LettersCase](LettersCase.md);
  `mode`: `SentenceCase`
  - Before: `hello. next line`
  - After: `Hello. next line`
  - Comment: Empty `characters` → cap only at start.
- [SentenceEndCharacters](SentenceEndCharacters.md); `characters`: `". "`; [LettersCase](LettersCase.md);
  `mode`: `SentenceCase`
  - Before: `hello world. next line`
  - After: `Hello world. Next line`
  - Comment: Same as `characters` `"."` when separator is space.
- [SpaceCharacter](../Space/SpaceCharacter.md); `spaceCharacter`: `"_"`; [LettersCase](LettersCase.md);
  `mode`: `SentenceCase`
  - Before: `hello._world._again`
  - After: `Hello._World._Again`
- [SpaceCharacter](../Space/SpaceCharacter.md); `spaceCharacter`: `"_"`; [LettersCase](LettersCase.md);
  `mode`: `Capitalize`; `capitalizeSkipWords`: `["the"]`
  - Before: `__gone__with__the__wind__`
  - After: `__Gone__With__the__Wind__`
- [SpaceCharacter](../Space/SpaceCharacter.md); `spaceCharacter`: `"_"`; [LettersCase](LettersCase.md);
  `mode`: `SentenceCase`
  - Before: `hello.__world!___again?__done`
  - After: `Hello.__World!___Again?__Done`
- [SpaceCharacter](../Space/SpaceCharacter.md); `spaceCharacter`: `"_"`; [LettersCase](LettersCase.md);
  `mode`: `SentenceCase`
  - Before: `hello.world`
  - After: `Hello.world`
  - Comment: No separator after `.` → no cap.

Unused option properties for a given `mode` are ignored.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)).

```json
{
  "type": "LettersCase",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "mode": "Capitalize",
    "capitalizeSkipWords": ["a", "an", "the", "of"],
    "weirdUppercaseChancePercent": 50,
    "weirdFixedPlaces": false
  }
}
```
