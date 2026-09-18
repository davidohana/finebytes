---
title: Unify token and Rename List property enums
description: >-
  Collapse Image and PDF twin field enums/formatters (token vs Rename List) into
  one Models-owned property enum + culture-aware formatter each.
---

# Unify token and Rename List property enums

Parent / origin: deferred deeper refactor from PDF Info review (after
[`pdf-document-info-tokens.plan.md`](pdf-document-info-tokens.plan.md)); same twin
already exists for Image. Internal cleanup — not a product feature.

## Problem

For Image and PDF, formatter tokens and Rename List columns each keep a **parallel
property enum + formatter** for the same DTO fields:

| Domain | Token (Filters) | Rename List (Models) |
|--------|-----------------|----------------------|
| Image | `ImagePropertyField` + `ImagePropertiesFormatting` | `ImageRenameListProperty` + `ImageRenameListFieldDisplay` |
| PDF | `PdfDocumentField` + `PdfDocumentInfoFormatting` | `PdfRenameListProperty` + `PdfRenameListFieldDisplay` |

Tokens also hand-map enum → `*RenameListFields.Key.*` in `TryGetFixedField`. Adding a
field requires touching both stacks; dates already diverge by design (token
Invariant `"G"` vs grid `FormatFileDate` / CurrentCulture) and are easy to drift
further.

Media/Mpeg have the same shape (`MediaPropertyField` vs media Rename List fields)
but are **out of scope** here — apply the Image/PDF pattern later if useful.

## Decisions (locked)

1. **One Models-side field enum per domain** — e.g. `ImagePropertyField` /
   `PdfDocumentField` live in `Mfr.Models` (next to the DTO or under
   `RenameList/Fields/<Domain>/`). Filters tokens consume that enum; delete the
   Rename List duplicate enum.
2. **One formatter per domain** in Models (or a Models helper used by both), with
   an explicit **display context** (`Token` vs `Grid`) or `IFormatProvider` so
   Invariant token dates/DPI stay distinct from CurrentCulture grid display.
   Do **not** force tokens and grid to the same culture.
3. **Catalog keys stay string constants** on `*RenameListFields.Key`; field enum →
   key map lives once next to the Rename List field type (tokens call that map
   from `TryGetFixedField`).
4. **Sort comparers stay on the Rename List field type** (numeric/date vs string);
   formatter is display-only.
5. **Ship Image first, then PDF** — Image proves the pattern; PDF is the smaller
   recent twin.
6. **No Help / whatsnew** unless a user-visible string accidentally changes (should
   not).

## MFR7 reference brief

Not a parity port. MFR7 exposed Image / document properties as single property
groups (columns + formatting tokens from the same group). finebytes already
matches that UX; this plan only removes an internal dual-representation in C#.
No new MFR7 crawl required.

## Non-goals

- Unifying Media / Mpeg / EXIF / Jpeg / Audio tag twins (follow-up after this pattern)
- Changing token names, Rename List group ids, or persisted column keys
- Merging token Invariant vs grid CurrentCulture display rules
- Metadata load-bucket work (already done: `RenameListMetadataBuckets`)
- Write/Apply paths, image-tag editing, PDF write

## Existing stubs (finebytes)

- Image token stack: `Mfr.Filters/Formatting/Tokens/Image/ImagePropertiesFormatting.cs`,
  `ImagePropertyTokens.cs`
- Image RL stack: `Mfr.Models/RenameList/Fields/Image/ImageRenameListField.cs`
  (`ImageRenameListProperty`, `ImageRenameListFieldDisplay`)
- PDF token stack: `Mfr.Filters/Formatting/Tokens/Pdf/PdfDocumentInfoFormatting.cs`,
  `PdfDocumentTokens.cs`
- PDF RL stack: `Mfr.Models/RenameList/Fields/Pdf/PdfRenameListField.cs`
  (`PdfRenameListProperty`, `PdfRenameListFieldDisplay`)
- Shared grid helpers: `RenameListFieldDisplay` (optional text, positive int, DPI,
  file date)
- Token helpers: `PropertyValueFormatting` in Filters/Utils
- Docs: `docs/image-metadata-model.md`, `docs/pdf-metadata-model.md`,
  `Mfr.Filters/docs/Formatting/Formatter.md`

## Phases

### P1 — Image: single enum + context-aware formatter

- **Scope / files:** Move/keep one `ImagePropertyField` in Models; one
  `ImagePropertiesFormatting.Format(image, field, displayContext)` (or culture);
  `ImagePropertyRenameListField` + `ImagePropertyTokenBase` both use it; delete
  `ImageRenameListProperty` / Filters-local enum duplicate; wire `TryGetFixedField`
  via shared key map.
- **Exit criteria:** No dual Image property enums; token + grid tests still pass;
  date/DPI token output remains Invariant; grid display unchanged for CurrentCulture.
- **Tests:** Existing `ImagePropertyTokenTests` + any Rename List Image resolve/sort
  coverage; add one assertion that token vs grid date/DPI culture paths still
  differ if a date-like image field exists (DPI is enough: Invariant vs grid helper).

### P2 — PDF: same pattern

- **Scope / files:** Same as P1 for `PdfDocumentField` /
  `PdfDocumentInfoFormatting`; delete `PdfRenameListProperty` and Filters-local
  duplicate; keep Author/Creator/Producer tips and sort rules.
- **Exit criteria:** No dual PDF property enums; `PdfDocumentTokenTests` + PDF
  catalog/sort tests pass; `<pdf-created>` / grid Created still culture-split.
- **Tests:** Existing PDF token + `CompareForSort` coverage; assert Created token
  Invariant `"G"` vs grid `FormatFileDate`.

### P3 — Docs only

- **Scope / files:** Note single field enum + display context in
  `docs/image-metadata-model.md`, `docs/pdf-metadata-model.md`, and a short
  Formatter.md cross-link; mark this plan done.
- **Exit criteria:** Docs match code; no behavior change.
- **Tests:** none (docs-only).

## Open (tidy)

None at write time. After P2, Media is the natural third twin — open a small follow-up
plan then if Media/Mpeg still duplicate token vs Rename List enums; do not expand this
plan’s phases to absorb Media.
