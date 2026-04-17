# StripParentheses

Removes **one kind** of bracket pair: round `()`, square `[]`, curly `{}`, or angle `<>`. Either delete **only the delimiters** or **the whole bracketed region** (delimiters + inside), depending on options.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `type` | string (enum) | Bracket style: `Round`, `Square`, `Curly`, or `Angle`. (This is the `type` field **inside** `options`, not the filter’s top-level `type`.) |
| `removeContents` | bool | If `true`, remove opening + closing + everything between. If `false`, remove only the opening and closing characters (content stays). |

## Examples

| Options | Before | After |
|---------|--------|-------|
| `type`: `Round`, `removeContents`: `true` | `Song (live)` | `Song ` (space may remain) |
| `type`: `Round`, `removeContents`: `false` | `Song (live)` | `Song live` |
| `type`: `Square`, `removeContents`: `true` | `Song [EP]` | `Song ` |
