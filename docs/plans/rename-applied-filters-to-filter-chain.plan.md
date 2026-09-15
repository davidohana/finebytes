---
name: Rename Applied Filters → Filter Chain
overview: Product rename Applied Filters → Filter Chain. Full rename AppliedFilters* → FilterChain*. Keep model Mfr.Models.Filters.FilterChain. Keep Available Filters / FilterPalette. No JSON migration for ConfirmationKind.
todos:
  - id: p1
    content: "P1: Move UI surface + core types (folders/ns/files/styles/assets); qualify model FilterChain when shadowed"
    status: completed
  - id: p2
    content: "P2: Wire MainWindow, ConfirmationKind, AppTips, IndicesDragPayload, and other App.Ui call sites"
    status: pending
  - id: p3
    content: "P3: Rename tests tree + docs; format/lint; verify build"
    status: pending
isProject: false
---

# Rename Applied Filters → Filter Chain

## Locked decisions

- **Product name:** Filter Chain
- **Full rename:** `AppliedFilters*` → `FilterChain*`
- **Keep model:** `Mfr.Models.Filters.FilterChain` (and `FilterChainStep`)
- **Keep:** Available Filters / `FilterPalette`
- **No JSON migration** for `ConfirmationKind` (persist one current schema; soft-load prefs fall back to defaults)

## Naming map

| From | To |
| ---- | -- |
| folders / namespaces `AppliedFilters` | `FilterChain` |
| `AppliedFiltersViewModel` / `AppliedFiltersView` | `FilterChainViewModel` / `FilterChainView` |
| `AppliedFilterStepViewModel` | `FilterChainStepViewModel` |
| `AppliedFiltersUiHooks` | `FilterChainUiHooks` |
| `Assets/AppliedFilters` | `Assets/FilterChain` |
| styles `applied-filters` / `applied-filter-*` | `filter-chain` / `filter-chain-*` |
| control `AppliedFiltersList` | `FilterChainList` |

Inside namespace `FilterChain`, qualify the model as `Mfr.Models.Filters.FilterChain` or use a type alias when the namespace shadows the type name.

## Phases

### P1 — Move UI surface + core types

- Write this plan under `docs/plans/`.
- `git mv` trees:
  - `Mfr.App.Ui/ViewModels/AppliedFilters` → `FilterChain`
  - `Mfr.App.Ui/Views/AppliedFilters` → `FilterChain`
  - `Mfr.App.Ui/Assets/AppliedFilters` → `FilterChain`
- Rename primary files (`*ViewModel` / `*View` / step / hooks). Keep `FilterOptionsDialog*`, `FilterTarget*`, etc. file names; update namespaces only.
- In moved files: namespaces, type names, `x:Class`, `x:Name` (`AppliedFiltersList` → `FilterChainList`), `ElementName` refs, style classes, asset paths, xmldocs about this pane.
- Do **not** update MainWindow, ConfirmationKind, tests tree, AppTips (except refs only from moved files), IndicesDragPayload.

### P2 — App.Ui wiring

- MainWindow, ConfirmationKind, AppTips, IndicesDragPayload, and remaining App.Ui call sites outside the moved trees.

### P3 — Tests + docs

- Rename tests tree, docs references, format/lint, verify build.
