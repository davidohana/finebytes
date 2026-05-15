---
title: Audio tag frame model (implementation plan)
description: Phased plan for structured AudioTagOverlay, per-TagTypes read/write, semantic mapping, selective deletion, and frame-level targets.
---

# Audio tags: per-type overlays and frame-aware persistence

## Overview

Phased work: (1) structured overlay + MP3 ID3v1/v2 round-trip and semantic mapping, (2) extend to other TagLib tag types, (3) harden parity with existing filters/commit tests, (4) selective per-type tag deletion, (5) frame-level filter targets. Replaces flat `AudioTagOverlay` + merged `file.Tag` I/O.

## Checklist (phases)

- [ ] **Phase 1:** Structured overlay (ID3v1/v2 blocks, frame inventory, deep equality); `AudioTagPersistence` Read/Apply for MP3 only; semantic projections + mappers; core/tests on MP3 slice.
- [x] **Phase 2:** Add Xiph, Apple/Mp4, Ape, Asf blocks + Read/Apply round-trips; extend mappers per container.
- [ ] **Phase 3:** Wire `AudioTagSetter` + all `AudioOverlayField` paths; update `RenamePropertyChangeBuilder` / Commit / RenameList tests; `EmbeddedTagRemover` empty-overlay contract.
- [ ] **Phase 4:** Absent optional blocks + per `TagTypes` removal on Apply; filter/UI hooks for deleting specific tag types or frames (keep AllTags nuclear remover).
- [ ] **Phase 5:** `FilterTarget` variants + preview dispatch for specific frames/fields; Id3v2FieldSetter-style filter.

## Current behavior (problem)

