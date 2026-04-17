# CasingList

Loads a text file with **one word per line**. Each “word” in the target (split by the current **word separator**, usually space) is looked up case-insensitively. If it appears in the file, it is rewritten to match the **exact spelling** in the file. Words not in the list are unchanged.

Optional: **uppercase first letter in sentence** uses [SentenceEndCharacters](SentenceEndCharacters.md) (and the word separator) the same way as sentence-style rules elsewhere—put `SentenceEndCharacters` before this filter if you need custom sentence boundaries.

**Example** (list contains `and`, `RMX`)

- `WiTH Or Rmx` → `with Or RMX` (unknown words like `Or` stay as in the input except list matches)

**Tips**

- Put [SpaceCharacter](SpaceCharacter.md) before title/sentence/casing-list behavior when filenames use `_` or another separator instead of spaces.
- Put [SentenceEndCharacters](SentenceEndCharacters.md) before **CasingList** when you use sentence initials and need custom punctuation as sentence breaks.
