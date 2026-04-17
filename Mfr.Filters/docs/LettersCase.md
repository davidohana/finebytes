# LettersCase

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

**Sentence case** uses the **word separator** from [SpaceCharacter](SpaceCharacter.md) (default: space) and **sentence end characters** from [SentenceEndCharacters](SentenceEndCharacters.md) (default: `.!?` until you add that filter). Place `SentenceEndCharacters` (and optionally `SpaceCharacter`) *before* this filter when you need custom behavior.
