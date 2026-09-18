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

- Tests: [`Mfr.Tests/`](../Mfr.Tests) (guardrails + regression, TagLib-backed `AudioTagPersistence` integration in `Metadata/`; UI tests under `Ui/<Pane>/` with matching namespaces, plus `Ui/Services/<Slice>/` for non-View service coverage such as `FileList`; refs entry points only per architecture test)
- UI: [`Mfr.App.Ui/`](../Mfr.App.Ui) (Avalonia 12 + CommunityToolkit.Mvvm desktop shell; `just run-ui`)

## Allowed dependencies

**Rule:** A project may reference **any** project in a **strictly lower** layer (all layers below), not only the adjacent one. No references upward or sideways within the same layer.

Illustrative spine (typical flow, not exhaustive):

`Mfr.App.Cli -> Mfr.Engine -> Mfr.Filters -> Mfr.Models -> Mfr.Utils`

`Mfr.App.Ui -> Mfr.Engine -> ...` (same lower layers as CLI; UI also references `Mfr.Filters` directly for editors)

`Mfr.Metadata` bridges TagLib Sharp and MetadataExtractor to canonical records in `Mfr.Models` (overlay types, semantic projection/merge, and field get/set live in L1; TagLib and MetadataExtractor read/write/detect stay in L2). `Mfr.Engine` references Metadata for commit Apply; filters use Models for overlay edits and Metadata only for lazy load.

### Prefs ownership (Engine store, Models shape)

- **L4 Engine** owns process-wide prefs I/O: [`ConfigStore`](../Mfr.Engine/Config/ConfigStore.cs) and [`ConfirmationPolicy`](../Mfr.Engine/Config/ConfirmationPolicy.cs) under `Mfr.Engine.Config` (load/save/delete, soft-load dialect, CLI `--set`). Guarded by `ConfigStoreOwnershipArchitectureTests`.
- **L1 Models** owns prefs **shape** only: `OptionsConfig`, `LogConfig`, `RenameLogConfig`, session DTOs (`FileListPrefs`, …), `ConfirmationKind` under `Mfr.Models.Config`.
- Reset Configuration stays [`PersistedConfigurationReset`](../Mfr.Engine/Config/PersistedConfigurationReset.cs) (calls `ConfigStore.DeleteDefaultFile`).

### Models friend assemblies

`Mfr.Models` uses `InternalsVisibleTo` for **Engine**, **Filters**, and **Tests** so row mutation helpers (`RenameItem` load flags / overlay setters), field-catalog internals, and `BaseFilter.Setup`/`Apply` stay non-public to UI. Do **not** publicize those for App.Ui. Metadata uses only public Models DTOs (no Models friend access). Guarded by `ModelsFriendAssemblyArchitectureTests`.

## Enforcement

- Enforced by `.csproj` project references.
- Keep architecture tests in `Mfr.Tests` for guardrails (`ProjectReferenceArchitectureTests`, `PackageOwnershipArchitectureTests`, `ConfigStoreOwnershipArchitectureTests`, `ModelsFriendAssemblyArchitectureTests`, UI layer tests).

## UI project internal layering

Inside [`Mfr.App.Ui/`](../Mfr.App.Ui), keep dependencies one-way:

`Views → ViewModels → Services`

- **ViewModels** use Engine + Filters + Models (+ Utils as needed) for orchestration (Rename List, Filter Chain, Options, presets). ViewModels must not import Views (guarded by `UiViewModelsLayerArchitectureTests`).
- **Services** may use Engine.Config / Models (session DTOs, File List options) and must **not** import Filters or Metadata. Services must not import ViewModels or Views (guarded by `UiServicesLayerArchitectureTests`).
- **Views** bind ViewModels and may call Services for thin platform / session glue (see table below). Supporting folders (`Diagnostics`, `Input`, `Converters`, `Threading`) are not separate layers.

Do not import `ViewModels` (or Views) from `Services`. Session restore/save passes Models session DTOs (`FileListPrefs`, `RenameListPrefs`) across that boundary; apply/capture lives on the pane view models.

### Views → Services (intentional glue)

Views may call into `Services` for thin platform / session glue. That edge is **allowed** and is **not** gated by a blanket architecture forbid — forcing every call through a ViewModel wrapper would add indirection without clearer ownership. Do not add a Views↛Services forbid test without an explicit allowlist of the cases below (or a deliberate redesign).

Current code-behind / AXAML consumers (verify with `using Mfr.App.Ui.Services` under `Views/`):

| Site                                       | Service surface                                       | Why                                                                               |
| ------------------------------------------ | ----------------------------------------------------- | --------------------------------------------------------------------------------- |
| `Views/MainWindow/MainWindow`              | `Services.Session` (`MainWindowPaneGrids`)            | Session splitter restore/capture needs named pane grids from the window           |
| `Views/MainWindow/MainWindow`              | `Services.Help` (`HelpHost`)                          | Missing-help dialog text lists the app-local `help/` folder                       |
| Resizable dialogs under `Views/`           | `Services.Session` (`DialogSession.Attach`)           | Remembered dialog geometry needs the window as visual root                        |
| `PathMoverFilterEditorView`                | `Services.FolderPicker`                               | Folder picker needs a visual root; VM gets an injected async delegate             |
| `RenameListView`                           | `Services.FileSavePicker`                             | Export save dialog needs a visual root; VM gets an injected async delegate        |
| `FileListView`                             | `Services.RenameList` (`RenameListAddSourceResolver`) | DnD / Add-Selected path validation shared with Rename List add                    |
| `FileListAddressBarView` (+ AXAML `xmlns`) | `Services.FileList` (`PathBreadcrumbSegment`)         | Breadcrumb overflow UI binds the same segment type as the File List service model |

`Views/DragAndDrop/` helpers do not currently import Services; keep new DnD format / path helpers in Views or Services deliberately, and extend this table if a View starts using Services there. Rename List–specific drag format constants live under `Views/RenameList/` (`RenameListDragFormats`); crash UI lives under `Views/Crash/` + `ViewModels/Crash/` while `Diagnostics/UiCrashHandler` stays outside Views (covered by `Mfr.Tests/Ui/Crash/`). File List service tests live under `Mfr.Tests/Ui/Services/FileList/`.
