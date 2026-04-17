# ShrinkSpaces

Collapses **each run** of the current **word separator** into a **single** character. The separator comes from [SpaceCharacter](SpaceCharacter.md) (default: space).

No `options` object.

## Examples

- `hello    world` → `hello world`
- With separator `_`: `a___b` → `a_b`

Often used with [SpaceCharacter](SpaceCharacter.md) to normalize messy separators.
