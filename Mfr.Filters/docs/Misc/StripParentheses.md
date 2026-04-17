# StripParentheses

Removes **one kind** of bracket pair: round `()`, square `[]`, curly `{}`, or angle `<>`. Either delete **only the delimiters** or **the whole bracketed region** (delimiters + inside), depending on options.

Examples match [`StripParenthesesFilterTests`](../../../Mfr.Tests/Models/Filters/Misc/StripParenthesesFilterTests.cs) (remix tags, editorial notes).

## Options

| Property | Type | Description |
|----------|------|-------------|
| `type` | string (enum) | Bracket style: `Round`, `Square`, `Curly`, or `Angle`. (This is the `type` field **inside** `options`, not the filter’s top-level `type`.) |
| `removeContents` | bool | If `true`, remove opening + closing + everything between. If `false`, remove only the opening and closing characters (content stays). |

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `type`: `Round`, `removeContents`: `true` | `a(rem)b` | `ab` | |
| `type`: `Round`, `removeContents`: `false` | `a(rem)` | `arem` | Delimiters removed; inner text kept. |
| `type`: `Square`, `removeContents`: `true` | `a[xx]b` | `ab` | |
