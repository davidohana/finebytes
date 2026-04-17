# CapitalizeAfter

Uppercases the **single character immediately after** any character that appears in your configured list. Other characters are unchanged. If the list is empty, the segment is unchanged.

Examples match [`CapitalizeAfterFilterTests`](../../../Mfr.Tests/Models/Filters/Case/CapitalizeAfterFilterTests.cs) (comma/punctuation in messy tags, custom `._` triggers).

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `capitalizeAfterChars` | string | `",!()[]{};-"` | Every character in this string is a trigger: the **next** character in the segment is uppercased (default includes comma, `!`, parentheses, brackets, `;`, `-`). |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `capitalizeAfterChars` default | `hello,world!(again)[is]it-fine?` | `hello,World!(Again)[Is]It-Fine?` | Default triggers include `, ! ( ) [ ] ; -`. |
| `capitalizeAfterChars`: `"._"` | `hello.world_again` | `hello.World_Again` | Only `.` and `_` trigger. |
| `capitalizeAfterChars`: `"._"` | `a,b` | `a,b` | Comma not in custom set, so no change after `,`. |
| `capitalizeAfterChars` default | `hello world` | `hello world` | No trigger character immediately before a letter to capitalize. |
| `capitalizeAfterChars` default | `hello!` | `hello!` | Nothing after `!` to capitalize. |
