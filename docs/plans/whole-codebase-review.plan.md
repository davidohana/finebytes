---
name: Whole codebase review
overview: "Phased whole-repo review: Phase 0–4 done. Next: Phase 5 Session/Config."
todos:
  - id: phase-0
    content: "Phase 0: Architecture/layering gate + plan file bootstrap"
    status: completed
  - id: phase-1
    content: "Phase 1: Utils + Models core — mfr-code-review + autofix"
    status: completed
  - id: phase-2
    content: "Phase 2: Metadata + Tags — mfr-code-review + autofix"
    status: completed
  - id: phase-3
    content: "Phase 3: Filters pipeline (Formatting last) — mfr-code-review + autofix"
    status: completed
  - id: phase-4
    content: "Phase 4: Engine Preview + Commit risk gate (+ bugbot)"
    status: completed
  - id: phase-5
    content: "Phase 5: Session/Config/Presets/Reset — mfr-code-review + autofix"
    status: pending
  - id: phase-6
    content: "Phase 6: File List services + UI — mfr-code-review + autofix"
    status: pending
  - id: phase-7
    content: "Phase 7: Rename List UI + Applied Filters/Palette — mfr-code-review + autofix"
    status: pending
  - id: phase-8
    content: "Phase 8: Format Editor + FilterEditors (respect f5/f6 prior art)"
    status: pending
  - id: phase-9
    content: "Phase 9: CLI + architecture tests + backlog/debts sweep"
    status: pending
isProject: false
---

# Whole codebase review (phased)

## Decisions locked

- **Order:** architecture / layering first, then full [`mfr-code-review`](../../.agents/skills/mfr-code-review/SKILL.md) per slice.
- **Per phase:** apply high-confidence fixes in-pass; report remaining findings + cost-to-value-ranked deeper refactors (skill default).
- **Artifact:** this file — phase status + rolling **Deeper refactors backlog**. Do not re-open “already done” items in [`f5-…`](f5-attributes-audio-editors-review-deeper-refactors.md) / [`f6-…`](f6-formateditor-deep-refactors.md).

## Method (every content phase)

1. Scope paths for the phase (project folders + matching `Mfr.Tests/…`).
1. **Architecture check** (lightweight after Phase 0): deps vs [`docs/mfr-folder-layering.md`](../mfr-folder-layering.md); UI `Views → ViewModels → Services`; no second resolver in UI; policy in domain.
1. Run **mfr-code-review** lenses; apply high-confidence fixes; run affected tests + format touched files.
1. Append report section here; promote structural items into the backlog ranked by cost-to-value.
1. Subagents only when skill triggers: `explore` for cross-file twins; `bugbot` on Engine Commit/Preview; `security-review` on path/UNC/session deserialize surfaces.

```mermaid
flowchart TD
  p0[Phase0_Architecture]
  p1[Phase1_Utils_Models]
  p2[Phase2_Metadata_Tags]
  p3[Phase3_Filters]
  p4[Phase4_Engine_Preview_Commit]
  p5[Phase5_Session_Config]
  p6[Phase6_FileList]
  p7[Phase7_RenameList_AppliedFilters]
  p8[Phase8_FormatEditor_FilterEditors]
  p9[Phase9_CLI_ArchTests_Sweep]
  p0 --> p1 --> p2 --> p3 --> p4 --> p5 --> p6 --> p7 --> p8 --> p9
```

## Status

| Phase                               | Status   | Notes                                                              |
| ----------------------------------- | -------- | ------------------------------------------------------------------ |
| **0** Architecture / layering       | **done** | Project graph healthy; Services→Views fixed; arch tests tightened  |
| **1** Utils + Models                | **done** | Numeric parity + Windows path chars; TextValues deleted            |
| **2** Metadata + Tags               | **done** | Semantic 0→null; row Compare owners; detector path guard           |
| **3** Filters                       | **done** | Setup caches + exhaustiveness; Formatting tokens OK                |
| **4** Engine Preview/Commit         | **done** | failFast stash; rebase PreviewOk; DirectoryPath ordinal; bugbot OK |
| **5** Session/Config                | pending  |                                                                    |
| **6** File List                     | pending  |                                                                    |
| **7** Rename List + Applied Filters | pending  |                                                                    |
| **8** Format Editor + FilterEditors | pending  |                                                                    |
| **9** CLI + arch tests + sweep      | pending  |                                                                    |

