# SeparateCapitalizedText

Inserts the current **word separator** between selected pairs of characters so **camel-case** runs and **letter–digit** boundaries split into separate tokens. The separator is [SpaceCharacter](SpaceCharacter.md) when that filter appears earlier in the chain; otherwise it is U+0020 SPACE.

Insertion happens **between** two consecutive characters when **any** of these is true:

- The first is **lowercase** and the second is **uppercase**.
- The first is a **letter** and the second is a **digit**.
- The first is a **digit** and the second is a **letter**.

Runs of digits stay together (for example `01` is not split). Pairs of uppercase letters are not split by this rule alone (there is no lowercase letter before the second cap).

No `options` object.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| (no options)<br>default word separator | `DandyWorhols01Godless` | `Dandy Worhols 01 Godless` | |
| (no options)<br>default word separator | `song2remix` | `song 2 remix` | Letter–digit and digit–letter boundaries. |
| [SpaceCharacter](SpaceCharacter.md)<br>`spaceCharacter`: `"_"`<br>other flags: `false`<br>then this filter | `aBc12x` | `a_Bc_12_x` | Same rules; inserts underscores. |

Put [SpaceCharacter](SpaceCharacter.md) **before** this filter when you want a non-space separator.

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). No `options` property.

```json
{
  "type": "SeparateCapitalizedText",
  "target": {
    "targetType": "FilePrefix"
  }
}
```
