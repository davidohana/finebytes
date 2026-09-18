---
title: Office document Info tokens
description: Read-only Office PackageProperties tokens and Rename List columns via OpenXml (DOCX first).
todos:
  - id: p1-reader
    content: "P1: OpenXml + OfficeDocumentInfo + OfficeFileReader + FileMeta/flag/loader (DOCX only)"
    status: done
  - id: p2-tokens
    content: "P2: office-* tokens + Rename List Office fields/catalog"
    status: done
  - id: p3-docs
    content: "P3: office-metadata-model.md + help/credits + parent P3 mark"
    status: done
---

# Office DOCX PackageProperties (read-only)

Parent: [`docs/plans/more-read-only-metadata-formats.plan.md`](docs/plans/more-read-only-metadata-formats.plan.md) **P3**. Sibling templates: [`docs/plans/epub-document-info-tokens.plan.md`](docs/plans/epub-document-info-tokens.plan.md), [`docs/plans/pdf-document-info-tokens.plan.md`](docs/plans/pdf-document-info-tokens.plan.md).

## Status

- [x] P1 — Reader + FileMeta bucket
- [x] P2 — Tokens + Rename List columns
- [x] P3 — Docs + help

## Decisions (locked)

- **Read-only only** — tokens + Rename List columns; no OpenXml write / Apply (deferred per parent).
- **Library:** `DocumentFormat.OpenXml` on [`Mfr.Metadata/Mfr.Metadata.csproj`](Mfr.Metadata/Mfr.Metadata.csproj) only (forbidden elsewhere via `PackageOwnershipArchitectureTests`, same as PdfPig / VersOne).
- **Open path (P3):** extension must be `.docx` (case-insensitive); then `WordprocessingDocument.Open(path, isEditable: false)` → map `PackageProperties` → dispose. Do **not** load body parts beyond what Open needs for core props.
- **DTO:** nullable `FileMeta.Office` / `OfficeDocumentInfo` in `Mfr.Models.Media` (shared name for parent P4).
- **Author mapping:** DTO + token use **Author**; source is OpenXml `PackageProperties.Creator` (OPC core prop). No separate “creating app” field on PackageProperties.
- **Dates:** `Created` / `Modified` as `DateTimeOffset?` (from package `DateTime?`, unspecified kind → UTC offset 0 or treat as UTC consistently with PDF map). Formatting = PDF fork: Token = Invariant `DateTimeOffset` `"G"`; Grid = `RenameListFieldDisplay.FormatFileDate(LocalDateTime)`; sort via `RenameListFieldSortCompare.DateTime`.
- **Text fields:** `_NormalizeText` copy from PDF/EPUB (`IsBlank` → null; collapse `\r`/`\n` to space; trim).
- **Bucket flag:** `RenameListMetadataRequirement.Office = 16`; wire `RenameListMetadataBuckets.All`, `RenameItem.MetadataBuckets`, `RenameListMetadataLoader._BucketEnsures`, `ClearMetadataCaches` / `FileMeta.Clone`.
- **Empty vs error:** successful open + missing field → empty; non-`.docx` / corrupt / OpenXml open failure → PreviewError (tokens) / soft grid message; directory → `InvalidOperationException`.
- **Token / group ids:** tokens `office-*`; FormatEditor `[FormatTokenInfo]` group `"Document\\Office"`; Rename List `Group = "Office"`, `GroupLabel = "Office Document"` (flat shuttle sibling — do not nest Rename List groups).
- **P4 out of scope** — XLSX/PPTX stay on parent P4 (extend `OfficeFileReader` gate only; reuse this DTO/tokens/columns).
- **No MFR7 names** — net-new (parent brief: no Office property group in MFR7).

## Tokens (`Office`)

| Token                       | Field          | PackageProperties source |
| --------------------------- | -------------- | ------------------------ |
| `<office-title>`            | Title          | `Title`                  |
| `<office-author>`           | Author         | `Creator`                |
| `<office-subject>`          | Subject        | `Subject`                |
| `<office-keywords>`         | Keywords       | `Keywords`               |
| `<office-category>`         | Category       | `Category`               |
| `<office-description>`      | Description    | `Description`            |
| `<office-last-modified-by>` | LastModifiedBy | `LastModifiedBy`         |
| `<office-created>`          | Created        | `Created`                |
| `<office-modified>`         | Modified       | `Modified`               |

Tips: Author (maps from Creator core prop); LastModifiedBy. Optional: Category.

## Architecture

```mermaid
flowchart LR
  tokens["office-*"] --> ensure["EnsureOfficeLoaded"]
  ensure --> reader["OfficeFileReader.Read"]
  reader --> open["WordprocessingDocument.Open"]
  open --> map["OfficeDocumentInfo map"]
  map --> cache["FileMeta.Office"]
  cache --> format["OfficeDocumentInfoFormatting"]
  format --> grid["Rename List columns"]
```

Clone EPUB file-for-file:

