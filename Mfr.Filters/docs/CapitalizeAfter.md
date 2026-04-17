# CapitalizeAfter

Uppercases the **single character immediately after** any character that appears in your configured list. Other characters are unchanged. If the list is empty, the segment is unchanged.

## Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `capitalizeAfterChars` | string | `",!()[]{};-"` | Every character in this string is a trigger: the **next** character in the segment is uppercased (default includes comma, `!`, parentheses, brackets, `;`, `-`). |

## Examples

| Options | Before | After |
|---------|--------|-------|
| `capitalizeAfterChars`: default `",!()[]{};-"` | `hello (world)` | `hello (World)` |
| `capitalizeAfterChars`: `"-"` | `hello-world` | `hello-World` |
| `capitalizeAfterChars`: `""` | `hello-world` | `hello-world` |
