# Image tag editing plan

Parent: advisory notes in Cursor plan `image_tag_editing_0562126d` (same feature). No prior
`docs/plans/` parent for image write.

## Existing stubs (finebytes)

- **No** `ImageTagOverlay` / `SemanticImageTag` / Image Tag Rename List fields / image-tag filters.
- `FileMeta.AudioTagOverlay` is a **non-null** overlay (`= new()`), not a nullable cache. Load state
  is `RenameItem.TagLibLoadAttempted`. `FileMeta.Image` / `Exif` are the **nullable ME** caches with
  `ImagePropertiesLoadAttempted`.
- `TagLibFileAccess.Read` maps **audio overlay + media only** from one TagLib open.
  `EnsureTagLibLoaded` is the only TagLib hydrate path (filters, `media-*` / `mp3-*` tokens, Rename
  List `RenameListMetadataRequirement.TagLib`).
- `CommitExecutor` Apply is audio-only (`AudioTagPersistence` after optional
  `StripAllEmbeddedTagsOnCommit`). `ClearMetadataCaches` already clears TagLib + ME caches.
- `FilterGroup` has Case…Audio…Misc — **no Image**. Palette test
  `Groups_Cover_Every_FilterGroup_Enum_Value` requires a toolbar button for every enum value.
- ME **Jpeg Tag** columns already cover overlapping editorial keys (Title, Subject, Author,
  Keywords, Comments, Date/Time Taken, Description, Artist) as **original-only** EXIF.
- MFR7 **Image Tag** columns (`ImageTagPgInfo`) were never ported.
- `Formatter.md` defers `<imagetag-*>`. `docs/image-metadata-model.md` says TagLib Image Tag is a
  later slice.
- Fixtures: `Mfr.Tests/Fixtures/tiny.jpeg`, `tiny-exif.jpeg`, `tiny.png`, `tiny-animated.gif`.
- Sibling to copy: audio overlay → `SemanticAudioTag` → `*TagFields` patch →
  `AudioTagSetter` / `TagRemover` → `AudioTagRenameListFields`.

## Decisions (locked)

- **Field ownership:** TagLibSharp is the sole read/write owner for editable image-tag fields.
  MetadataExtractor stays read-only for `ImageProperties` and camera/makernote/`<exif-*>` — never
  feeds setters or editable preview columns.
- **No new library** for v1. If TagLib write quality fails later, replace the write stack; do not
  pair ME-read with TagLib-write for the same key.
- **v1 semantic fields:** Title, Subject, Description, Creator, Keywords, Comment, Copyright,
  DateTaken, Rating. Not camera EXIF (Make/Model/exposure/GPS/Orientation/Software).
- **Overlay shape:** Mirror audio. `ImageTagOverlay` holds optional **per-TagLib-type blocks** plus
  `ContainerFormat`. `SemanticImageTag` is a projection (`FromOverlay` / `MergeSemantic`). Setter
  broadcasts onto present blocks; empty overlay creates the container’s **recommended** block only
  (do not invent siblings). Remover nulls listed blocks. There are **no** mirrored scalars on the
  overlay type.
- **Load path:** Extend the existing TagLib open. `TagLibFileAccess.Read` also maps
  `ImageTagOverlay`. `EnsureTagLibLoaded` / `TagLibLoadAttempted` hydrate it. **Do not** add
  `EnsureImageTagsLoaded` or a second load flag. Image overlay is non-null on `FileMeta` like audio
  (`= new()`), cloned with the snapshot, reset in `ClearEmbeddedTagsCache`.
- **Apply:** Per-type field patch (Original→Preview diff). Never `tag.Clear()` + rebuild. Never write
  through TagLib `CombinedImageTag` / `file.Tag` façade once per-type writers exist (same “no dual
  write” rule as audio).
- **Unsupported / directories / corrupt:** **PreviewError**, same as audio — not a silent skip.
- **Keywords:** `;` via `DelimitedText` (audio convention).
- **DateTaken target string:** same display/parse as file timestamps
  (`RenameListFieldDisplay.FormatFileDate` / `RenameListFieldParse.ParseFileDate`). Overlay stores
  `DateTime?` with `DateTimeKind.Unspecified` (TagLib `ImageTag.DateTime`).
