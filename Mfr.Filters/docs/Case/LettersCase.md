# LettersCase

Changes letter casing on the target segment. **Title case** and **sentence case** use the [word separator](../Space/SpaceCharacter.md) (default space). **Sentence case** uses [SentenceEndCharacters](SentenceEndCharacters.md) (default `.!?` until that filter runs).

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `mode` | string (enum) | (required) | One of the **Modes** below. |
| `skipWords` | array of string | `[]` | **Title case only:** words matched case-insensitively stay lowercase. |
| `weirdUppercaseChancePercent` | int | `50` | **Weird case only:** chance each letter is uppercased (clamped 0–100). |
| `weirdFixedPlaces` | bool | `false` | **Weird case only:** if `true`, casing depends only on character index (same positions across files); if `false`, the per-file index affects randomness. |

### Modes (`mode`)

| Value | Behavior |
|--------|----------|
| `UpperCase` | All letters uppercase. |
| `LowerCase` | All letters lowercase. |
| `FirstLetterUp` | First character uppercased if it is a letter; rest of segment lowercased. |
| `WeirdCase` | Random/mixed case per `weirdUppercaseChancePercent` / `weirdFixedPlaces`. |
| `TitleCase` | Each word (between word separators) title-cased; `skipWords` stay lower. |
| `SentenceCase` | Whole segment lowercased, then first letter of segment and after sentence ends (see [SentenceEndCharacters](SentenceEndCharacters.md)), when followed by separator(s). |
| `InvertCase` | Swap upper ↔ lower for letters. |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `mode`: `UpperCase` | `hello` | `HELLO` | |
| `mode`: `LowerCase` | `HELLO` | `hello` | |
| `mode`: `FirstLetterUp` | `hELLO world` | `Hello world` | |
| `mode`: `FirstLetterUp` | ` 123_aBC` | ` 123_abc` | |
| `mode`: `WeirdCase`<br>`weirdUppercaseChancePercent`: `0` | `AbC XyZ` | `abc xyz` | |
| `mode`: `WeirdCase`<br>`weirdUppercaseChancePercent`: `100` | `AbC XyZ` | `ABC XYZ` | |
| `mode`: `TitleCase`<br>`skipWords`: `["a","the","for"]` | `a song for the world` | `a Song for the World` | |
| `mode`: `SentenceCase`<br>(default [sentence-end characters](SentenceEndCharacters.md)) | `hello world. next line.` | `Hello world. Next line.` | |
| `mode`: `InvertCase` | `Hello` | `hELLO` | |
| [SentenceEndCharacters](SentenceEndCharacters.md)<br>`characters`: `":;"`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello: next; again. no` | `Hello: Next; Again. no` | |
| [SentenceEndCharacters](SentenceEndCharacters.md)<br>`characters`: `""`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello. next line` | `Hello. next line` | Empty `characters` → cap only at start. |
| [SentenceEndCharacters](SentenceEndCharacters.md)<br>`characters`: `". "`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello world. next line` | `Hello world. Next line` | Same as `characters` `"."` when separator is space. |
| [SpaceCharacter](../Space/SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello._world._again` | `Hello._World._Again` | |
| [SpaceCharacter](../Space/SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>[LettersCase](LettersCase.md)<br>`mode`: `TitleCase`<br>`skipWords`: `["the"]` | `__gone__with__the__wind__` | `__Gone__With__the__Wind__` | |
| [SpaceCharacter](../Space/SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello.__world!___again?__done` | `Hello.__World!___Again?__Done` | |
| [SpaceCharacter](../Space/SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello.world` | `Hello.world` | No separator after `.` → no cap. |

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
    "mode": "TitleCase",
    "skipWords": ["a", "an", "the", "of"],
    "weirdUppercaseChancePercent": 50,
    "weirdFixedPlaces": false
  }
}
```
