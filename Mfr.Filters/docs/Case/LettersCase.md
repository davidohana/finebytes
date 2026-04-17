# LettersCase

Changes letter casing on the target segment. **Sentence case** and **title case** use the current [word separator](../Space/SpaceCharacter.md) (default: space). **Sentence case** also uses [sentence-end characters](SentenceEndCharacters.md) (default `.!?` until you add that filter).

Examples below mirror [`LettersCaseFilterTests`](../../../Mfr.Tests/Models/Filters/Case/LettersCaseFilterTests.cs) (music-style titles where it helps).

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
| `mode`: `UpperCase` | `hello` | `HELLO` | All letters uppercased. |
| `mode`: `LowerCase` | `HELLO` | `hello` | All letters lowercased. |
| `mode`: `FirstLetterUp` | `hELLO world` | `Hello world` | First letter of segment only. |
| `mode`: `FirstLetterUp` | ` 123_aBC` | ` 123_abc` | Leading non-letter run unchanged; first letter lowercased from there. |
| `mode`: `WeirdCase`, `weirdUppercaseChancePercent`: `0` | `AbC XyZ` | `abc xyz` | Always lowercase letters. |
| `mode`: `WeirdCase`, `weirdUppercaseChancePercent`: `100` | `AbC XyZ` | `ABC XYZ` | Always uppercase letters. |
| `mode`: `TitleCase`, `skipWords`: `["a","the","for"]` | `a song for the world` | `a Song for the World` | Article words stay lower per list. |
| `mode`: `SentenceCase` (default sentence ends) | `hello world. next line.` | `Hello world. Next line.` | Capital after `.` when followed by separator. |
| `mode`: `InvertCase` | `Hello` | `hELLO` | Swap case per letter. |
| Chain: [SentenceEndCharacters](SentenceEndCharacters.md) `characters`: `":;"` then `mode`: `SentenceCase` | `hello: next; again. no` | `Hello: Next; Again. no` | Custom sentence ends `:` and `;`. |
| Chain: [SentenceEndCharacters](SentenceEndCharacters.md) `characters`: `""` then `SentenceCase` | `hello. next line` | `Hello. next line` | Empty set: only start of segment is capitalized. |
| Chain: [SentenceEndCharacters](SentenceEndCharacters.md) `characters`: `". "` then `SentenceCase` | `hello world. next line` | `Hello world. Next line` | Sentence end is `.` + space together. |
| Chain: [SpaceCharacter](../Space/SpaceCharacter.md) `spaceCharacter`: `"_"` then `SentenceCase` | `hello._world._again` | `Hello._World._Again` | Underscore is word separator. |
| Chain: [SpaceCharacter](../Space/SpaceCharacter.md) `spaceCharacter`: `"_"`, `TitleCase`, `skipWords`: `["the"]` | `__gone__with__the__wind__` | `__Gone__With__the__Wind__` | Title case with `%20`-style input normalized first. |
| Chain: [SpaceCharacter](../Space/SpaceCharacter.md) `spaceCharacter`: `"_"` then `SentenceCase` | `hello.__world!___again?__done` | `Hello.__World!___Again?__Done` | Mixed `_` and punctuation. |
| Chain: [SpaceCharacter](../Space/SpaceCharacter.md) `spaceCharacter`: `"_"` then `SentenceCase` | `hello.world` | `Hello.world` | No separator after `.`, so no capital after that “sentence end”. |

Unused option properties for a given `mode` are ignored.