- **Rating:** TagLib `uint?`. Filter-target / setter text is a decimal integer; empty or `0` clears;
  non-numeric → PreviewError.
- **Filters:** Image Tag Setter + Image Tag Remover (new `FilterGroup.Image`, palette label
  “Image Tag”). DateTaken Shifter is P7, not v1 exit. **Do not** overload audio `TagRemover` UX or
  `StripAllEmbeddedTagsOnCommit`. Remover `all: true` uses a separate `StripAllImageTagsOnCommit`
  (TagLib `RemoveTags` of image types only, not audio blocks).
- **Columns (P6):** New Rename List **Image Tag** group with **only the nine v1 editable fields**,
  all `supportsPreview: true`. Not a 1:1 MFR7 `ImageTagPgInfo` port (that list is Keywords, Rating,
  DateTime, Orientation, Software, GPS, camera, Creator, Tag Types — no Title/Subject/Description/
  Comment/Copyright). Camera/GPS/Orientation/Software/Tag Types stay out of this group in v1; ME
  Jpeg Tag already has most camera fields. Do not mix ME and TagLib on one column key.
- **Dual read after P1:** Both libs may open the same file once TagLib image-tag load exists, but
  they must not feed the same UI key. P1 adds TagLib→overlay (tests/API); **UI stays ME-only until
  P6**. From P6 the UI can show both groups (Image Tag = TagLib overlay, Jpeg Tag / Image /
  `<exif-*>` = ME) with distinct names — values may disagree, documented.
- **JPEG recommended create:** `TiffIFD` (Windows Explorer EXIF/XP interop). PNG / GIF / TIFF
  recommended blocks come from TagLib allowed types (see policy table); P1 tests may amend the table
  if a v1 field cannot be written to that block.
- **Docs:** New `docs/image-tag-model.md` (audio-tag-model sibling) in P1–P2 as the write model
  lands. P6 updates `docs/image-metadata-model.md` and `Formatter.md` (`<imagetag-*>` is no longer
  “later”).

## Read paths by phase

| After     | Engine                                                             | Rename List / tokens UI                                                                                       |
| --------- | ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------- |
| **P1**    | ME as today + TagLib→audio overlay, media, **and** image overlay   | **Unchanged** — still ME Image / Jpeg Tag / `<image-*>` / `<exif-*>` only (TagLib hydrate still unused by UI) |
| **P2–P5** | Overlay may be mutated/Applied; ME still used for existing columns | Still no TagLib-backed columns; filter editors don’t show “current ME Title”                                  |
| **P6+**   | Same                                                               | **Image Tag** columns + `<imagetag-*>` from overlay; ME Jpeg Tag / `<exif-*>` remain separate                 |

## MFR7 reference brief

| Item     | Detail                                                                                                                                                                                                                                                                                                                                                                                  |
| -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Sources  | Help `basicimagefp.html`, `eximagefp.html`, `fp.html` (Image Tag group); `PropertyGroups/Image/*` (`JpegPGInfo`, `ImagePGInfo`, `ImageTagPgInfo`) all `PropertyType.ReadOnly`; no `[FilterInfo]` image/EXIF setters under `Filters/`                                                                                                                                                    |
| Behavior | Extract-only for rename tokens/columns (Format, dims, EXIF escape, Jpeg Tag, Image Tag, geo nearby). No commit write of image metadata via filters                                                                                                                                                                                                                                      |
| UX       | Formatter tokens + Rename List columns only                                                                                                                                                                                                                                                                                                                                             |
| Parity   | finebytes already matches read-only ME image/EXIF. **Image tag editing is net-new** (no MFR7 filter UX to copy). Closest sibling: Audio Tag Setter / Tag Remover. MFR7 Image Tag columns were TagLib `file.Tag` / `ImageTag` reflection (Keywords, Rating, DateTime, Orientation, Software, GPS, camera, Creator, TagTypes) — v1 columns are the nine editable semantic fields instead. |