______________________________________________________________________

## Phase 0 — Architecture / layering gate (done)

**Goal:** one current picture of allowed deps and known violations before deep cleanup.

### Layer health

| Check                                                                                     | Verdict                                                                                                                                                  |
| ----------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `.csproj` ProjectReference graph vs [`mfr-folder-layering.md`](../mfr-folder-layering.md) | **Healthy** — L5→L0 strict; Tests → entry points only                                                                                                    |
| Engine / Filters → Avalonia / App.Ui                                                      | **Clean**                                                                                                                                                |
| Models → TagLib / MetadataExtractor / Metadata / Filters / Engine                         | **Clean**                                                                                                                                                |
| TagLib / MetadataExtractor package ownership                                              | **Clean** — packages only in `Mfr.Metadata` (+ Tests fixtures)                                                                                           |
| Filters TagLib I/O                                                                        | **Clean** — lazy load via Metadata readers only                                                                                                          |
| UI Services → ViewModels                                                                  | **Clean**                                                                                                                                                |
| UI Services → Views                                                                       | **Was broken; fixed**                                                                                                                                    |
| Design doc                                                                                | [`magic-file-renamer-design.md`](../magic-file-renamer-design.md) is an empty stub — use layering + domain docs as truth; **do not invent a design doc** |

**Allowed spine (confirmed):**

`App.Ui / App.Cli → Engine → Filters → Metadata → Models → Utils`

Ui also refs Filters directly (catalog/editors). Cli does not. Engine + Filters both ref Metadata as documented.

### Applied (high confidence)

1. **Services must not take `MainWindow`:** introduced `MainWindowPaneGrids` DTO; `SplitterSession` / `UiSessionPersistence` take Avalonia `Window` + pane grids; `MainWindow.GetPaneGrids()` builds the handle from the view.
1. **Architecture test:** `UiServicesLayerArchitectureTests` now forbids `Mfr.App.Ui.Views` as well as ViewModels.
1. **Doc drift:** `ProjectReferenceArchitectureTests` remarks updated to match current L0–L5 labels (map values were already correct).

### Deferred (later phases / backlog)

| Item                                                                      | Why defer                                                  |
| ------------------------------------------------------------------------- | ---------------------------------------------------------- |
| Models JSON/FS I/O (`ConfigStore`, `SessionStore`, `RenameResultSummary`) | Ownership smell, not a layer violation; revisit in Phase 5 |
| `RenameListFieldDisplay` directory scan for folder file-count             | Domain field resolve; Phase 7 if touched                   |
| `JpegExifThumbnailReader` hand-rolled EXIF in UI Services                 | No ME package leak; optional L2 move — Phase 6             |
| Fuller UI DAG tests (Views↛Services shortcuts, VM↛Views)                  | Nice-to-have; Phase 9 if still wanted                      |
| Forbidden-using / package-ownership arch tests for lower layers           | csproj + spot-check enough for now; Phase 9                |

### Phase 0 exit

Layer picture is current; one real UI-internal leak fixed and guarded. Ready for Phase 1 (Utils + Models core).

______________________________________________________________________

## Phase 1 — L0 Utils + L1 Models core (done)

**Scope:** `Mfr.Utils/`, `Mfr.Models/` (Rename, Filters targets/chain, RenameList catalog), matching `Mfr.Tests/Models/` + `Utils/`. Explore subagent used for cross-file twins.

**Verdict:** Core is coherent — one Rename List field schema, FilterChain/`_Setup` contracts sound, no dual persist schemas. Applied drift/dead-code fixes; leftover items are naming collisions and intentional I/O ownership smells.

### Applied (high confidence)

