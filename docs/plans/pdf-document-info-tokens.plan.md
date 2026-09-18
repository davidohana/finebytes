---
title: PDF document Info tokens
description: Read-only PDF Info + page count tokens and Rename List columns via PdfPig.
---

# PDF document Info tokens

Single slice (no phases). Architecture locked from Cursor plan `createplan_405ae051`.

## Decisions (locked)

- **Tokens + Rename List columns only** — no write filters / Apply in v1
- **Library:** UglyToad.PdfPig in `Mfr.Metadata` (not MetadataExtractor / TagLib)
- **DTO:** nullable `FileMeta.Pdf` / `PdfDocumentInfo` (mirror Image/Exif)
- **Load:** lazy `EnsurePdfLoaded`; `RenameListMetadataRequirement.Pdf`; clear on commit
- **Empty vs error:** missing fields empty after successful open; non-PDF / corrupt → PreviewError
- **Out of scope:** XMP fallback, text extraction, write path, generic documents

## Tokens (`Pdf\Document`)

| Token              | Field                  |
| ------------------ | ---------------------- |
| `<pdf-title>`      | Title                  |
| `<pdf-author>`     | Author                 |
| `<pdf-subject>`    | Subject                |
| `<pdf-keywords>`   | Keywords               |
| `<pdf-creator>`    | Creator (creating app) |
| `<pdf-producer>`   | Producer               |
| `<pdf-created>`    | Creation date          |
| `<pdf-modified>`   | Modification date      |
| `<pdf-page-count>` | Page count             |

Tooltips: `[FormatTokenInfo]` shortDescription on every token; Rename List `tip` for Author / Creator / Producer.

Help: `help/tokens/pdffp.html`, link from `fp.html`, PDF section in `fields.html`, whatsnew bullet. Dev: `docs/pdf-metadata-model.md`, Formatter.md note.

## Slice — implement

- [x] **Done** — Full v1 surface (exit criteria below)

### Exit criteria

1. PdfPig package on `Mfr.Metadata`; `PdfDocumentInfo` + reader; `FileMeta.Pdf`; `EnsurePdfLoaded` / clear / `RenameListMetadataRequirement.Pdf`
1. Nine `pdf-*` tokens + Rename List PDF fields with tips where needed
1. Fixtures + tests (success, missing fields empty, non-PDF PreviewError)
1. Help (`pdffp.html`, `fp.html`, `fields.html`, whatsnew) + `docs/pdf-metadata-model.md` + Formatter.md
1. `just format` + targeted tests pass
