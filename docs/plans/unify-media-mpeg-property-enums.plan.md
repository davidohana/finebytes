---
title: Unify Media and MPEG property enums
description: >-
  Apply the Image/PDF Models-owned field enum + PropertyDisplayContext formatter
  pattern to Media and MPEG token/Rename List twins; optional helper collapse.
status: done
---

# Unify Media and MPEG property enums

Parent / origin: deferred deeper refactor from
[`unify-token-rename-list-property-enums.plan.md`](unify-token-rename-list-property-enums.plan.md)
(shipped). Internal cleanup — not a product feature.

## Priority

| Rank | Phase                    | Priority   | Why                                                                                      |
| ---- | ------------------------ | ---------- | ---------------------------------------------------------------------------------------- |
| 1    | **P1 — Media**           | **High**   | Largest remaining twin; key-name drift is the real risk; proves map-heavy case           |
| 2    | **P2 — MPEG**            | **Medium** | Same pattern, smaller surface; ship after Media so CatalogPropertyKey hits rule-of-three |
| 3    | **P3 — Helper collapse** | **Low**    | Optional; ride Media/MPEG move onto Models helpers; skip alone if cost outweighs         |

Do **not** start P3 before P1–P2. EXIF / Jpeg / Audio-tag twins stay out of this plan.

## Problem

Media and MPEG still keep parallel property enum + formatter stacks (token vs Rename List),
unlike Image/PDF which now share one Models-owned enum + `PropertyDisplayContext` formatter.

| Domain | Token (Filters)                                            | Rename List (Models)                                      |
| ------ | ---------------------------------------------------------- | --------------------------------------------------------- |
| Media  | `MediaPropertyField` + `MediaPropertiesFormatting`         | `MediaRenameListProperty` + `MediaRenameListFieldDisplay` |
| MPEG   | `MpegAudioPropertyField` + `MpegAudioPropertiesFormatting` | `MpegRenameListProperty` + `MpegRenameListFieldDisplay`   |

Unlike Image/PDF, **enum member names are not 1:1** — catalog keys and DTO property names
already differ from token-oriented enum labels. Format logic is near-copy (YesNo / duration /
positive int / bitrate+layer helpers); no intentional Token vs Grid culture/date fork today.
Tokens still hand-map enum → `*RenameListFields.Key.*` in `TryGetFixedField`.

### Known name drift (must map explicitly)

**Media** (token enum → RL enum / DTO):

| Token `MediaPropertyField` | RL `MediaRenameListProperty` / DTO    |
| -------------------------- | ------------------------------------- |
| `Corrupt`                  | `PossiblyCorrupt` / `PossiblyCorrupt` |
| `DurationSec`              | `DurationSeconds`                     |
| `SampleRate`               | `AudioSampleRate` / `AudioSampleRate` |
| `Channels`                 | `AudioChannels` / `AudioChannels`     |
| (others)                   | same label                            |

**MPEG** (token enum → RL enum / catalog key):

| Token `MpegAudioPropertyField` | RL `MpegRenameListProperty` / Key           |
| ------------------------------ | ------------------------------------------- |
| `Encoding`                     | `Vbr` / `VBR`                               |
| `MpegVer`                      | `Level`                                     |
| `DurationSec`                  | `DurationSecs`                              |
| (others)                       | same or trivial (`Frequency`, `Bitrate`, …) |

Text arms today: tokens use `?? string.Empty`; grid uses `FormatOptionalText` (blank → empty).
Unify on Models helpers (Image/PDF precedent); whitespace-only edge may tighten to empty — accept.

After P1+P2, `CatalogPropertyKey` exists on Image, PDF, Media, MPEG (4 sites) — optional shared
helper only if it stays thin; do not force abstraction in P1.

## Decisions (locked)

1. **Reuse Image/PDF pattern** — one Models-owned field enum per domain under
   `RenameList/Fields/<Domain>/`; one `*Formatting.Format(dto, field, PropertyDisplayContext)`;
   delete RL duplicate enum + Filters-local formatter; `CatalogPropertyKey` next to field type;
   sort stays on the field type; unknown arm → `UnreachableException`; types `internal`.
1. **Keep token enum type + member names** (`MediaPropertyField`, `MpegAudioPropertyField`,
   including `Corrupt` / `Encoding` / `MpegVer`). Do **not** rename members to match RL/DTO —
   map in formatter + `CatalogPropertyKey` only. Token names and persisted column keys unchanged.
1. **`PropertyDisplayContext` required** even with no fork today (Media/MPEG arms may ignore
   `context` like Image). Do not invent a Token/Grid culture split.
1. **Display via `RenameListFieldDisplay`** (and local bitrate/layer helpers) once in Models —
   drop Filters `PropertyValueFormatting` use from these formatters.
