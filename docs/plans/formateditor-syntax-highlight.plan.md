# FormatEditor syntax highlighting

Status: **shipped**. Cleared from [`docs/debts.md`](../debts.md).

## Goal

Color recognized format tokens in the shared FormatEditor (not MFR7 red/blue zebra).

## UX

| State                        | Treatment                                                               |
| ---------------------------- | ----------------------------------------------------------------------- |
| Literal text / token args    | Default editor foreground (args include the `:` after the written name) |
| Valid token delimiters       | Quieter accent (`FormatTokenDelimiterForegroundBrush` for `<` / `>`)    |
| Valid token name             | Stronger accent (`FormatTokenNameForegroundBrush`; uses written name)   |
| Failing span                 | Soft error background (`FormatTokenErrorBackgroundBrush`)               |
| Valid tokens before an error | Still name/delimiter-colored                                            |

## Implementation

1. Engine: `FormatStringSyntax.TryValidate` returns prior `FormatTokenSpan`s (with `WrittenName` from the scan) on unknown-token / Compile failure (walk failures still empty).
1. UI: `Avalonia.AvaloniaEdit` hosts the template field; `FormatTokenColorizingTransformer` colors delimiter vs written name from `LastParseResult` (args stay default fg).
1. Themes: Light/Dark delimiter + name brushes in [`FilterEditor.axaml`](../../Mfr.App.Ui/Themes/FilterEditor.axaml).

## Out of scope

- Nested `source=` highlight / nested FormatEditor
- Category rainbows / alternating backgrounds

## References

- F6 deferral: [`formatter-formateditor-ux.plan.md`](formatter-formateditor-ux.plan.md)
- Applied Filters backlog: [`applied-filter-editors.plan.md`](applied-filter-editors.plan.md)
