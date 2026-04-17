---
title: MFR folder layering
description: Current layers and allowed project dependencies.
---

# MFR layering (current)

## Projects by layer

| Layer | Project |
|---|---|
| L4 App | [`Mfr.App.Cli/`](../Mfr.App.Cli) |
| L3 Application | [`Mfr.Core/`](../Mfr.Core) |
| L2 Domain rules | [`Mfr.Filters/`](../Mfr.Filters) |
| L1 Domain model | [`Mfr.Models/`](../Mfr.Models) |
| L0 Shared utilities | [`Mfr.Utils/`](../Mfr.Utils) |

Supporting:

- Host: [`Mfr/`](../Mfr)
- Tests: [`Mfr.Tests/`](../Mfr.Tests)
- UI placeholder: [`Mfr.App.Ui/`](../Mfr.App.Ui)

## Allowed dependencies

Primary direction:

`Mfr (host) -> Mfr.App.Cli -> Mfr.Core -> Mfr.Filters -> Mfr.Models -> Mfr.Utils`

## Enforcement

- Enforced by `.csproj` project references.
- Keep architecture tests in `Mfr.Tests` for guardrails.