| Concern | EPUB template              | Office target                           |
| ------- | -------------------------- | --------------------------------------- |
| Package | VersOne.Epub               | `DocumentFormat.OpenXml`                |
| DTO     | `EpubDocumentInfo`         | `OfficeDocumentInfo`                    |
| Reader  | `EpubFileReader`           | `OfficeFileReader`                      |
| Ensure  | `RenameItemEpubExtensions` | `RenameItemOfficeExtensions`            |
| Fields  | `Fields/Epub/`             | `Fields/Office/`                        |
| Tokens  | `Tokens/Epub/`             | `Tokens/Office/OfficeDocumentTokens.cs` |
| Flag    | `Epub = 8`                 | `Office = 16`                           |
| Docs    | `epub-metadata-model.md`   | `docs/office-metadata-model.md`         |
| Help    | `epubfp.html`              | `help/tokens/officefp.html`             |

## MFR7 reference brief

(Copied from parent — do not re-crawl.)

- **Sources:** MFR7 PropertyGroups / Help — Audio/File/Image/Media only.
- **Behavior:** No DOCX/Office property group or tokens in MFR7.
- **UX:** No MFR7 names; use `office-*` parallel to `pdf-*` / `epub-*`. Group label **Office Document**.
- **Parity gaps:** Intentional net-new; OpenXml write deferred.

## Non-goals

- Write / Apply / core.xml rewrite
- Legacy `.doc` / OLE
- XLSX / PPTX (parent P4)
- Revision, Language, ContentStatus, Version, custom props, body text
- Extension-only success without a successful OpenXml open

## Phases

### P1 — Reader + FileMeta bucket

- **Scope / files:**
  - Add `DocumentFormat.OpenXml` to [`Mfr.Metadata.csproj`](Mfr.Metadata/Mfr.Metadata.csproj).
  - New [`Mfr.Models/Media/OfficeDocumentInfo.cs`](Mfr.Models/Media/OfficeDocumentInfo.cs) — seven `string?` + two `DateTimeOffset?`: Title, Author, Subject, Keywords, Category, Description, LastModifiedBy, Created, Modified.
  - New [`Mfr.Metadata/OfficeFileReader.cs`](Mfr.Metadata/OfficeFileReader.cs) — `Read(absolutePath)`: `RequireExistingRegularFile`; reject non-`.docx`; `using var doc = WordprocessingDocument.Open(..., false)`; map `PackageProperties`; `_NormalizeText` + date map.
  - Wire `FileMeta.Office` + Clone; `RenameItem` Set/Clear/load-attempted/load-error; `RenameListMetadataRequirement.Office = 16`; buckets + loader; soft message `"This file could not be read as Office document Info."`; `_IsMetadataReadFailure` type-name list for OpenXml concretes observed on bad fixtures (e.g. `OpenXmlPackageException` / `FileFormatException` — name-match only, no Filters PackageReference).
  - [`RenameItemOfficeExtensions.EnsureOfficeLoaded`](Mfr.Filters/).
  - [`FilterTestHelpers`](Mfr.Tests/Models/Filters/FilterTestHelpers.cs) mark Office load attempted; architecture package gate for OpenXml.
- **Exit criteria:** Reader maps fixture; non-docx/corrupt throws; ensure caches Original+Preview; clear resets; grid soft-load user message.
- **Tests:** Commit `Mfr.Tests/Fixtures/tiny-info.docx` + `tiny-empty-info.docx`. `OfficeFileReaderTests` mirroring EPUB/PDF. Extend loader / bucket / load-error tests.

### P2 — Tokens + Rename List columns

- **Scope / files:**
  - `Mfr.Models/RenameList/Fields/Office/`: `OfficeDocumentField`, `OfficeDocumentInfoFormatting` (text + PDF-style date arms), `OfficeRenameListFields` (`Group = "Office"`, `GroupLabel = "Office Document"`), field/tips types.
  - Register in [`RenameListFieldCatalog`](Mfr.Models/RenameList/RenameListFieldCatalog.cs).
  - `Mfr.Filters/Formatting/Tokens/Office/OfficeDocumentTokens.cs` — nine `[FormatTokenInfo]` tokens, group `"Document\\Office"`.
  - Catalog / relevant-column / token tests (mirror EPUB).
- **Exit criteria:** All nine `office-*` expand from seeded DTO and fixture; non-docx PreviewError; columns require `Office`; FormatEditor nests under Document.
- **Tests:** `OfficeDocumentTokenTests`; catalog key / metadata-requirement asserts.

### P3 — Docs + help

- **Scope / files:**
  - New [`docs/office-metadata-model.md`](docs/office-metadata-model.md); link from [`AGENTS.md`](AGENTS.md) References.
  - [`Mfr.Filters/docs/Formatting/Formatter.md`](Mfr.Filters/docs/Formatting/Formatter.md) Office section.
  - Help: `help/tokens/officefp.html`; links in `fp.html`, `fields.html`, `features.html`, `whatsnew`, `credits.html` (DocumentFormat.OpenXml).
  - Mark parent plan P3 done + link this child plan.
- **Exit criteria:** Docs/help match shipped tokens; `just format` + targeted tests green.
- **Tests:** none beyond prior phases.

## Fixture notes

Minimal DOCX ZIP (OPC):

- `tiny-info.docx` — `[Content_Types].xml`, `_rels/.rels`, `docProps/core.xml` with all nine props populated, minimal `word/document.xml` + rels so WordprocessingDocument opens.
- `tiny-empty-info.docx` — valid minimal package with empty/absent optional core props so all DTO fields null; open must succeed.

Build under `.tmp/` if needed, then commit binaries under `Mfr.Tests/Fixtures/` (`Content` CopyToOutputDirectory).