1. **Ship Media first (P1), then MPEG (P2)** — Media is prio high; MPEG medium.
1. **P3 helper collapse is optional / low prio** — only if Media+MPEG leave
   `PropertyValueFormatting` with few or no callers; otherwise leave a thin Filters helper and
   note in Open. Do not expand into EXIF/Audio.
1. **No Help / whatsnew** unless a user-visible string accidentally changes (should not).

## MFR7 reference brief

Not a parity port. finebytes already exposes Media / MPEG as shared property groups for columns
and formatter tokens. This plan only removes the internal dual C# representation, following the
shipped Image/PDF unify. No new MFR7 crawl required (parent brief still applies).

## Non-goals

- EXIF / Jpeg / Audio-tag / Id3v2 twin unifies
- Renaming token strings, Rename List group ids, or persisted column keys
- Inventing Token vs Grid display forks for Media/MPEG
- Changing sort rules or tips
- Global rewrite of every Filters token formatter

## Existing stubs (finebytes)

- Pattern to clone: `Mfr.Models/RenameList/PropertyDisplayContext.cs`;
  `RenameList/Fields/Image|Pdf/` (`*PropertyField`, `*Formatting`, `CatalogPropertyKey`)
- Media token: `Mfr.Filters/Formatting/Tokens/Media/MediaPropertiesFormatting.cs`,
  `MediaPropertyTokens.cs`
- Media RL: `Mfr.Models/RenameList/Fields/Media/MediaRenameListField.cs`
  (`MediaRenameListProperty`, `MediaRenameListFieldDisplay`)
- MPEG token: `Mfr.Filters/Formatting/Tokens/Mpeg/MpegAudioPropertiesFormatting.cs`,
  `MpegAudioPropertyTokens.cs`
- MPEG RL: `Mfr.Models/RenameList/Fields/Mpeg/MpegRenameListField.cs`
  (`MpegRenameListProperty`, `MpegRenameListFieldDisplay`)
- Helpers: Models `RenameListFieldDisplay` (ints/duration/YesNo/optional text); Filters
  `PropertyValueFormatting` removed in P3 (was unused after Media/MPEG move)
- Tests: `MediaPropertyTokenTests`, `MpegAudioPropertyTokenTests`, Rename List catalog/sort
- Docs (P3 or phase docs): media sections in Formatter.md if stubs still point at Filters
  formatters; no dedicated media-metadata-model.md today

## Phases

### P1 — Media: single enum + context-aware formatter (prio: high)

- **Status:** done (reviewed)
- **Scope / files:** Move `MediaPropertyField` + `MediaPropertiesFormatting` into Models under
  `RenameList/Fields/Media/`; `Format(media, field, PropertyDisplayContext)`; wire
  `MediaPropertyRenameListField` + `MediaPropertyTokenBase`; delete `MediaRenameListProperty` /
  Filters-local enum+formatter; explicit `CatalogPropertyKey` for name-drift arms; keep tips/sort.
- **Exit criteria:** No dual Media property enums; token + grid tests pass; display strings
  unchanged for normal values; drift map covered (Corrupt / DurationSec / SampleRate / Channels).
- **Tests:** Existing `MediaPropertyTokenTests` + Media catalog/resolve/sort; assert key map for
  drifted members (at least Corrupt → PossiblyCorrupt key).

### P2 — MPEG: same pattern (prio: medium)

- **Status:** done (reviewed)
- **Scope / files:** Same as P1 for `MpegAudioPropertyField` / `MpegAudioPropertiesFormatting`;
  delete `MpegRenameListProperty`; keep bitrate/VBR prefix and layer Roman helpers once in Models;
  map Encoding→VBR key, MpegVer→Level, DurationSec→DurationSecs.
- **Exit criteria:** No dual MPEG property enums; `MpegAudioPropertyTokenTests` + MPEG
  catalog/sort pass; Encoding/MpegVer key map correct.
- **Tests:** Existing MPEG token + catalog coverage; assert Encoding→`Key.VBR` and
  MpegVer→`Key.Level` (or equivalent catalog constants).

### P3 — Optional: collapse Filters `PropertyValueFormatting` (prio: low)

- **Status:** done (deferred review → batch)
- **Scope / files:** If Media+MPEG no longer call `PropertyValueFormatting`, delete it or leave
  only remaining callers; optionally extract a tiny shared `CatalogPropertyKey` helper only if
  Image/PDF/Media/MPEG copies are identical enough (rule of three — prefer after P2). Short
  Formatter.md / plan stub updates if paths drifted.
- **Exit criteria:** No orphan dead helper, or documented thin remainder; no behavior change.
- **Tests:** none beyond existing token suites if code touched; else docs-only.
- **Done:** deleted orphan `PropertyValueFormatting` (0 callers). Skipped shared
  `CatalogPropertyKey` helper — four domain switches stay domain-specific (low cost-to-value).

## Open (tidy)

None at write time. After P2, reconsider EXIF / Audio-tag twins only if a third independent
drift class appears — do not expand this plan’s phases to absorb them.
