# LettersCase

Changes letter casing on the target segment. **Sentence case** and **title case** use the current [word separator](../Space/SpaceCharacter.md) (default: space). **Sentence case** also uses [sentence-end characters](SentenceEndCharacters.md) (default `.!?` until you add that filter).

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
| `mode`: `FirstLetterUp` | ` 123_aBC` | ` 123_abc` | Leading non-letters unchanged; casing starts at first letter. |
| `mode`: `WeirdCase`<br>`weirdUppercaseChancePercent`: `0` | `AbC XyZ` | `abc xyz` | 0% → all lowercase. |
| `mode`: `WeirdCase`<br>`weirdUppercaseChancePercent`: `100` | `AbC XyZ` | `ABC XYZ` | 100% → all uppercase. |
| `mode`: `TitleCase`<br>`skipWords`: `["a","the","for"]` | `a song for the world` | `a Song for the World` | `skipWords` stay lowercase. |
| `mode`: `SentenceCase`<br>(default [sentence-end characters](SentenceEndCharacters.md)) | `hello world. next line.` | `Hello world. Next line.` | |
| `mode`: `InvertCase` | `Hello` | `hELLO` | |
| [SentenceEndCharacters](SentenceEndCharacters.md)<br>`characters`: `":;"`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello: next; again. no` | `Hello: Next; Again. no` | |
| [SentenceEndCharacters](SentenceEndCharacters.md)<br>`characters`: `""`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello. next line` | `Hello. next line` | Empty set: no caps after punctuation, only at segment start. |
| [SentenceEndCharacters](SentenceEndCharacters.md)<br>`characters`: `". "`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello world. next line` | `Hello world. Next line` | Sentence end is the two-char sequence `. `, not `.` alone. |
| [SpaceCharacter](../Space/SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello._world._again` | `Hello._World._Again` | |
| [SpaceCharacter](../Space/SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>[LettersCase](LettersCase.md)<br>`mode`: `TitleCase`<br>`skipWords`: `["the"]` | `__gone__with__the__wind__` | `__Gone__With__the__Wind__` | |
| [SpaceCharacter](../Space/SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello.__world!___again?__done` | `Hello.__World!___Again?__Done` | |
| [SpaceCharacter](../Space/SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>[LettersCase](LettersCase.md)<br>`mode`: `SentenceCase` | `hello.world` | `Hello.world` | No word separator after `.`, so no capital after the period. |

Unused option properties for a given `mode` are ignored.
