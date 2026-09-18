---
title: Office metadata model
description: How Mfr lazily reads Office PackageProperties via OpenXml for formatter tokens.
---

# Office metadata model

How Magic File Renamer reads Office Open XML PackageProperties for `<office-*>` formatter tokens: a
lazy OpenXml `WordprocessingDocument.Open`, a mapped `OfficeDocumentInfo` snapshot on `FileMeta`, and
empty vs PreviewError rules.

Product/UI sketches live in [magic-file-renamer-design.md](magic-file-renamer-design.md). Folder
layering is in [mfr-folder-layering.md](mfr-folder-layering.md). This cache is read-only (no core.xml
rewrite / Apply path in v1). PDF Info, EPUB Dublin Core, image/EXIF, and TagLib audio stay on separate
caches. v1 opens **`.docx` only**; XLSX/PPTX reuse this DTO/tokens later.

```mermaid
flowchart LR
  tokens["office-*"] --> ensure["EnsureOfficeLoaded"]
  ensure --> reader["OfficeFileReader.Read"]
  reader --> open["WordprocessingDocument.Open"]
  open --> map["OfficeDocumentInfo map"]
  map --> cache["FileMeta.Office"]
  cache --> format["OfficeDocumentInfoFormatting (Models)"]
```

## Design principles

1. **Read-only.** Office PackageProperties are never written on commit. There is no Apply / core.xml
   rewrite path in this slice.
1. **Lazy, one open.** The first `<office-*>` token on a file row in a Preview run opens the package
   once via `WordprocessingDocument.Open(path, isEditable: false)`, maps PackageProperties to
   `OfficeDocumentInfo`, and caches it. Later tokens in the same cycle reuse it. Commit clears the
   cache so the next preview reloads from disk. Do **not** load body parts beyond what Open needs for
   core props.
1. **DTO only.** Raw OpenXml types are not stored on `FileMeta`. Mapping discards them after dispose.
1. **Original snapshot.** Tokens read `item.Original.Office` (disk-backed facts), not Preview.
1. **Readable DOCX only.** Empty tokens apply only after a successful open when a field is missing
   (`null`). Non-`.docx`, corrupt, or OpenXml open failure → PreviewError
   (`OpenXmlPackageException` / `FileFormatException` family, or IO). Missing PackageProperties on a
   successful open is an **empty** field, not PreviewError.
1. **Author from Creator.** DTO + token use **Author**; the OPC source is `PackageProperties.Creator`.
   There is no separate “creating app” field on PackageProperties.
1. **UTC date normalize.** `Created` / `Modified` are `DateTimeOffset?`. OpenXml often surfaces Zulu
   core props as `DateTimeKind.Local`; Unspecified kind is treated as UTC offset 0, and Local/Utc keep
   the same instant then normalize to offset 0 (PDF-stable token times). Formatting matches the PDF
   fork: Token = Invariant `DateTimeOffset` `"G"`; Grid =
   `RenameListFieldDisplay.FormatFileDate(LocalDateTime)`.
1. **Text normalize.** Blank → null; collapse `\r`/`\n` to space; trim (same policy as PDF/EPUB).

## Layer map

- **Snapshot record** — `Mfr.Models` — `OfficeDocumentInfo` on `FileMeta.Office`
- **Disk read / map** — `Mfr.Metadata` — `OfficeFileReader`
- **Lazy load (formatter preview)** — `Mfr.Filters` — `RenameItemOfficeExtensions.EnsureOfficeLoaded`
- **Rename List grid** — eager-loads via `RenameList.EnsureMetadataLoaded`
  (`RenameListMetadataRequirement.Office`); attempt/error state shares the
  `RenameItem` / `RenameListMetadataBuckets` single-flag API with TagLib, Image, PDF, and EPUB
- **Shared field enum + display** — `Mfr.Models` — `OfficeDocumentField` and
  `OfficeDocumentInfoFormatting` under `RenameList/Fields/Office/`;
  `Format(..., PropertyDisplayContext)` with tokens using `Token` and Rename List
  columns using `Grid` (`PropertyDisplayContext` at `RenameList/`). Created/Modified fork: Token =
  Invariant `DateTimeOffset` `"G"`; Grid = `RenameListFieldDisplay.FormatFileDate(LocalDateTime)`
- **Tokens** — `Mfr.Filters` — `OfficeDocumentTokenBase` (`office-*`), FormatEditor group
  `Document\Office`
- **Commit cache clear** — `Mfr.Engine` — `RenameList.Commit` calls `ClearMetadataCaches` →
  `ClearOfficeCache`

## Cache lifetime

`FileMeta.Office` is `null` until the first Office token load. `RenameItem.SetOfficeDocumentInfo`
assigns the same record reference to Original and Preview. `OfficeLoadAttempted` / `OfficeLoadError`
track the cycle. `ClearOfficeCache` nulls the snapshot and resets load state.

`FilterTestHelpers.CreateRenameItem` marks Office load as already attempted on file rows so seeded
unit tests never hit disk. Integration-style tests construct an unmarked `RenameItem` pointing at a
real file.

## Empty vs PreviewError

- **Directory row** — `InvalidOperationException` from ensure → PreviewError
- **Missing / relative path** — `ArgumentException` from the reader → PreviewError
- **Non-DOCX / corrupt open failure** — Propagated OpenXml `OpenXmlPackageException` /
  `FileFormatException` (or `InvalidDataException` for non-`.docx`) → PreviewError
- **Successful open, missing PackageProperties field** — That `office-*` token expands **empty**, not
  an error

## Mapped fields

| Field          | Source                                               |
| -------------- | ---------------------------------------------------- |
| Title          | `PackageProperties.Title`                            |
| Author         | `PackageProperties.Creator`                          |
| Subject        | `PackageProperties.Subject`                          |
| Keywords       | `PackageProperties.Keywords`                         |
| Category       | `PackageProperties.Category`                         |
| Description    | `PackageProperties.Description`                      |
| LastModifiedBy | `PackageProperties.LastModifiedBy`                   |
| Created        | `PackageProperties.Created` → UTC `DateTimeOffset?`  |
| Modified       | `PackageProperties.Modified` → UTC `DateTimeOffset?` |

When a date is absent, the DTO stores `null` and tokens/columns expand empty. Display formatting is
shared via Models `OfficeDocumentInfoFormatting` (see Layer map for the Token vs Grid date split and
UTC normalize above).
