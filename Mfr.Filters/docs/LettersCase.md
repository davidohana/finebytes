# LettersCase

Changes letter casing on the target segment. **Sentence case** and **title case** use the current [word separator](SpaceCharacter.md) (default: space). **Sentence case** also uses [sentence-end characters](SentenceEndCharacters.md) (default `.!?` until you add that filter).

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

| Options | Before | After |
|---------|--------|-------|
| `mode`: `UpperCase` | `Hello` | `HELLO` |
| `mode`: `LowerCase` | `HeLLo` | `hello` |
| `mode`: `FirstLetterUp` | `hELLO world` | `Hello world` |
| `mode`: `TitleCase`, `skipWords`: `["a","the","for"]` | `a song for the world` | `a Song for the World` |
| `mode`: `SentenceCase` (default [SentenceEndCharacters](SentenceEndCharacters.md)) | `hello world. next line.` | `Hello world. Next line.` |
| `mode`: `InvertCase` | `Hello` | `hELLO` |
| `mode`: `WeirdCase`, `weirdUppercaseChancePercent`: `0` | `AbC` | `abc` (letters lowercased) |
| `mode`: `WeirdCase`, `weirdUppercaseChancePercent`: `100` | `AbC` | `ABC` (letters uppercased) |

Unused option properties for a given `mode` are ignored.
