---
title: A/B side column remap
description: In-place Before/After projected key remap instead of DataGrid column rebuild.
status: done
---

# A/B side column remap (perf)

Parent: [rename-list-preview-columns-toggle.plan.md](rename-list-preview-columns-toggle.plan.md) (shipped).

## Decisions (locked)

- In-place remap on `AbSide` only when the same originals are in display order.
- Full rebuild when the column set changes.
- No semantic-overlay cache, dual column sets, or hydrate changes.

## Approach

- Cells read `GetFieldKey(column)` live on `FieldDisplayRevision`.
- `ProjectedColumns` handler tries `_TryRemapAbSideProjectedKeys()` then falls back to `_RebuildColumns()`.
- Remap rebinds headers, refreshes min widths, bumps field display; `OnAbSideChanged` also bumps display after notify.