1. **Deleted** dead `TextValues` twin of `DelimitedText` (zero production callers).
1. **Unified File Name Numeric** via `FileNameNumericValue.Extract` (MFR7 `[0-9]{1,10}` + `long.Parse`) — Rename List field + formatter token share one owner (token previously had no 10-digit cap).
1. **Windows path chars:** `WindowsFileNameChars.ContainsInvalidPath`; `FileMetaPreviewExtensions` no longer uses host `Path.GetInvalidPathChars` (Linux CI ↔ Windows product parity).
1. **Docs:** `SessionState.Version` no longer says “migrations”; `PathRelations.SameOnDisk` vs `IsSamePath` trailing-sep difference clarified; `FilterTargetText.TryGet` documents false ≠ empty field.
1. **File rename:** `ContractAssert.cs` → `Contracts.cs` (`Check` / `Require`).
1. **Tests:** `FileNameNumericValueTests`, `WindowsFileNameCharsTests`, `PathRelations` IsSamePath cases, `FileMeta` path-write / illegal-char cases, token 11-digit cap.

### Correctness (found, not changed)

| Item                                                 | Notes                                                                                                                                                                           |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| “Parent Folder” label collision                      | Apply-To ancestor L1 = segment name; Rename List column = absolute `DirectoryPath`; “Parent Directory” = absolute dir target — MFR7-facing; don’t force one map without UI pass |
| Prefix/extension writes skip Windows name validation | Ancestor segments validate; name parts rely on Cleaner / commit — product choice                                                                                                |
| `FileMeta.Clone` shares Media/Image/Exif refs        | Safe while snapshots stay immutable                                                                                                                                             |

### Deeper refactors (promoted to backlog)

See backlog below (SameOnDisk collapse, FirstDelimitedSegment, folder-file-count I/O, session sort DTO, label maps).

### Phase 1 exit

Utils/Models numeric + path policy aligned; dead twin removed. Ready for Phase 2 (Metadata + Tags).

______________________________________________________________________

## Phase 2 — L2 Metadata + Tags (done)

**Scope:** `Mfr.Metadata/`, `Mfr.Models/Tags/`, docs `audio-tag-model.md` / `image-metadata-model.md`, `Mfr.Tests/Metadata/` + `Mfr.Tests/Models/Tags/`. Explore subagent used for cross-file twins ([Metadata Tags twins](4089d359-8272-4d95-8c6a-4fcf3322505e)).

**Verdict:** Layering matches the design — TagLib/ME stay in Metadata; overlay/semantic/merge/policy stay in Models; field-patch Apply never dual-writes `file.Tag`. Image/EXIF lazy map is coherent and allowlist-gated. Applied numeric clear parity, shared row/frame comparers (killing `\0`-Join drift), detector path hardening, and empty-block prune on ASF/Apple reads. Leftovers are known-key catalogs for Ape/Riff (Xiph already has `XiphKnownKeys`) and optional TagLib open helper.

### Applied (high confidence)

1. **`SemanticFields._ParseNullableUInt`:** `"0"` → `null` (aligned with Id3v1 field IO, projection `_ParseUInt`, and audio-tag-model “never store 0”).
1. **`Id3v1TagData.IsEmpty`:** one owner; SemanticMerge + BlockFieldIo prune through it.
1. **Shared `Compare` on row/frame types:** `TextFieldRow`, `Id3v2ModeledFrame`, `AppleAtomRow`, `AsfDescriptorRow`, `RiffInfoFieldRow` — Metadata TagFields + SemanticMerge + BlockFieldIo (removed divergent `Join('\0')` sorts).
1. **`AudioTagContainerDetector.Detect`:** `RequireExistingRegularFile` (parity with Read/Apply/image opens).
1. **`AsfTagFields.Read`:** empty descriptor list → `null` block (same as other readers).
1. **`AppleTagFields.Read`:** `DelimitedText.TrimNonEmpty` + skip empty value rows.
1. **Exhaustiveness:** `MediaPropertiesReader` MPEG version + `AudioTagContainerPolicy` container switches → `UnreachableException`.
1. **Docs:** `TagBlocksStructurallyEquals` no longer falsely inheritdocs `Equals`; clarifies same contract / named call-site API.
1. **Tests:** `SemanticFieldsTests` zero-clear theory; detector missing-file ArgumentException.

### Correctness (found, not changed)

