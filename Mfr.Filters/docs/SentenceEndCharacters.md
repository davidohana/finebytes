# SentenceEndCharacters

Does **not** change the text. It sets which characters count as **sentence endings** for the current file on this preview pass. Later filters ([LettersCase](LettersCase.md) in sentence mode, [CasingList](CasingList.md) with sentence initials) read this setting.

**Example**

- Set characters to `-.!`  
- Then sentence-style rules treat `-`, `.`, and `!` as places after which the next word may get a capital (depending on the next filter).

Default if you never use this filter: `.!?`.

**Tips**

- Put [SpaceCharacter](SpaceCharacter.md) before title/sentence/casing-list behavior when filenames use `_` or another separator instead of spaces.
- Put this filter before [LettersCase](LettersCase.md) (sentence) or [CasingList](CasingList.md) (sentence initials) when you need custom punctuation as sentence breaks.
