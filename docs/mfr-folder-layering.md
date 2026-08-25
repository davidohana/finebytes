---
title: MFR folder layering
description: Current layers and allowed project dependencies.
---

# MFR layering (current)

## Projects by layer

| Layer               | Project                                                          |
| ------------------- | ---------------------------------------------------------------- |
| L5 Entry            | [`Mfr.App.Cli/`](../Mfr.App.Cli), [`Mfr.App.Ui/`](../Mfr.App.Ui) |
| L4 Engine           | [`Mfr.Engine/`](../Mfr.Engine)                                   |
| L3 Domain rules     | [`Mfr.Filters/`](../Mfr.Filters)                                 |
| L2 Tagged media I/O | [`Mfr.Metadata/`](../Mfr.Metadata)                               |
| L1 Domain model     | [`Mfr.Models/`](../Mfr.Models)                                   |
| L0 Shared utilities | [`Mfr.Utils/`](../Mfr.Utils)                                     |

Supporting:

- Tests: [`Mfr.Tests/`](../Mfr.Tests) (guardrails + regression, TagLib-backed `AudioTagPersistence` integration in `Metadata/`; refs entry points only per architecture test)
- UI: [`Mfr.App.Ui/`](../Mfr.App.Ui) (Avalonia 11 + CommunityToolkit.Mvvm desktop shell; `just run-ui`)

## Allowed dependencies

**Rule:** A project may reference **any** project in a **strictly lower** layer (all layers below), not only the adjacent one. No references upward or sideways within the same layer.

Illustrative spine (typical flow, not exhaustive):

`Mfr.App.Cli -> Mfr.Engine -> Mfr.Filters -> Mfr.Models -> Mfr.Utils`

`Mfr.App.Ui -> Mfr.Engine -> ...` (same lower layers as CLI)

`Mfr.Metadata` bridges TagLib Sharp and MetadataExtractor to canonical records in `Mfr.Models` (overlay types, semantic projection/merge, and field get/set live in L1; TagLib and MetadataExtractor read/write/detect stay in L2). `Mfr.Engine` references Metadata for commit Apply; filters use Models for overlay edits and Metadata only for lazy load.

## Enforcement

- Enforced by `.csproj` project references.
- Keep architecture tests in `Mfr.Tests` for guardrails.

## UI project internal layering

Inside [`Mfr.App.Ui/`](../Mfr.App.Ui), keep dependencies one-way:

`Views → ViewModels → Services → Engine / Models / Utils`

Do not import `ViewModels` (or Views) from `Services`. Session restore/save passes snapshot DTOs across that boundary (`FileListSessionSnapshot`); apply/capture lives on the File List view model. Guarded by `UiServicesLayerArchitectureTests`.
