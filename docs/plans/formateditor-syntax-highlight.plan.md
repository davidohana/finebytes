# FormatEditor syntax highlighting

Status: **shipped**. Cleared from [`docs/debts.md`](../debts.md).

## Goal

Mark recognized format tokens in the shared FormatEditor (soft zebra chips; not MFR7 red/blue).

## UX

| State                        | Treatment                                                                                                  |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Literal text                 | Default editor colors                                                                                      |
| Valid token span (`<…>`)     | Soft zebra chip (`FormatTokenBackgroundBrush` / `FormatTokenAltBackgroundBrush`) — keeps selection visible |
| Written token name           | Accent foreground (`FormatTokenNameForegroundBrush`)                                                       |
| Delimiters / args            | Default foreground                                                                                         |
| Failing span                 | Soft error background (`FormatTokenErrorBackgroundBrush`)                                                  |
| Valid tokens before an error | Still washed + name accent                                                                                 |

## Implementation

1. Engine: `FormatStringSyntax.TryValidate` returns prior `FormatTokenSpan`s (with `WrittenName` from the scan) on unknown-token / Compile failure (walk failures still empty).
1. UI: `Avalonia.AvaloniaEdit` hosts the template field; name foreground via `FormatTokenColorizingTransformer`; yellow/mint (light) or muted (dark) zebra washes via `FormatTokenBackgroundRenderer` on `KnownLayer.Background` (under selection), alternating by token index.
1. Themes: Light/Dark brushes in [`FilterEditor.axaml`](../../Mfr.App.Ui/Themes/FilterEditor.axaml).

## Out of scope

- Nested `source=` highlight / nested FormatEditor
- Colored delimiters / args foregrounds
- Category rainbows

## References

- F6 deferral: [`formatter-formateditor-ux.plan.md`](formatter-formateditor-ux.plan.md)
- Applied Filters backlog: [`applied-filter-editors.plan.md`](applied-filter-editors.plan.md)
