# SpaceCharacter

Sets the **word separator character** and can rewrite common substitutes (spaces, underscores, `%20`, custom text) into that character. Later filters such as [ShrinkSpaces](ShrinkSpaces.md), [RemoveSpaces](RemoveSpaces.md), [StripSpacesLeft](StripSpacesLeft.md), [StripSpacesRight](StripSpacesRight.md), and case/casing-list logic use this character.

**Example**

- Set separator to `_` and map spaces to it: `my song` → `my_song`

**Tips**

- Put **SpaceCharacter** **first** when you want underscores (or another separator) to define “words” for later filters.
- Combine [ShrinkSpaces](ShrinkSpaces.md) with **SpaceCharacter** to normalize messy separators.
