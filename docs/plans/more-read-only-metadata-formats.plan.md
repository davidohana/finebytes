---
name: More metadata formats
overview: "Ship more rename-useful metadata by cost-to-value: extend the existing image/EXIF allowlist for HEIF first, then add PDF-shaped read-only EPUB and Office buckets (DOCX, then XLSX/PPTX). No write/Apply in this plan."
todos:
  - id: p1-heif
    content: "P1: HEIF allowlist + dim/EXIF mapping, fixture, docs/help"
    status: done
  - id: p2-epub
    content: "P2: VersOne.Epub PDF-shaped EpubDocumentInfo bucket, tokens, columns"
    status: done
  - id: p3-docx
    content: "P3: OpenXml OfficeDocumentInfo for DOCX PackageProperties"
    status: done
  - id: p4-office-rest
    content: "P4: Extend Office reader to XLSX + PPTX"
    status: pending
isProject: false
---

# More read-only metadata formats

Parent / sibling: shipped PDF Info tokens ([`docs/pdf-metadata-model.md`](docs/pdf-metadata-model.md)). Clone that bucket pattern for Office. HEIF extends [`docs/image-metadata-model.md`](docs/image-metadata-model.md), not a new `FileMeta` bucket.

## Decisions (locked)

- **Priority = cost-to-value** (phases ordered below). Skip low-ROI items into Deferred.
- **Read-only only** — tokens + Rename List columns; no write filters / Apply (even though OpenXml and TagLib can write).
- **HEIF first** — widen MetadataExtractor image/EXIF allowlist; no new NuGet if 2.9.3 maps dims/EXIF; bump ME only if HEIF mapping fails in spike.
- **Allowlist `HEIF` only** (ME detected name for `.heic`/`.heif`) — not full camera RAW in this plan.
- **EPUB** — new package `VersOne.Epub`; new `FileMeta` bucket + `RenameListMetadataRequirement` flag; Dublin Core subset (title, creator, publisher, language, date, identifier, subject, description).
- **Office** — `DocumentFormat.OpenXml`; shared `OfficeDocumentInfo` DTO for PackageProperties; DOCX in P3, XLSX+PPTX in P4 (same reader family, extension/content-type gate).
- **Empty vs error** — same as PDF: successful open + missing field → empty; wrong/corrupt type → PreviewError.
- **Shared field enums** — new buckets use one Models enum + `PropertyDisplayContext` (Token vs Grid), same as PDF after unify.
- **Video frame-rate / container-only tags** — **not in this plan** (media already matches MFR7’s 15 TagLib fields; title/year for MP4 already live on audio/Apple tag surface).

## Cost-to-value ranking

| Rank | Item                                          | Why                                                                      |
| ---- | --------------------------------------------- | ------------------------------------------------------------------------ |
| 1    | HEIF allowlist                                | No new bucket/package; unlocks phone photos for `<image-*>` / `<exif-*>` |
| 2    | EPUB                                          | High rename value (Author – Title); PDF-shaped clone + one reader lib    |
| 3    | DOCX                                          | Same PDF shape; huge desktop corpus; OpenXml write deferred              |
| 4    | XLSX / PPTX                                   | Amortize Office DTO/reader after DOCX                                    |
| —    | RAW / sidecars / fonts / video extras / write | Deferred (see Non-goals)                                                 |

## MFR7 reference brief

### Sources

- `D:\Devl\mfr7\Site\finebytes\mfr\Help\` (`fields.html`, `fp.html`, image/media help)
- `D:\Devl\mfr7\Core\MfrFilters\PropertyGroups\`, `FormattingParams\Audio\MediaPropertiesFP.cs`

### Behavior

- **No** EPUB, DOCX/Office, or HEIF property groups in MFR7.
- PDF in MFR7 = Image Tag / XMP carrier via TagLib — **not** Info dictionary (finebytes `pdf-*` is already ahead).
- Media Properties = 15 TagLib fields, extract-only; finebytes already at parity (`<media-*>`).

### UX

- MFR7 media tokens were `mediaproperties-*`; finebytes uses `<media-*>`.
- No MFR7 token names to copy for EPUB/Office/HEIF.

### Parity gaps

- This plan is **net-new product**, not MFR7 parity (except HEIF filling a modern-format hole MFR7 never had).
- Image Tag / PDF-XMP remain on [`image-tag-editing.plan.md`](docs/plans/image-tag-editing.plan.md) — out of scope here.

## Non-goals

- Write / Apply for EPUB, Office, PDF, HEIF EXIF
- Camera RAW allowlist (CR2/NEF/ARW/…)
- XMP sidecars, font name tables
- Video frame rate / video bitrate / Matroska-specific tokens
- PDF XMP fallback; EPUB DRM / full spine text
- Legacy `.doc` / `.xls` / `.ppt` (OLE)

## Architecture (new document buckets)

```mermaid
flowchart LR
  tokens["epub-* or office-*"] --> ensure[EnsureLoaded]
  ensure --> reader[Mfr.Metadata reader]
  reader --> lib[VersOne or OpenXml]
  lib --> dto[FileMeta DTO]
  dto --> format[Models Formatting]
  format --> grid[Rename List columns]
