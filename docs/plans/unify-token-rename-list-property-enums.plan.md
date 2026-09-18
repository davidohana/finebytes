---
title: Unify token and Rename List property enums
description: >-
  Collapse Image and PDF twin field enums/formatters (token vs Rename List) into
  one Models-owned property enum + context-aware formatter each.
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

Enums already match 1:1 (same member names/order). Format logic is near-copy; the
only intentional Token vs Grid behavior fork is **PDF Created/Modified**:

- Token: `DateTimeOffset.ToString("G", CultureInfo.InvariantCulture)`
- Grid: `RenameListFieldDisplay.FormatFileDate(date.LocalDateTime)` → `"G"` +
  `CurrentCulture`

Ints/text/DPI already share the same rules (DPI is Invariant on both sides —
not a culture split). Tokens also hand-map enum → `*RenameListFields.Key.*` in
`TryGetFixedField`. Adding a field still requires touching both stacks, and the
PDF date fork is easy to re-diverge if left duplicated.

Media/Mpeg have the same shape (`MediaPropertyField` vs media Rename List fields)
but are **out of scope** here — apply the Image/PDF pattern later if useful
(Media also has key-name drift: e.g. `Corrupt` → `PossiblyCorrupt`).

## Decisions (locked)

1. **One Models-side field enum per domain** — keep token names
   (`ImagePropertyField` / `PdfDocumentField`) in `Mfr.Models` under
   `RenameList/Fields/<Domain>/` next to the field type + key map. Filters tokens
   consume that enum; delete the Rename List duplicate enum
   (`ImageRenameListProperty` / `PdfRenameListProperty`).
2. **One formatter per domain** in Models, with an explicit
   **`PropertyDisplayContext { Token, Grid }`** (name flexible). Do **not** use
   bare `IFormatProvider` alone — PDF dates also differ by
   `DateTimeOffset` vs `LocalDateTime`, not only culture. Image has no
   context-sensitive arms today; still take the context parameter so P1 proves
   the PDF-ready signature (Image may ignore it until a fork appears).
   Do **not** force tokens and grid to the same culture/date representation.
3. **Catalog keys stay string constants** on `*RenameListFields.Key`; field enum →
   key map lives once next to the Rename List field type (tokens call that map
   from `TryGetFixedField`).
4. **Sort comparers stay on the Rename List field type** (numeric/date vs string);
   formatter is display-only. PDF sort keeps UTC chronological compare — do not
   route sort through the culture formatter.
5. **Unknown enum arm:** exhaustive `switch` → `throw UnreachableException()`
   (token style). Drop the grid’s `_ => string.Empty` fallback.
6. **Ship Image first, then PDF** — Image proves the pattern; PDF is the smaller
   recent twin and is where context actually branches.
7. **No Help / whatsnew** unless a user-visible string accidentally changes (should
   not).

## MFR7 reference brief

Not a parity port. MFR7 exposed Image / document properties as single property
groups (columns + formatting tokens from the same group). finebytes already
matches that UX; this plan only removes an internal dual-representation in C#.
No new MFR7 crawl required.

## Non-goals

- Unifying Media / Mpeg / EXIF / Jpeg / Audio tag twins (follow-up after this pattern)
- Changing token names, Rename List group ids, or persisted column keys
- Merging token Invariant vs grid CurrentCulture (and LocalDateTime) display rules
- Collapsing `PropertyValueFormatting` vs `RenameListFieldDisplay` helpers globally
  (optional later; Image/PDF formatters should call Models helpers once moved)
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
- Token helpers: `PropertyValueFormatting` in Filters (ints/duration/YesNo; no
  DPI/date — Image/PDF tokens inline those today)
- Layering: Filters → Models allowed; Models must not reference Filters (formatter
  in Models is the correct direction)
- Docs: `docs/image-metadata-model.md`, `docs/pdf-metadata-model.md`,
  `Mfr.Filters/docs/Formatting/Formatter.md`

## Phases

### P1 — Image: single enum + context-aware formatter

- **Scope / files:** Move `ImagePropertyField` + `ImagePropertiesFormatting` into
  Models under `RenameList/Fields/Image/`; signature
  `Format(image, field, PropertyDisplayContext)` (context unused for Image arms);
  `ImagePropertyRenameListField` + `ImagePropertyTokenBase` both use it; delete
  `ImageRenameListProperty` / Filters-local enum + Filters formatter; wire
  `TryGetFixedField` via shared key map next to the field type.
- **Exit criteria:** No dual Image property enums; token + grid tests still pass;
  Image display strings unchanged (all arms culture-identical today).
- **Tests:** Existing `ImagePropertyTokenTests` + Rename List Image resolve/sort
  coverage; no new “culture split” assertion for Image (there is none). Optionally
  assert Format still compiles with both contexts if useful smoke.

### P2 — PDF: same pattern

- **Scope / files:** Same as P1 for `PdfDocumentField` /
  `PdfDocumentInfoFormatting`; delete `PdfRenameListProperty` and Filters-local
  duplicate; keep Author/Creator/Producer tips and sort rules; context branches
  only on Created/Modified (Token Invariant `DateTimeOffset` `"G"` vs Grid
  `FormatFileDate(LocalDateTime)`).
- **Exit criteria:** No dual PDF property enums; `PdfDocumentTokenTests` + PDF
  catalog/sort tests pass; `<pdf-created>` / grid Created still split as above.
- **Tests:** Existing PDF token + `CompareForSort` coverage; assert Created token
  Invariant `"G"` on `DateTimeOffset` vs grid `FormatFileDate` on `LocalDateTime`.

### P3 — Docs only

- **Scope / files:** Note single field enum + display context in
  `docs/image-metadata-model.md`, `docs/pdf-metadata-model.md`, and a short
  Formatter.md cross-link; mark this plan done.
- **Exit criteria:** Docs match code; no behavior change.
- **Tests:** none (docs-only).

## Open (tidy)

None at write time. After P2, Media is the natural third twin — open a small follow-up
plan then if Media/Mpeg still duplicate token vs Rename List enums; do not expand this
plan’s phases to absorb Media (Media needs key-name mapping, not a pure enum merge).
