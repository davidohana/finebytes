# LettersCase

Changes letter casing on the target segment. **Sentence case** and **title case** use the current [word separator](SpaceCharacter.md) (default: space). **Sentence case** also uses [sentence-end characters](SentenceEndCharacters.md) (default `.!?` until you add that filter).

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `LettersCase`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See **Target** below. |
| `options` | object | See **Options** below. |

### Target

`target` selects what is transformed. For file names, use `family` `FileName` and `fileNamePart`:

| `fileNamePart` | Segment |
|------------------|---------|
| `Prefix` | Name without extension |
| `Extension` | Extension including the leading dot |
| `Full` | Full file name (prefix + extension) |

### Options (`options`)

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

## Examples (conceptual)

**Upper case**

- Input: `Hello` → Output: `HELLO`

**Title case with skip words**

- `skipWords`: `["a","the","for"]`  
- Input: `a song for the world` → Output: `a Song for the World`

**Sentence case (defaults)**

- Input: `hello world. next line.` → Output: `Hello world. Next line.`

**Weird case**

- `weirdUppercaseChancePercent`: `0` → effectively all lowercase (for letters).  
- `100` → all letters uppercase.

**Example preset fragment (title case on prefix)**

```json
{
  "type": "LettersCase",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "mode": "TitleCase",
    "skipWords": [ "a", "the", "and" ],
    "weirdUppercaseChancePercent": 50,
    "weirdFixedPlaces": false
  }
}
```

`skipWords`, `weirdUppercaseChancePercent`, and `weirdFixedPlaces` matter only for the modes that use them; other modes ignore them.