## Non-goals

- Writing Make/Model/exposure/ISO/focal/GPS/Orientation/Software
- RAW/HEIF write; WebP write unless TagLib already works in tests
- Replacing MetadataExtractor
- Full IPTC/XMP schema editing beyond semantic projection
- Asserting ME `ExifData` equals TagLib after Apply
- DateTaken Shifter (scheduled as P7, not required to ship editing)
- Deprecating ME Jpeg Tag columns in the same ship as first Apply (optional follow-up)
- Porting the rest of MFR7 Image Tag columns (camera/GPS/Tag Types)
- Changing audio `TagRemover` nuclear `RemoveTags(AllTags)` (it can already strip image types if
  pointed at a JPEG; Image Tag Remover is the dedicated image UX)

## Container policy (P1 fills exact TagLib types)

Modeled blocks are whatever TagLib reports on disk for raster containers, typically:

| Container | Allowed blocks (TagLib)   | Recommended if empty                |
| --------- | ------------------------- | ----------------------------------- |
| JPEG      | TiffIFD, XMP, JpegComment | TiffIFD                             |
| PNG       | XMP, Png                  | Png (amend if tests fail v1 fields) |
| GIF       | XMP, GifComment           | GifComment                          |
| TIFF      | TiffIFD, XMP              | TiffIFD                             |

`IPTCIIM` only if TagLibSharp 2.3 exposes it on a v1 container (`TagTypesOnDisk`); otherwise omit
from the enum. Capability API lives in `Mfr.Models` (`ImageTagContainerPolicy`); detection in
`Mfr.Metadata` (`ImageTagContainerDetector`), same split as audio.

## Phases

### P1 — Overlay model + TagLib read

- **Scope / files:**
  - `Mfr.Models` — `ImageTagOverlay` (blocks + `ContainerFormat`), `SemanticImageTag` + field enum,
    `ImageTagContainerPolicy`, `ImageTagBlockKind`. Wire `FileMeta.ImageTagOverlay` (non-null,
    clone, equality). Extend `ClearEmbeddedTagsCache` / `SetEmbeddedTagOverlay` as needed so one
    TagLib hydrate sets audio + image overlays.
  - `Mfr.Metadata` — TagLib image read → overlay (JPEG/PNG/TIFF/GIF minimum); extend
    `TagLibFileAccess.Read` / `TagLibFileSnapshot` to include the image overlay; per-block
    `*TagFields` under `Mfr.Metadata` (image analog of `TagFields/`).
  - `Mfr.Filters` — `EnsureTagLibLoaded` already hydrates; no second ensure. Seeded tests keep
    `MarkTagLibLoadAttempted` so Ensure is a no-op.
  - `docs/image-tag-model.md` stub (principles + layer map; Apply details in P2).
  - **No UI / Rename List / token wiring** in this phase
- **Exit criteria:** Unit tests load a fixture JPEG/PNG and expose Title/Keywords/DateTaken (or
  null) from overlay without ME. App UI behavior identical to pre-P1. One TagLib open fills audio
  overlay, media, and image overlay.
- **Tests:** `Mfr.Tests` metadata/model read fixtures; empty overlay equality; container policy
  table.

### P2 — Field-patch Apply + commit

- **Scope / files:**
  - `Mfr.Metadata` — `ImageTagPersistence.Apply` (Original→Preview per-block diff; create
    recommended when Preview has a block Original lacked; no wholesale rebuild)
  - `Mfr.Engine` — `CommitExecutor` after audio Apply when image overlays differ; image strip flag
    later in P5. After commit, existing `ClearMetadataCaches` reloads both TagLib and ME.
- **Exit criteria:** Programmatic overlay mutate → Commit → TagLib re-read shows new values;
  unmodeled blobs preserved when possible. Unsupported format / directory → PreviewError (or commit
  skip only when the overlay never loaded / stayed default-empty — same as unchanged audio overlay).
- **Tests:** Apply round-trip JPEG (+ PNG); clear-field → absent; unsupported format PreviewError.

### P3 — Filter targets + string-filter path

