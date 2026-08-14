---
title: Image metadata model
description: How Mfr lazily reads raster image properties via MetadataExtractor for formatter tokens.
---

# Image metadata model

How Magic File Renamer reads raster image facts (dimensions, bit depth, DPI, frame count, format)
for `<image-*>` formatter tokens: a lazy MetadataExtractor open, a mapped `ImageProperties` snapshot
on `FileMeta`, and empty vs PreviewError rules.

Product/UI sketches live in [magic-file-renamer-design.md](magic-file-renamer-design.md) (§7.7).
Folder layering is in [mfr-folder-layering.md](mfr-folder-layering.md). TagLib photo fields
(`<media-photo-width>` / `<media-photo-height>`) are a separate cache; see media tokens in
[Formatter.md](../Mfr.Filters/docs/Formatting/Formatter.md).

```mermaid
flowchart LR
  token["image-* token"] --> ensure["EnsureImagePropertiesLoaded"]
  ensure --> reader["ImagePropertiesReader.Read"]
  reader --> me["MetadataExtractor.ImageMetadataReader"]
  me --> map["Map raster directories"]
  map --> cache["FileMeta.Image on Original and Preview"]
  cache --> format["ImagePropertiesFormatting"]
```

## Design principles

1. **Read-only.** Image properties are never written on commit. There is no Apply path in this slice.
2. **Lazy, one open.** The first `<image-*>` token on a file row in a Preview run opens the file once,
   maps directories to `ImageProperties`, and caches the record. Later tokens in the same cycle reuse it.
   Commit clears the cache so the next preview reloads from disk.
3. **DTO only.** Raw MetadataExtractor `Directory` lists are not stored on `FileMeta`. Mapping discards them.
   A later EXIF slice can map from the same in-memory directory list during that single open.
4. **Original snapshot.** Tokens read `item.Original.Image` (disk-backed facts), not Preview.
5. **Mapped rasters only.** Empty tokens apply only after a successful allowlist map when a field is
   missing (`0` / null). Anything that is not JPEG, PNG, GIF, BMP, TIFF, ICO, or WebP is PreviewError —
   including types MetadataExtractor will open (MP3, WAV, MP4, HEIF, RAW, …).
6. **Separate from TagLib.** `<media-photo-*>` stays on the TagLib `FileMeta.Media` cache. `<image-*>`
   values may differ from TagLib/GDI+.

## Layer map

| Concern | Project / type |
|---|---|
| Snapshot record | `Mfr.Models` — `ImageProperties` on `FileMeta.Image` |
| Disk read / raster map | `Mfr.Metadata` — `ImagePropertiesReader` |
| Lazy load | `Mfr.Filters` — `RenameItemImagePropertiesExtensions.EnsureImagePropertiesLoaded` |
| Tokens | `Mfr.Filters` — `ImagePropertyTokenBase` and seven `image-*` tokens |
| Commit cache clear | `Mfr.Engine` — `RenameList.Commit` calls `ClearMetadataCaches` |

## Cache lifetime

`FileMeta.Image` is `null` until the first image token load. `RenameItem.SetImageProperties` assigns the
same record reference to `Original.Image` and `Preview.Image` (clone shares it the same way as `Media`).

`FilterTestHelpers.CreateRenameItem` marks image load as already attempted on file rows so seeded unit
tests never hit disk. Integration-style tests construct an unmarked `RenameItem` pointing at a real file.

## Empty vs PreviewError

| Situation | Result |
|---|---|
| Directory row | `InvalidOperationException` from ensure → PreviewError |
| Missing / relative path | `ArgumentException` from the reader → PreviewError |
| Unknown format / ME processing or IO failure (e.g. `.txt`) | Propagated ME exception → PreviewError |
| ME opens a non-allowlist type (MP3, WAV, MP4, …) | `InvalidOperationException` naming the type → PreviewError |
| Mapped raster, missing field (no DPI, WebP bit depth, `0` dims) | That token expands **empty**, not an error |

## Mapped fields (5a)

Format-native directories first. EXIF IFD is not used for width/height except TIFF (IFD0). DPI may use
JFIF, then EXIF IFD0, then PNG pHYs, then BMP pixels/metre, converted to dots per inch. Bit depth is
total bits per pixel where possible (typical JPEG `8×3 = 24`), not JPEG sample precision alone.

Frame count: GIF/ICO count per-image directories; TIFF counts dimension-bearing IFD0s (not thumbnails);
JPEG/PNG/BMP/WebP are `1` when width or height is known.