| Item                                                    | Notes                                                                                                                      |
| ------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `SemanticFields` year/BPM range                         | No 1–9999 / 1–65535 clamp — `AudioTagSetterFilter` owns product ranges; direct BlockFieldIo Id3v1 year still maxes at 9999 |
| `TagBlocksStructurallyEquals` vs `Equals`               | Same body; Engine prefers the long name for “blocks only” clarity — optional rename later (backlog)                        |
| Live Id3v1 empty (`Id3v1TagFields._IsEffectivelyEmpty`) | TagLib sentinels (`Year == 0`, …) — keep separate from `Id3v1TagData.IsEmpty`                                              |
| Apple binary atoms / freeform catalog                   | Intentionally unmodeled; BPM/track/disc text gaps documented in merge                                                      |
| Image ME allowlist vs TagLib photo tokens               | Separate caches by design (`image-*` / `exif-*` vs `media-photo-*`)                                                        |

### Deeper refactors (promoted to backlog)

See backlog items 17–20 below.

### Phase 2 exit

Metadata I/O + Tags domain hygiene closed; numeric clear and sort/compare ownership fixed. Ready for Phase 5 (Session/Config) — Phases 3–4 already done.

______________________________________________________________________

## Phase 3 — L3 Filters pipeline (done)

**Scope:** `Mfr.Filters/` by group (Formatting tokens/compiler last), `FilterCatalog`, `BaseFilter` setup caches; matching `Mfr.Tests/Models/Filters/`. Explore subagent used for cross-file twins.

**Verdict:** Pipeline is sound — palette reflection + `PresetJsonOptions` stay aligned via existing catalog/polymorphism tests; `_Setup` caches that existed were already unconditional. Applied local setup-cache moves, exhaustiveness, CharacterRun/KISS cleanup, and Counter auto-pad unify. Leftovers are structural (regex compile per replace, sentence-casing twins, ConfigStore line-length, dual JSON registration).

### Applied (high confidence)

1. **Setup caches** for char-set options that rebuilt `HashSet` every transform: `CleanerFilter`, `SpaceAfterFilter`, `SpaceAroundFilter`, `CapitalizeAfterFilter` (unconditional assign / clear-to-null for `with`).
1. **`CharacterRunHelpers`:** regex → linear collapse (Shrink Spaces / Shrink Duplicate Characters).
1. **`AudioTagSetterFilter`:** early-return when no semantic fields configured (skip `EnsureTagLibLoaded` / merge).
1. **Exhaustive switches:** `LettersCaseFilter`, `AttributesSetterFilter`, Media/Image/Mpeg property formatters → `UnreachableException` (no silent fall-through).
1. **Dead `partial`** on `FormatterFilter`; non-nullable compiled `Formatter` fields on Formatter / Inserter / NameList / ReplaceList (drop `Check.NotNull` after setup).
1. **Counter automatic pad-width:** one owner `CounterPadding.ResolveAutomaticPadWidth` — filter and `<counter>` both throw when list counts are missing (was silent no-pad on filter). Follow-up from [Filters cross-file twins](3a7748bf-dc7c-4ea3-9e19-c836c70a17e6).
1. **Tests:** Cleaner + SpaceAfter `with`-clear; CounterFilter missing-count throws; filter suite green.

### Correctness (found, not changed)