- [`Mfr.Metadata/AudioTagPersistence.cs`](../Mfr.Metadata/AudioTagPersistence.cs) reads and writes only TagLib’s merged [`Tag`](https://github.com/mono/taglib-sharp) via `file.Tag`. That collapses ID3v1 vs ID3v2, hides duplicate/conflicting frames, and drops fields that are not mapped to the common surface (e.g. many TXXX variants, APIC, arbitrary Vorbis keys).
- [`Mfr.Models/Tags/AudioTagOverlay.cs`](../Mfr.Models/Tags/AudioTagOverlay.cs) is a single flat “canonical” snapshot, so the app never represents multiple coexisting tag blocks or raw frame lists.

## Target behavior

```mermaid
flowchart LR
  disk[File on disk]
  taglib[TagLib File]
  overlay[AudioTagOverlay container]
  semantic[Semantic fields and filters]
  disk --> taglib
  taglib -->|"GetTag per TagTypes + enumerate"| overlay
  semantic -->|"AudioOverlayField / AudioTagSetter"| mapWrite[Format-specific semantic writers]
  mapWrite --> overlay
  overlay -->|"Reconstruct tags + Save"| disk
```

1. **Load**: For each relevant `TagTypes` instance present on the opened [`File`](https://github.com/mono/taglib-sharp), materialize a **detached** model of that tag (full ID3v2 frame list where applicable; full Vorbis comment key/value multiset; Apple `ilst` atoms; etc.). No silent merge at the overlay level—keep each block separate so ID3v1 vs ID3v2 remain visible and round-trippable.
2. **Semantic edits** (today’s `Title`, `Album`, [`AudioOverlayField`](../Mfr.Models/Tags/AudioOverlayField.cs), [`AudioTagSetterFilter`](../Mfr.Filters/Audio/AudioTagSetterFilter.cs)): do **not** write through `file.Tag` properties. Instead, apply a **small mapping layer per container** (MP3/ID3v2 → `TIT2`, `TPE1`, …; Vorbis → `TITLE`, `ARTIST`, …; MP4 → `©nam`, `©ART`, …) so the same logical field updates the correct underlying storage for that file’s format(s).
3. **Commit**: [`AudioTagPersistence.Apply`](../Mfr.Metadata/AudioTagPersistence.cs) compares the **full structured overlay** to a fresh read from disk (same as today’s “no-op if equal”), then writes by **reattaching** each tag block to TagLib (clear/replace as needed) and `Save()`.

## Phased delivery

Work is split so each phase is shippable and reviewable; later phases assume the overlay container and equality semantics from **Phase 1**.

- **Phase 1 — MP3 vertical slice:** Structured `AudioTagOverlay` with **ID3v1 + ID3v2** blocks (frame inventory, deep **Clone/Equals**); [`AudioTagPersistence`](../Mfr.Metadata/AudioTagPersistence.cs) **Read/Apply for MP3 only**; **semantic properties** (`Title`, …) as projections + writers targeting ID3v2 (documented precedence vs ID3v1); update [`CommitExecutor`](../Mfr.Core/CommitExecutor.cs) and critical tests (commit, persistence, rename list) for this path. **Outcome:** end-to-end proof without boiling the ocean.

- **Phase 2 — More TagLib tag types:** Add blocks and Read/Apply for **Xiph** (FLAC/Vorbis), **Apple** (MP4), **Ape**, **Asf**, etc.; golden round-trip tests per type. **Outcome:** “all formats” coverage from the product goal.
  - **Status:** Implemented in `AudioTagOverlay` / `AudioTagPersistence` (Xiph and Ape use canonical `SerializedTagBlob` bytes; Apple uses sorted `ilst` text atoms with explicit ordinal equality on `AppleAtomRow`; Asf uses sorted content descriptors; ASF-only golden test deferred—no tiny fixture yet). Semantic mappers for non-ID3 containers remain Phase 3 parity.

- **Phase 3 — Semantic and pipeline parity:** Ensure [`AudioTagSetterFilter`](../Mfr.Filters/Audio/AudioTagSetterFilter.cs), [`FileMetaPreviewExtensions`](../Mfr.Models/FileMetaPreviewExtensions.cs), formatter tokens, and [`EmbeddedTagRemover`](../Mfr.Filters/Audio/EmbeddedTagRemoverFilter.cs) behave correctly with structured overlay; adjust [`RenamePropertyChangeBuilder`](../Mfr.Core/RenamePropertyChangeBuilder.cs) as needed. **Outcome:** no regressions vs current feature set.

- **Phase 4 — Selective deletion:** Optional absent blocks = **remove that TagTypes** on disk; **frame-level** removal inside ID3v2 list; optional dedicated filter or UI later; **EmbeddedTagRemover** stays **all tags**. **Outcome:** user can drop e.g. ID3v1 only.

- **Phase 5 — Frame-level targets:** New [`FilterTarget`](../Mfr.Models/Targets.cs) types + preview get/set for specific frames/keys; **Id3v2FieldSetter**-style filter. **Outcome:** power-user editing without new persistence architecture.

**Dependencies:** Phases 2 onward need overlay **extensibility** from Phase 1 (nullable per-block payloads, consistent Apply loop). Phases 4 and 5 rely on **addressable** frames from Phases 1–2.

## Data model (`Mfr.Models`)

Introduce a **structured** `AudioTagOverlay` (name can stay for less churn) that:

- Holds a set of **tag blocks** keyed by kind, e.g. `Id3v1Tag?`, `Id3v2Tag?` (list of frames with id + encoding + payload + frame-specific metadata TagLib exposes), `VorbisCommentTag?` (`FIELD` → list of values), `AppleTagSnapshot?` / MP4 atom model, plus blocks for other `TagTypes` you plan to support in v1 (ASF, APE, etc.).
- Implements **`Clone`**, **`Equals`**, **`GetHashCode`** with **deep** structural equality (required by [`CommitExecutor`](../Mfr.Core/CommitExecutor.cs), [`RenameItem.HasPreviewChanges`](../Mfr.Models/RenameItem.cs), and tests).

**Semantic surface:** Keep existing public string/uint properties (`Title`, `Album`, …) as **projections** over the structured store (read: defined precedence within each format; write: delegate to the same semantic writers used by `FileMetaPreviewExtensions`). That preserves most call sites ([`FileMetaPreviewExtensions`](../Mfr.Models/FileMetaPreviewExtensions.cs), filters, tokens) while the persistence layer stops using the flat model as the on-disk truth.

**Precedence policy** (document in XML on the type): when ID3v1 and ID3v2 disagree, projections should follow product rules aligned with [design §9](magic-file-renamer-design.md) (e.g. prefer ID3v2 for display; optionally mirror writes to ID3v1 when within limits—spell out one rule and apply consistently).

**Binary frames:** For “all tags in the overlay,” include **APIC** / other binary frames in the ID3v2 model **for fidelity**. If memory becomes an issue, a later optimization can lazy-load or cap album-art size; not required for the first correct design.

## Metadata (`Mfr.Metadata`)

- **Read**: Open `TagLib.File`, discover tags via `GetTag(TagTypes.X, create: false)` (and TagLib patterns for “has tag”) for each supported `TagTypes`; populate the overlay blocks by **enumerating** native structures (`Id3v2.Tag` frames, `XiphComment` fields, etc.) rather than copying only `file.Tag.*` properties.
- **Apply**: Build TagLib tags from the overlay blocks; assign to the file (replace tag types that the preview changed; align with design intent of strip-vs-update—[`EmbeddedTagRemover`](../Mfr.Filters/Audio/EmbeddedTagRemoverFilter.cs) still clears via `RemoveTags(TagTypes.AllTags)` + empty overlay).
- **Semantic write helpers** (private static in metadata or a dedicated internal type): `SetTitleForFile(TagLib.File, …)` style that branches on which tag blocks exist / file format, mirroring TagLib’s own mapping rules but driven by **your** overlay blocks.

## Ripple updates (non-exhaustive but required)

- [`RenamePropertyChangeBuilder.cs`](../Mfr.Core/RenamePropertyChangeBuilder.cs): today it diffs flat overlay fields; either keep **semantic** lines only (simplest) or extend to show per-block/frame diffs later.
- [`FileMeta.cs`](../Mfr.Models/FileMeta.cs): still holds one `AudioTagOverlay` per snapshot; clone semantics must be deep.
- Tests: expand [`AudioTagPersistenceTests.cs`](../Mfr.Tests/Metadata/AudioTagPersistenceTests.cs) with **golden** round-trip fixtures (MP3 with ID3v1+v2, FLAC with extra Vorbis keys, one MP4 sample if feasible) plus projection tests (semantic `Title` reads expected frame). Update any tests that assumed equality of `new AudioTagOverlay()` as “empty tags” if the empty state is now structural.

## Relation to design doc

- Brings implementation closer to **§9–10** and **§7.4** (frame-level thinking). The documented **`Id3v2FieldSetter`** remains a natural follow-up: once ID3v2 frames are first-class in the overlay, that filter becomes a thin mutator on the `Id3v2` block rather than a special-case.

### Future: apply / filter targets for specific frames (Phase 5)

**Does this change the core design?** No. The structured overlay is exactly what you need so that **semantic** targets (`AudioOverlayField`) and **frame-level** targets can coexist:

- **Frame targets** become new [`FilterTarget`](../Mfr.Models/Targets.cs) records, for example `Id3v2FrameTarget(frameId, description?, language?)` for text frames that need disambiguation (`COMM`, `USLT`, `TXXX`), or `VorbisFieldTarget(fieldName)` for arbitrary Vorbis keys, and an Apple/MP4 atom target if ever needed.
- **Preview I/O**: Extend the same `GetTargetString` / `SetTargetString` dispatch in [`FileMetaPreviewExtensions`](../Mfr.Models/FileMetaPreviewExtensions.cs) (or a dedicated internal helper) to read/write **directly** into the appropriate tag block’s frame list / key-value list instead of going through `Title` / `Album` projections.
- **Commit path**: Unchanged in principle—`AudioTagPersistence.Apply` still serializes the **whole overlay**; frame targets only mutate different properties on that overlay before commit.

**Design constraint to keep in mind now:** Model ID3v2 (and Vorbis/MP4) so that **individual frames or values are addressable** (not only a merged map keyed by a single string). For example, support multiple `COMM`/`USLT`/`TXXX` instances by storing enough metadata (language, description, owner id) to match TagLib’s frame identity.

This ships in **Phase 5**; it does not require redesigning the overlay again if Phases 1–2 already store an enumerated frame list per block.

### Deletion: entire specific tag blocks (Phase 4)

**Requirement:** Besides “clear this semantic field” and [`EmbeddedTagRemover`](../Mfr.Filters/Audio/EmbeddedTagRemoverFilter.cs) (all embedded tags), support removing **whole tag types** on a file—for example strip **ID3v1 only** while keeping **ID3v2**, or remove **Vorbis comments** but leave other containers untouched, as the format allows.

**Design impact:** Fits the structured overlay if each block is **optional** (`Id3v1Tag?`, `Id3v2Tag?`, …) and **absent** means “do not write this tag type on commit” (equivalent to `RemoveTags(that TagTypes)` or replacing with no tag for that slot). Distinguish carefully:

- **Block missing / null in overlay after user action “delete this tag”:** persist as **removal** of that entire embedded blob on disk.
- **Block present but with no frames / empty:** still a valid state if the format allows an empty tag (usually prefer treating as removal—follow TagLib semantics per type).

**Apply semantics:** When building the file to save, for each `TagTypes` the overlay tracks: if the preview says that block is **removed**, call the appropriate TagLib removal for that type only; do not rely on `file.Tag = null` for everything.

**Product / follow-up:** A dedicated filter or UI action (“Remove ID3v1”, “Remove Vorbis comments”) sets the preview overlay to drop that block; [`EmbeddedTagRemover`](../Mfr.Filters/Audio/EmbeddedTagRemoverFilter.cs) remains the **nuclear** option (`TagTypes.AllTags` + fully empty overlay). **Frame-level delete** (single `APIC` frame) is the same idea at finer granularity: remove one frame from the `Id3v2` list in the overlay; commit writes the updated frame list.

### Risk note

Scope is bounded by **Phases 1–2** (MP3 first, then extend containers). **Keep the public `AudioTagOverlay` semantic surface stable** after Phase 1 to limit churn in filters and tests.