- **Scope / files:**
  - `SemanticImageFieldTarget` + `IImageOverlayFilterTarget` (do **not** hang image targets off
    `IAudioOverlayFilterTarget`). `*TargetIo` for semantic image fields (mirror
    `AudioOverlayTargetIo`). JSON `targetType` discriminator.
  - `EnsureTargetReady` loads TagLib when the target is an image overlay field.
  - Filter Options Apply-To catalog (`FilterTargetCatalog`) grows an Image Tag group.
- **Exit criteria:** Formatter/Replacer with image Title target changes Preview overlay in
  filter-chain tests (no Image Tag Setter UI yet).
- **Tests:** Target get/set + one Formatter integration test.

### P4 — Image Tag Setter + editor

- **Scope / files:**
  - `Mfr.Filters/Image/` — `ImageTagSetter` (multi-field, templates, `onlyIfEmpty`; Keywords
    delimited)
  - Palette: add `FilterGroup.Image`, toolbar button + asset, `FilterPaletteViewModel` group list
  - `Mfr.Filters/docs/Image/`; `help/` page
  - App.Ui editor VM + AXAML under `FilterEditors/Image/` + `FilterOptionsEditorFactory` (mirror
    Audio Tag Setter)
- **Exit criteria:** User can set Title/Keywords/DateTaken via filter; overlay + commit persist.
  Rename List does **not** show Image Tag columns yet (P6) — verify via tests / debug overlay, not
  grid preview.
- **Tests:** Filter unit tests + editor VM tests; one headless smoke if the audio setter editor has
  that pattern.

### P5 — Image Tag Remover + editor

- **Scope / files:**
  - `ImageTagRemover` — checkboxes per `ImageTagBlockKind` and `all: true` →
    `StripAllImageTagsOnCommit` (do **not** overload audio `TagRemover`)
  - Editor checkboxes; docs
- **Exit criteria:** Remover clears selected blocks in overlay; commit removes those TagLib types;
  later Setter can recreate the recommended block.
- **Tests:** Remove XMP / TiffIFD / all; strip flag interaction with Apply (setter after strip).

### P6 — Rename List Image Tag columns + `<imagetag-*>` tokens

- **Scope / files:**
  - New Rename List field group `ImageTag` / label “Image Tag” (nine overlay-backed fields,
    `supportsPreview: true`, `RenameListMetadataRequirement.TagLib`)
  - Formatter tokens `<imagetag-*>` (deferred family in `Formatter.md`); map 1:1 to the nine fields.
    DateTaken may take an optional `:format` like `<exif-date>` if that stays small; otherwise one
    default formatted string.
  - Eager-load already follows TagLib requirement in `RenameListMetadataLoader` once columns are
    visible.
  - Docs: `image-metadata-model.md`, `Formatter.md`, `image-tag-model.md` UI section.
- **Exit criteria:** Visible Image Tag columns show Original/Preview from overlay; tokens expand
  from overlay; ME Jpeg Tag / `<exif-*>` unchanged and documented as separate.
- **Tests:** Column catalog + token + metadata-requirement tests.

### P7 — DateTaken Shifter (optional follow-up)

- **Scope / files:** Filter + editor shifting overlay `DateTaken` by ±delta (file `TimeShifter`
  analog).
- **Exit criteria:** Batch clock correction preview + Apply.
- **Tests:** Shift / clear / missing DateTaken.

## Implementation notes

- Round-trip tests: TagLib → overlay → Apply → TagLib. Never assert ME equality for editable keys.
- After Apply, clear both TagLib (audio+image overlays, media) and ME caches via existing
  `ClearMetadataCaches`.
- Public API docs + `_` private methods per `AGENTS.md`.
- Prefer sibling audio types for naming/layering (`docs/mfr-folder-layering.md`).
- Semantic write mapping (P1/P2 tables, not extra types): Title/Comment/Copyright from TagLib
  `Tag`; Creator/Keywords/DateTime/Rating from `ImageTag`; Subject/Description from the same
  blocks’ XP/IPTC/XMP keys TagLib already exposes — one owner per semantic field, documented in
  `image-tag-model.md`.
