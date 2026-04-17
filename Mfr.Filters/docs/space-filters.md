# Space filters

Control what counts as a “space” between words and how runs of spaces behave.

The **word separator** defaults to ordinary space (`U+0020`). Several filters read `WordSeparator` from the current rename item; **`SpaceCharacter`** is the filter that sets it for the rest of the chain.

---

## SpaceCharacter

Sets the **word separator character** and can rewrite common substitutes (spaces, underscores, `%20`, custom text) into that character. Later filters such as **ShrinkSpaces**, **RemoveSpaces**, **StripSpacesLeft**, **StripSpacesRight**, and case/casing-list logic use this character.

**Example**

- Set separator to `_` and map spaces to it: `my song` → `my_song`

---

## RemoveSpaces

Deletes **every** occurrence of the current word-separator character (no gaps left).

**Example**

- Separator is space: `a b c` → `abc`

---

## ShrinkSpaces

Collapses **runs** of the word-separator character to a **single** occurrence.

**Example**

- `hello    world` → `hello world` (when separator is space)

---

## StripSpacesLeft

Trims the word-separator character from the **start** of the segment only.

**Example**

- `  hello` → `hello`

---

## StripSpacesRight

Trims the word-separator character from the **end** of the segment only.

**Example**

- `hello  ` → `hello`

---

### Tips

- Put **SpaceCharacter** **first** when you want underscores (or another separator) to define “words” for later filters.
- Combine **ShrinkSpaces** with **SpaceCharacter** to normalize messy separators.
