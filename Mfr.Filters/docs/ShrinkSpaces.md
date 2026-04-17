# ShrinkSpaces

Collapses **runs** of the word-separator character to a **single** occurrence. The separator comes from [SpaceCharacter](SpaceCharacter.md) when used earlier (default is ordinary space).

**Example**

- `hello    world` → `hello world` (when separator is space)

Combine with [SpaceCharacter](SpaceCharacter.md) to normalize messy separators.
