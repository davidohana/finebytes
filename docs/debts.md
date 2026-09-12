# Debts

Deferred work. Short bullets only; grow this list when skipping a feature.

## File List context menu

- Cut / Copy / Paste / Delete — Recycle Bin (`IFileOperation`) and clipboard file drops
- View mode radios on the context menu — already on the File List menu

## FormatEditor

- Nested `FormatEditor` in token `source=` fields + nested error/caret spans (high cost; soft Source text boxes are enough for now)

## Rename List

- Show Last Rename Errors — row-level **Show Rename Error** is available; a list-wide history view is deferred

## Avalonia / tooling

- Avalonia **12** upgrade (breaking-change pass: packages, APIs, headless/UI tests) — still on 11.3.x
- Official Avalonia **Hot Reload** (Plus license + Avalonia 12) — or evaluate community HotAvalonia on 11 if AXAML iteration pain justifies it

## Status bar

- Undo Last → sticky status outcome when Undo ships (deferred with Undo itself)

## Options dialog (deferred from v1)

- Explorer shell integrate, Undo & Log retention tab — see [options-dialog.plan.md](plans/options-dialog.plan.md) (double-click-to-add and confirmation prompts shipped in [options-confirmations-and-double-click.plan.md](plans/options-confirmations-and-double-click.plan.md))
