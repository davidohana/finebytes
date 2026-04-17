# Case filters

Filters that change capitalization or apply a custom word list.

---

## LettersCase

Changes how letters are cased across the whole target segment.

**Options (conceptually):**

- **Mode** — what to do: upper case, lower case, first letter only, title case, sentence case, weird (random) case, invert case, etc.

**Examples**

| Mode (idea) | Before | After |
|-------------|--------|--------|
| Upper | `hello` | `HELLO` |
| Lower | `HeLLo` | `hello` |
| Title case | `hello world` | `Hello World` (with optional skip-words left lower) |
| Sentence case | `hello world. next` | `Hello world. Next` |

**Sentence case** uses the **word separator** from `SpaceCharacter` (default: space) and **sentence end characters** from `SentenceEndCharacters` (default: `.!?` until you add that filter). Place `SentenceEndCharacters` (and optionally `SpaceCharacter`) *before* this filter when you need custom behavior.

---

## CapitalizeAfter

Uppercases the single character *immediately after* any character in your configured list (for example after `,` or `(`).

**Example**

- Characters include `(`  
- `hello (world)` → `hello (World)`

---

## UppercaseInitials

Finds “initials” patterns: single letters separated by dots (for example `u.s.a`, `d.j`) and uppercases only those letters. Everything else stays as-is.

**Example**

- `track u.s.a mix` → `track U.S.A mix`

---

## CasingList

Loads a text file with **one word per line**. Each “word” in the target (split by the current **word separator**, usually space) is looked up case-insensitively. If it appears in the file, it is rewritten to match the **exact spelling** in the file. Words not in the list are unchanged.

Optional: **uppercase first letter in sentence** uses `SentenceEndCharacters` (and the word separator) the same way as sentence-style rules elsewhere—put `SentenceEndCharacters` before this filter if you need custom sentence boundaries.

**Example** (list contains `and`, `RMX`)

- `WiTH Or Rmx` → `with Or RMX` (unknown words like `Or` stay as in the input except list matches)

---

## SentenceEndCharacters

Does **not** change the text. It sets which characters count as **sentence endings** for the current file on this preview pass. Later filters (`LettersCase` in sentence mode, `CasingList` with sentence initials) read this setting.

**Example**

- Set characters to `-.!`  
- Then sentence-style rules treat `-`, `.`, and `!` as places after which the next word may get a capital (depending on the next filter).

Default if you never use this filter: `.!?`.

---

### Tips

- Put **`SpaceCharacter`** before title/sentence/casing-list behavior when filenames use `_` or another separator instead of spaces.
- Put **`SentenceEndCharacters`** before **`LettersCase`** (sentence) or **`CasingList`** (sentence initials) when you need custom punctuation as sentence breaks.
