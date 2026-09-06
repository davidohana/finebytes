# FormatEditor syntax highlighting

Status: **shipped**. Cleared from [`docs/debts.md`](../debts.md).

## Goal

Color recognized format tokens in the shared FormatEditor (not MFR7 red/blue zebra).

## UX

| State                        | Treatment                                                 |
| ---------------------------- | --------------------------------------------------------- |
| Literal text                 | Default editor foreground                                 |
| Valid token span             | Single theme accent (`FormatTokenForegroundBrush`)        |
| Failing span                 | Soft error background (`FormatTokenErrorBackgroundBrush`) |
| Valid tokens before an error | Still accent-colored                                      |

## Implementation

1. Engine: `FormatStringSyntax.TryValidate` returns prior `FormatTokenSpan`s on unknown-token / Compile failure (walk failures still empty).
1. UI: `Avalonia.AvaloniaEdit` hosts the template field; `FormatTokenColorizingTransformer` colors spans from `LastParseResult`.
1. Themes: Light/Dark brushes in [`FilterEditor.axaml`](../../Mfr.App.Ui/Themes/FilterEditor.axaml).

## Out of scope

- Nested `source=` highlight / nested FormatEditor
- Category rainbows / name-vs-args split / alternating backgrounds

## References

- F6 deferral: [`formatter-formateditor-ux.plan.md`](formatter-formateditor-ux.plan.md)
- Applied Filters backlog: [`applied-filter-editors.plan.md`](applied-filter-editors.plan.md)
