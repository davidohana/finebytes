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
| `capitalizeAfterChars` default | `hello,world!(again)[is]it-fine?` | `hello,World!(Again)[Is]It-Fine?` | |
| `capitalizeAfterChars`: `"._"` | `hello.world_again` | `hello.World_Again` | |
| `capitalizeAfterChars`: `"._"` | `a,b` | `a,b` | Comma not in custom set—no trigger. |
| `capitalizeAfterChars` default | `hello world` | `hello world` | No trigger immediately before a letter. |
| `capitalizeAfterChars` default | `hello!` | `hello!` | Nothing after `!` to capitalize. |
