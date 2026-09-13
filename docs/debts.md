# Debts

Deferred work. Short bullets only; grow this list when skipping a feature.

## File List context menu

- View mode radios on the context menu — already on the File List menu

## FormatEditor

- Nested `FormatEditor` in token `source=` fields + nested error/caret spans (high cost; soft Source text boxes are enough for now)

## Rename List

- Show Last Rename Errors — row-level **Show Rename Error** is available; a list-wide history view is deferred
- Drag-out FileDrop to Explorer (selected rows) — cut from [rename-list-ui.plan.md](plans/rename-list-ui.plan.md) 14f; inbound drop + reorder already ship

## Avalonia / tooling

- Avalonia **12** upgrade (breaking-change pass: packages, APIs, headless/UI tests) — still on 11.3.x
- Official Avalonia **Hot Reload** (Plus license + Avalonia 12) — or evaluate community HotAvalonia on 11 if AXAML iteration pain justifies it

## Options dialog (deferred from v1)

- Explorer shell integrate — see [options-dialog.plan.md](plans/options-dialog.plan.md) (double-click-to-add and confirmation prompts shipped in [options-confirmations-and-double-click.plan.md](plans/options-confirmations-and-double-click.plan.md); Undo & Log retention shipped in [undo.plan.md](plans/undo.plan.md) P4)
