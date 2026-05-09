---
title: MFR folder layering
description: Current layers and allowed project dependencies.
---

# MFR layering (current)

## Projects by layer

| Layer | Project |
|---|---|
| L6 Host | [`Mfr/`](../Mfr) |
| L5 App | [`Mfr.App.Cli/`](../Mfr.App.Cli) |
| L4 Application | [`Mfr.Core/`](../Mfr.Core) |
| L3 Domain rules | [`Mfr.Filters/`](../Mfr.Filters) |
| L2 Tagged media I/O | [`Mfr.Metadata/`](../Mfr.Metadata) |
| L1 Domain model | [`Mfr.Models/`](../Mfr.Models) |
| L0 Shared utilities | [`Mfr.Utils/`](../Mfr.Utils) |

Supporting:

- Tests: [`Mfr.Tests/`](../Mfr.Tests) (guardrails + regression, TagLib-backed `AudioTagPersistence` integration in `Metadata/`; refs host `Mfr` only per architecture test)
- UI placeholder: [`Mfr.App.Ui/`](../Mfr.App.Ui)

## Allowed dependencies

**Rule:** A project may reference **any** project in a **strictly lower** layer (all layers below), not only the adjacent one. No references upward or sideways within the same layer.

Illustrative spine (typical flow, not exhaustive):

`Mfr (host) -> Mfr.App.Cli -> Mfr.Core -> Mfr.Filters -> Mfr.Models -> Mfr.Utils`

`Mfr.Metadata` bridges TagLib Sharp to canonical tag records in `Mfr.Models` (for example overlay types used by previews). `Mfr.Core` references it for future commit hooks; formatter tokens in `Mfr.Filters` may reference it in later phases following the layer map enforced in `ProjectReferenceArchitectureTests`.

## Enforcement

- Enforced by `.csproj` project references.
- Keep architecture tests in `Mfr.Tests` for guardrails.