| Item                                             | Notes                                                                                                                                                                        |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ContainsLikelyFormatTokens` vs always-`Compile` | Inserter / AudioTag / Id3v2 use heuristic literals; Replacer / ReplaceList / Formatter / NameList / PathMover always compile (MFR7 format-string replacements). Intentional. |
| `ListEntryLength` → `ConfigStore`                | Filters read process config max line length; ownership smell → Phase 5                                                                                                       |
| `UppercaseInitialsFilter` SYSLIB1045 disable     | Documented; GeneratedRegex noise — leave                                                                                                                                     |
| Sentence-end defaults                            | Options/JSON/`RenameItem` default `".!?"`; add-to-list `"-.!"` (MFR7). Documented in filter docs — keep                                                                      |
| LettersCase vs CasingList sentence-initial       | Divergent letter/separator rules — backlog #7                                                                                                                                |

### Deeper refactors (promoted to backlog)

See backlog items 6–12 below.

### Phase 3 exit

Filters setup/transform hygiene improved; Formatting registry/compiler coherent. Ready for Phase 4 (Engine Preview/Commit) once Phase 2 is done or explicitly skipped again.

______________________________________________________________________

## Phase 4 — L4 Engine Preview + Commit (done)

**Scope:** `Mfr.Engine/Preview/`, `Commit/`, RenameList Preview/Commit/plan plumbing (not unfinished rename-list UI 14b–16; Presets/Config deferred to Phase 5). Matching `Mfr.Tests/Engine/`. Explore twins + bugbot on uncommitted fixes.

**Verdict:** Preview → rebase → conflict → plan → execute spine is sound and single-owned; UI does not re-plan. Applied fail-fast on stash failure, PreviewOk-only rebase ancestors, ordinal DirectoryPath change rows, and dead-API/doc cleanup. Leftovers are structural twins (ancestor rewrite loops, vacate graph, path-equality naming) and progress-phase labeling.

### Applied (high confidence)

1. **`CommitExecutor` fail-fast on stash failure:** stash errors now set `stopped` when `failFast` (previously only finalize failures did) so cycle partners are not attempted against a path that never vacated.
1. **`RenamePreviewFolderRebaser`:** folder ancestors from `PreviewFolderPathChanges.Collect` restricted to `PreviewOk` (aligned with conflict/planner scoping).
1. **`RenamePropertyChangeBuilder` DirectoryPath:** `OrdinalIgnoreCase` → `Ordinal` so case-only directory renames appear in change rows (matches `IsPreviewPathUnchanged` / `HasPreviewChanges`).
1. **Deleted** unused `RenameItem.IsPreviewPathSameOnDisk` (dead twin of `PathRelations.SameOnDisk` / `DiffersOnlyInCase`).
1. **Docs:** stash is cycle-only (not case-only); case-only commit uses direct `File`/`Directory.Move`; folder-child test summaries updated.
1. **Tests:** stash fail-fast cycle skip; PreviewError folder not used as rebase ancestor; DirectoryPath case-only emits row. Engine suite green (231). Bugbot on uncommitted Engine delta: no findings.

### Correctness (found, not changed)

| Item                                                        | Notes                                                                                              |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Preview cancel still plans partial list                     | Documented on `RenameList.Preview`; intentional                                                    |
| Conflict `Exists` not re-checked at commit                  | TOCTOU by design; FS errors become CommitError                                                     |
| `failFast: false` after stash failure                       | Later cycle members may still hit FS errors (same-item finalize skipped via `Status != PreviewOk`) |
| Progress phase `LoadMetadata` used for preview filter apply | UI remaps labels via `RenameListProgressCopy`; enum docs already mention preview                   |
| Case-only commit without temp stash                         | .NET Move accepts same-path different casing on Windows — verified by folder-child tests           |

### Deeper refactors (promoted to backlog)

See backlog items 1 (updated), 13–16 below.

### Phase 4 exit

Engine Preview/Commit risk gate closed with stash fail-fast + rebase scoping fixes. Ready for Phase 5 (Session/Config).

______________________________________________________________________

## Deeper refactors backlog

Ranked cost-to-value. Do not duplicate f5/f6 “already done.”

1. **Collapse or rename `SameOnDisk` vs `IsSamePath`**
   Sites: `PathRelations`; callers in Engine / Ui (dead `IsPreviewPathSameOnDisk` removed in Phase 4)
   Target: one equality API + explicit trim overload, or keep both with names that cannot be confused
   Value: closes trailing-sep footgun
   Cost: medium churn across Engine/Ui
   Rank: medium — Phase 7 if touched; Engine callers already deliberate

1. **`FirstDelimitedSegment` → `DelimitedText` only if empty-first semantics match**
   Sites: `RenameListFieldDisplay.FirstDelimitedSegment` vs `DelimitedText.Split` (AudioTag first-segment fields)
   Target: shared first-part helper **only if** `" ; Bob"` empty-first behavior is acceptable
   Value: small dedup
   Cost: low, but behavior risk
   Rank: medium — Phase 7 if AudioTag display touched

1. **Move or lazy-gate `FormatFolderFileCount` directory scan**
   Sites: `RenameListFieldDisplay.FormatFolderFileCount`
   Target: avoid live FS on every resolve/sort paint, or document as intentional expensive field
   Value: paint/sort side effects
   Cost: medium (Engine/UI cache?)
   Rank: medium — Phase 7

1. **`SessionStateRenameListSortField` vs `RenameListSortKey`**
   Sites: session DTO vs domain sort key
   Target: one type if JSON shape can stay identical
   Value: thin dedup
   Cost: session churn
   Rank: low — Phase 5

1. **Shared Apply-To / Rename List / Picard display labels**
   Sites: `FilterTargetCatalog` vs `AudioTagRenameListFields` vs `AudioCatalogFieldMaps`
   Target: optional Models display catalog — only if labels should converge
   Value: unclear (MFR7 may want divergent labels)
   Cost: high
   Rank: low — skip unless UI pass demands it

1. **Cache compiled regex in Replacer / ReplaceList setup**
   Sites: `ReplacerMatching.ReplaceSegment` builds `new Regex` every call; filters already `_Setup`
   Target: compile pattern (+ flags) once in `_Setup`, reuse in transform (list = one regex per entry)
   Value: preview cost on large lists
   Cost: medium — mode/flags/`with` must invalidate; WholeWord wrapping
   Rank: medium — do when profiling preview or touching Replace

1. **Sentence-initial uppercasing twin**
   Sites: `LettersCaseFilter._ApplySentenceCase` vs `CasingListFilter._UppercaseSentenceInitials` (ASCII-letter vs `IsLetter` nuance)
   Target: one shared helper **only if** MFR7 parity allows unifying letter detection
   Value: one behavior for sentence starts
   Cost: medium behavior risk
   Rank: medium — Case group only if product wants one rule

1. **`AudioTagSetterFilter` PascalCase private formatter fields**
   Sites: `PerformersFormatter`, `TitleFormatter`, …
   Target: `_performersFormatter`-style names (or a field→formatter map)
   Value: naming clarity; optional map reduces ApplyCore boilerplate
   Cost: medium churn in one large file
   Rank: low–medium — rename anytime; map only if editing the filter again

1. **`ListEntryLength` / max line length off `ConfigStore`**
   Sites: `ListEntryLength`; Name/Replace/Casing list parsers
   Target: inject max length or read from a Filters-owned options type at setup (Phase 5 config pass)
   Value: clearer ownership; testability
   Cost: medium with config reshape
   Rank: medium — Phase 5

1. **Generate `PresetJsonOptions` derived types from `FilterCatalog` discovery**
   Sites: `FilterCatalog` reflection vs `PresetJsonOptions.s_BaseFilterDerivedTypes` (drift gated by `FilterCatalogTests`)
   Target: one registration owner (generate JSON list from palette types, or reverse)
   Value: closes permanent dual-list surface
   Cost: medium Engine/Filters wiring
   Rank: medium — Phase 5/9

1. **`FilterOptionsEditorFactory` completeness vs option-bearing catalog types**
   Sites: `FilterOptionsEditorFactory` switch; missing arm → null editor
   Target: architecture/UI test that every filter with options records has an editor arm (optionless filters exempt)
   Value: silent missing-editor footgun
   Cost: low–medium test + maybe factory map
   Rank: medium — Phase 8

1. **Shared 1-based string-position helper**
   Sites: `SubstringApplyScope`, `TrimBetweenFilter._GetAbsoluteIndex`, `InserterFilter._ComputeInsertIndex`, `<substr>` (`SubstringToken`)
   Target: shared helper for dialects that match; document Inserter/`substr` negative-from-end separately
   Value: one place for clamp/anchor bugs
   Cost: medium behavior risk
   Rank: medium — Phase 7/8 if ApplyScope touched

1. **Shared innermost-ancestor rewrite helper**
   Sites: `RenamePreviewFolderRebaser._RebaseItemAgainstAncestors` ↔ `CommitPlanner._ResolveActualSourcePath`
   Target: one `ApplyInnermostAncestorRewrites(path, ancestors, matchPredicate)` with call-site selectors (preview-dir vs original-fullpath)
   Value: closes compose-order / ReplaceAncestor drift
   Cost: medium — two match dialects must stay explicit
   Rank: medium — do when next touching rebase or planner

1. **Vacate policy vs path-shift / containment edges**
   Sites: `PreviewConflictDetector._WillBeVacatedByBatch` ↔ `CommitPlanner` path-shift + containment
   Target: optional shared “batch path graph” helper, or documented invariant tests that conflict vacate ≡ planner order assumptions
   Value: prevents silent preview-ok / commit-fail drift
   Cost: medium–high behavior risk
   Rank: medium — add invariant tests first if touching either side

1. **Preview progress phase naming**
   Sites: `RenameListProgressPhase.LoadMetadata` used for filter apply; `RenameListProgressCopy` remaps UI titles
   Target: add `ApplyPreview` (or rename) so engine enum matches work; update UI binding
   Value: removes papered-over phase lie
   Cost: low–medium enum + UI/tests
   Rank: medium — Phase 7 if progress dialog touched

1. **Path split + preview-field reset helpers**
   Sites: `RenameItemSnapshotBuilder` ↔ `FileMetaPreviewExtensions.SetFromAbsoluteFullPath`; `ResetState` ↔ `ClearPreview`
   Target: shared path-dissection helper; private reset-of-preview-fields on `RenameItem`
   Value: small dedup / fewer clone-reset omissions
   Cost: low–medium Models churn
   Rank: low–medium — ride along when editing add/refresh or RenameItem

1. **`ApeKnownKeys` / `RiffInfoKnownKeys` (mirror `XiphKnownKeys`)**
   Sites: `ApeTagFields._KnownKeys` + alias map; `RiffInfoTagFields._KnownKeys`; hardcoded keys in `SemanticAudioTag` / `AudioTagSemanticMerge`
   Target: Models-owned known-key lists (+ Ape alias fold) used by Metadata read and merge/projection
   Value: closes key-list drift (Xiph already has one owner)
   Cost: medium — Metadata + Models + Filter Options Apply-To if exposed
   Rank: medium — do when next touching Ape/Riff field IO

1. **TagLib open helper**
   Sites: `AudioTagPersistence` / `TagLibFileReader` / `MediaPropertiesReader` / `AudioTagContainerDetector` (`RequireExistingRegularFile` + `LocalFileAbstraction`)
   Target: internal `TagLibFileOpen.OpenExisting(path)` (or similar) in Metadata
   Value: one open/guard pattern
   Cost: low churn, Metadata-only
   Rank: medium — ride along when next editing persistence opens

1. **Collapse `TagBlocksStructurallyEquals` onto `Equals`**
   Sites: `AudioTagOverlay`; Engine `RenamePropertyChangeBuilder` uses structural name
   Target: keep `Equals` only, or obsolete the long alias
   Value: one public name
   Cost: low call-site rename
   Rank: low — optional clarity pass

1. **Wire catalog maps to existing key constants**
   Sites: `AudioCatalogFieldMaps` re-lists `MUSICBRAINZ_*` / ASF `"MusicBrainz/…"` already on `XiphKnownKeys` / `AsfDescriptorNames`
   Target: reference those constants from the catalog rows (do not collapse the catalog table)
   Value: string-literal drift closed for catalog IDs
   Cost: low
   Rank: medium — ride along with #17 or catalog edits

______________________________________________________________________

## Remaining phase briefs

### Phase 5 — Session / Config / Presets / Reset

**Scope:** Models/Engine config + presets, `Mfr.App.Ui/Services/Session/`, `PersistedConfigurationReset`. One current JSON schema; corrupt → defaults.

### Phase 6 — File List (services + UI)

**Scope:** File List VM/Views + `Services/FileList/`; note `debts.md` shell ops as deferred.

### Phase 7 — Rename List UI + Applied Filters / Palette

**Scope:** Rename List + Applied Filters + Palette; respect open [`rename-list-ui.plan.md`](rename-list-ui.plan.md) phases 14b–16 — review shipped code only.

### Phase 8 — Format Editor + FilterEditors

**Scope:** Format Editor + `FilterEditors/<FilterGroup>/`; skip f5/f6 already-done items.

### Phase 9 — CLI + architecture tests + sweep

**Scope:** CLI, arch tests, backlog triage, `debts.md` refresh if needed.

## Out of scope

- Finishing open feature plan phases (rename-list 14b–16, etc.) unless a review fix is required for correctness.
- Writing a full design doc into the empty stub unless asked separately.
- Re-litigating completed f5/f6 “already done” items.
