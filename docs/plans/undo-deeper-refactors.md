---
name: Undo deeper refactors
overview: End-of-run deeper refactors after Undo P1–P5. All six items below are done.
---

# Undo — deeper refactors

Sibling to [undo.plan.md](undo.plan.md). Implemented after HEAD `9b70b0d5` (missing-path Undo status).

| #   | Item                                                                                        | Status   |
| --- | ------------------------------------------------------------------------------------------- | -------- |
| 1   | Shared `RenamePropertyNames` constants (Builder / OldValueApplier / `IsUndoable`)           | **Done** |
| 2   | Shared `NewestFilesPruner` for `LogPaths` + `RenameLogStore` prune                          | **Done** |
| 3   | Shared GO/Undo status primary + `_CombineStatusParts` (distinct error/not-loaded copy kept) | **Done** |
| 4   | Engine audio-tag undo round-trip on `metaflac.flac`                                         | **Done** |
| 5   | `RenameLogDisplay` in UI for details + list titles (removed from Models/Engine)             | **Done** |
| 6   | `RenameLogConfig.DefaultLimit` shared with Options `DefaultLimitedCount`                    | **Done** |
