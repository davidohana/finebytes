# TokenMover

Rearranges **tokens** (name parts) split by a delimiter string. You can move one token left or right by a number of positions—useful for swapping artist and title segments, or similar.

Can be applied to **any text field** (same `target` rules as other filters).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `delimiter` | string | **Token separator string.** The target segment is split by this exact substring into ordered tokens. Example: separator `-` splits `Artist-Album-Title` into three tokens: `Artist`, `Album`, `Title`. |
| `tokenNumber` | int | **One-based** index of the token to move (first token is `1`). Must not exceed the number of tokens in the segment. |
| `moveBy` | int | **Distance and direction** in token positions: positive = toward the **end** of the token list, negative = toward the **start**. Example: `-1` swaps the selected token with its **preceding** token (after removal and reinsertion, equivalent to one step left). |

### Bounds

If the requested move would place the token **before** the first position or **after** the last position, it is placed at the **first** or **last** position respectively.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `delimiter`: `","`<br>`tokenNumber`: `2`<br>`moveBy`: `3` | `milk,sugar,bread,potatoes,honey,salt,water` | `milk,bread,potatoes,honey,sugar,salt,water` | Move 2nd token three places right. |
| `delimiter`: `","`<br>`tokenNumber`: `2`<br>`moveBy`: `3` | `milk,sugar,bread,potatoes` | `milk,bread,potatoes,sugar` | Same move; target position past end → last slot. |
| `delimiter`: `"-"`<br>`tokenNumber`: `2`<br>`moveBy`: `-1` | `a-b-c` | `b-a-c` | One step left: swap with preceding token. |