```

Clone PDF wiring: DTO on `FileMeta` → reader in `Mfr.Metadata` → `Ensure*Loaded` + `RenameListMetadataLoader` bucket → shared field enum/formatting → tokens + Rename List fields → commit `ClearMetadataCaches` → help + `docs/*-metadata-model.md` + Formatter.md.

Key PDF template files:

- [`Mfr.Metadata/PdfFileReader.cs`](Mfr.Metadata/PdfFileReader.cs)
- [`Mfr.Filters/RenameItemPdfExtensions.cs`](Mfr.Filters/RenameItemPdfExtensions.cs)
- [`Mfr.Models/RenameList/Fields/Pdf/`](Mfr.Models/RenameList/Fields/Pdf/)
- [`Mfr.Filters/Formatting/Tokens/Pdf/PdfDocumentTokens.cs`](Mfr.Filters/Formatting/Tokens/Pdf/PdfDocumentTokens.cs)

## Phases

### P1 — HEIF on image/EXIF allowlist ✅

- **Scope / files:** Spike ME on a `.heic` fixture: confirm `TagDetectedFileTypeName` (`HEIF`), dimension + EXIF directories. Then update [`ImagePropertiesReader`](Mfr.Metadata/ImagePropertiesReader.cs) allowlist + `_ReadDimensions` / bit-depth / frame-count arms; EXIF path via existing [`ExifDataReader`](Mfr.Metadata/ExifDataReader.cs) if directories present. Docs: [`image-metadata-model.md`](docs/image-metadata-model.md), Formatter.md image section, `whatsnew` / image help. Bump `MetadataExtractor` only if spike proves 2.9.3 insufficient.
- **Exit criteria:** `.heic`/`.heif` succeed for `<image-width>` and at least one EXIF field when present; non-HEIF audio still PreviewError; docs/help updated.
- **Tests:** Fixture + `ImagePropertiesReader` / image+exif token tests (success + missing EXIF empty).
- **Shipped:** MetadataExtractor **2.9.3** kept (spike OK). Allowlist `HEIF`; fixtures `tiny.heic` / `tiny-exif.heic` (`.heif` extension covered via temp copy of `tiny.heic`).

### P2 — EPUB document Info (read-only) ✅

- **Scope / files:** Add `VersOne.Epub` to [`Mfr.Metadata.csproj`](Mfr.Metadata/Mfr.Metadata.csproj). New `EpubDocumentInfo` + `EpubFileReader`; `FileMeta.Epub`; new `RenameListMetadataRequirement` bit; Ensure/clear/loader; `EpubDocumentField` + formatting + Rename List fields; `epub-*` tokens; `docs/epub-metadata-model.md`; help `epubfp.html` + fields/fp/whatsnew.
- **Fields (v1):** Title, Creator, Publisher, Language, Date, Identifier, Subject, Description (first/primary string where lists exist).
- **Exit criteria:** Valid EPUB expands tokens/columns; missing DC fields empty; non-EPUB/corrupt → PreviewError; format + targeted tests green.
- **Tests:** Minimal EPUB fixture + reader/token/catalog/loader tests mirroring PDF.
- **Shipped:** EPUB reader/bucket, tokens/columns, docs/help ([`docs/epub-metadata-model.md`](docs/epub-metadata-model.md)).

### P3 — Office DOCX PackageProperties (read-only) ✅

- **Scope / files:** Add `DocumentFormat.OpenXml`. Shared `OfficeDocumentInfo` + `OfficeFileReader` (DOCX only in this phase). New metadata requirement bit (or shared `Office` bucket). Tokens `office-*` (or `docx-*` — **lock `office-*`** so P4 reuses names). Fields: Title, Author/Creator, Subject, Keywords, Category, Description, LastModifiedBy, Created, Modified (PackageProperties mapping).
- **Exit criteria:** DOCX Info tokens/columns work; wrong type PreviewError; docs `office-metadata-model.md` + help; no write path.
- **Tests:** Tiny DOCX fixture with core props + empty-props case.
- **Shipped:** Child plan [`docs/plans/office-document-info-tokens.plan.md`](docs/plans/office-document-info-tokens.plan.md) P1–P3 (reader/bucket, tokens/columns, docs/help).

### P4 — Office XLSX + PPTX

- **Scope / files:** Extend `OfficeFileReader` content-type / extension gate for spreadsheet and presentation; same DTO/tokens/columns. Help note that one Office group covers all three.
- **Exit criteria:** One fixture each for xlsx/pptx; shared tests parametrized by format.
- **Tests:** Reader matrix + one token smoke per format.

## Deferred (separate plans later)

- Camera RAW allowlist after HEIF proves mapping cost
- Office / EPUB write Apply (OpenXml is capable; EPUB needs OPF rewrite)
- XMP sidecars + rename pairing
- Font name-table tokens
- Video frame rate / bitrate tokens beyond current TagLib media set
- Image Tag / PDF-XMP ([`image-tag-editing.plan.md`](docs/plans/image-tag-editing.plan.md))
