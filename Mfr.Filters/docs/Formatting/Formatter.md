# Formatter

Replaces the **entire target segment** with the result of expanding a **template string**. Placeholders use angle brackets: `<token>` or `<name:arguments>`. The previous target value is ignored; an empty `template` clears the segment.

Unlike [Inserter](Inserter.md) / [AudioTagSetter](../Audio/AudioTagSetter.md), Formatter **always** compiles `template` as a format string (no “looks like a token” heuristic). Literals and tokens may be mixed freely.

## Options

| Property   | Type   | Description                        |
| ---------- | ------ | ---------------------------------- |
| `template` | string | Output text; see **Tokens** below. |

### Target (`target`, string filters)

Along with path and file-name targets ([preset shape](../README.md#preset-shape)), string filters accept:

- **`AudioField`**: set **`field`** to a camelCase overlay name (`title`, `album`, `performers`, `albumArtists`, `composers`, `genre`, `comment`, `lyrics`, `copyright`, `grouping`, `year`, `track`, `trackCount`, `disc`, `discCount`). Reads/writes the generic semantic projection (broadcast to present blocks; recommended create when empty). Numeric fields use decimal-digit strings; empty clears.
- **`Id3v1Field`**: set **`field`** to `title` / `artist` / `album` / `year` / `comment` / `track` / `genre`. MPEG only; unsupported containers → preview error.
- **`Id3v2Frame`**: set **`frameId`** (e.g. `TIT2`, `COMM`); optional **`language`** / **`description`** for multi-instance frames. Omit both for primary `COMM`/`USLT`. MPEG only.
- **`XiphField`**: set **`key`** (e.g. `TITLE`, `ARTIST`; case-insensitive). FLAC/Ogg only.

**Commit:** After a successful move, when **`Preview.AudioTagOverlay`** differs from the row’s **`Original.AudioTagOverlay`**, **`RenameList.Commit`** calls **`AudioTagPersistence.Apply`** on the destination file (which reads the file’s current TagLib state as the merge baseline, then writes). Rows with unchanged tag overlays skip this path.

### Tokens

#### File name

| Token                         | Output                                                                                                                                    |
| ----------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `<file-name>`                 | Preview prefix (no extension).                                                                                                            |
| `<file-extension>` or `<ext>` | Preview extension (with dot).                                                                                                             |
| `<full-name>`                 | Preview prefix + extension.                                                                                                               |
| `<parent-folder>`             | Name of the immediate parent folder (level 1) from preview directory.                                                                     |
| `<parent-folder:level>`       | Ancestor folder name at the given level (1 = immediate parent, 2 = grandparent, …). Returns empty when level exceeds path depth.          |
| `<full-path>`                 | Preview full path of the file.                                                                                                            |
| `<file-or-folder>`            | `File` or `Folder` from original `Attributes.IsDirectory` (read-only; no Apply target).                                                   |
| `<file-name-numeric-value>`   | First contiguous digit run in the preview full name (prefix + extension; leading zeros stripped); `0` when none. Useful for numeric sort. |
| `<file-name-length>`          | Character length of the preview full name (prefix + extension).                                                                           |
| `<full-path-length>`          | Character length of the preview full path.                                                                                                |

#### Audio tags (canonical overlay)

Reads from **`Preview.AudioTagOverlay`**. Tag-backed fields load from disk (**`TagLibFileReader.Read`**) **on first `audio-*` / `id3v2` / `id3v2-version` token use** (or first `media-*` / `mpeg-*` token, which shares that open) for that **file** row inside a **`Preview`** run; the same open also fills the media cache when it is not already marked loaded. **`RenameList.Commit`** clears cached overlays afterward so later previews reload from disk. **Directory rows** or **unsupported / unreadable** embedded metadata cause **`RenameStatus.PreviewError`** on that row; when TagLib or the reader throws, the surfaced **`RenameItem`** **`PreviewError`** entry keeps that exception as **`Cause`**.

**Contrast:** file-name and audio tokens both use **`Preview`** so later filters in the chain see mutated names/tags before a formatter runs. Disk-backed read-only facts (`media-*`, `mpeg-*`, `image-*`, `file-size`, dates, drive/label/count) still read **`Original`**.

Unit tests typically construct **`RenameItem`** via **`FilterTestHelpers.CreateRenameItem`**, which marks TagLib load as already attempted (`TagLibLoadAttempted`) so **`EnsureTagLibLoaded`** does not touch pre-seeded **`AudioTagOverlay`**; integration-style tests use real tagged temp files when exercising disk read.

| Token                          | Output                                                         |
| ------------------------------ | -------------------------------------------------------------- |
| `<audio-title>`                | Title; empty when unset.                                       |
| `<audio-artist>`               | Performers (joined overlay string); empty when unset.          |
| `<audio-album-artist>`         | Album artists; empty when unset.                               |
| `<audio-album>`                | Album; empty when unset.                                       |
| `<audio-year>`                 | Year as decimal digits; empty when unset.                      |
| `<audio-genre>`                | Genre; empty when unset.                                       |
| `<audio-track>`                | Track number; decimal digits; empty when unset.                |
| `<audio-track-count>`          | Track count (of *n/m* pair); decimal digits; empty when unset. |
| `<audio-disc>`                 | Disc number; decimal digits; empty when unset.                 |
| `<audio-disc-count>`           | Disc count; decimal digits; empty when unset.                  |
| `<audio-comment>`              | Comment; empty when unset.                                     |
| `<audio-composer>`             | Composers; empty when unset.                                   |
| `<audio-lyrics>`               | Lyrics text; empty when unset.                                 |
| `<audio-copyright>`            | Copyright; empty when unset.                                   |
| `<audio-grouping>`             | Grouping; empty when unset.                                    |
| `<audio-bpm>`                  | Beats per minute as decimal digits; empty when unset.          |
| `<audio-conductor>`            | Conductor; empty when unset.                                   |
| `<audio-mb-artist-id>`         | MusicBrainz artist ID; empty when unset.                       |
| `<audio-mb-release-id>`        | MusicBrainz release (album) ID; empty when unset.              |
| `<audio-mb-release-artist-id>` | MusicBrainz release artist ID; empty when unset.               |
| `<audio-mb-track-id>`          | MusicBrainz track ID; empty when unset.                        |
| `<audio-mb-disc-id>`           | MusicBrainz disc ID; empty when unset.                         |
| `<audio-mb-release-status>`    | MusicBrainz release status; empty when unset.                  |
| `<audio-mb-release-type>`      | MusicBrainz release type; empty when unset.                    |
| `<audio-mb-release-country>`   | MusicBrainz release country; empty when unset.                 |
| `<audio-musicip-id>`           | MusicIP PUID; empty when unset.                                |
| `<audio-amazon-id>`            | Amazon ASIN; empty when unset.                                 |

**Arguments:** Unlike some design-draft examples (**`:0`** suffixes), these tokens accept **no** argument (`<audio-title>` only). A stray **`<audio-title:…>`** fails at compile with a formatter error listing the token name.

**Rows without readable tag data (real `RenameList` rows):** Matches the disk-read rule above: **directory rows** cannot load tags (`InvalidOperationException`), and **file rows** where TagLib cannot open or read embedded metadata (typical plain text or other non-tagged files) surface **`RenameStatus.PreviewError`** with the exception as **`Cause`**. There is no “empty token” fallback on that path.

**Unit tests:** **`FilterTestHelpers.CreateRenameItem`** marks TagLib load as already attempted, so **`EnsureTagLibLoaded`** is skipped and the overlay stays at its initial state (usually the default empty overlay), meaning **`<audio-*>`** tokens expand to **empty** strings without touching disk.

Stream properties (duration, bitrate, channels, …) are under **Media properties** (`<media-*>`) and **MPEG audio properties** (`<mpeg-*>`), not `<audio-*>`.

#### Media properties

Reads from **`Original.Media`** (read-only TagLib cache). Properties load from disk (**`TagLibFileReader.Read`**) **on first `media-*` or `mpeg-*` token use** (or first `audio-*` / tag-filter load, which shares that open) for that **file** row inside a **`Preview`** run (one TagLib open; MPEG header nested on **`Media.Mpeg`** when present). The same open also fills embedded-tag overlays when they are not already marked loaded. **`RenameList.Commit`** clears the cache afterward so later previews reload from disk. **Directory rows** or files TagLib cannot open surface **`RenameStatus.PreviewError`** (exception as **`Cause`**), same policy as audio tags. Wrong stream kind (e.g. video width on a pure MP3) expands to **empty**, not an error.

Unit tests via **`FilterTestHelpers.CreateRenameItem`** mark stream properties as already loaded so seeded **`FileMeta.Media`** (including nested **`Media.Mpeg`**) is used without disk I/O.

| Token                     | Output                                                         |
| ------------------------- | -------------------------------------------------------------- |
| `<media-mime>`            | MIME type; empty when unset.                                   |
| `<media-corrupt>`         | `Yes` or `No`.                                                 |
| `<media-duration>`        | Duration as `h:mm:ss` (total hours unpadded); empty when zero. |
| `<media-duration-sec>`    | Whole seconds (floor); empty when zero.                        |
| `<media-types>`           | TagLib media-type flags text (e.g. `Audio`); empty when none.  |
| `<media-description>`     | Codec description; empty when unset.                           |
| `<media-audio-bitrate>`   | Audio bitrate (kbps); empty when `0`.                          |
| `<media-samplerate>`      | Sample rate (Hz); empty when `0`.                              |
| `<media-bits-per-sample>` | Bits per sample; empty when `0`.                               |
| `<media-channels>`        | Channel count; empty when `0`.                                 |
| `<media-video-width>`     | Video width (px); empty when `0`.                              |
| `<media-video-height>`    | Video height (px); empty when `0`.                             |
| `<media-photo-width>`     | Photo width (px); empty when `0`.                              |
| `<media-photo-height>`    | Photo height (px); empty when `0`.                             |
| `<media-photo-quality>`   | Photo quality; empty when `0`.                                 |

**Arguments:** No argument (`<media-mime>` only). A stray **`<media-mime:…>`** fails at compile.

#### MPEG audio properties

Reads from **`Original.Media.Mpeg`** (nested read-only TagLib `Mpeg.AudioHeader` on the media cache). Loaded by the same **`TagLibFileReader.Read`** path as media properties (and as audio tags when that family loads first). Files without an MPEG audio header (e.g. WAV/FLAC/AAC) leave **`Media.Mpeg`** null and expand tokens to **empty** (not PreviewError). Replaces MFR7’s legacy **`mp3-*`** names.

| Token                 | Output                                                                                 |
| --------------------- | -------------------------------------------------------------------------------------- |
| `<mpeg-bitrate>`      | Bitrate (kbps); prefixed `VBR` when Xing/VBRI present (e.g. `VBR128`); empty when `0`. |
| `<mpeg-copyright>`    | `Yes` or `No`.                                                                         |
| `<mpeg-duration>`     | Header duration as `h:mm:ss`; empty when zero.                                         |
| `<mpeg-duration-sec>` | Whole seconds (floor); empty when zero.                                                |
| `<mpeg-encoding>`     | `CBR` or `VBR`; empty when no MPEG header.                                             |
| `<mpeg-frequency>`    | Sample rate (Hz); empty when `0`.                                                      |
| `<mpeg-layer>`        | `I`, `II`, or `III`; empty when unset.                                                 |
| `<mpeg-ver>`          | MPEG version (`1`, `2`, or `2.5`); empty when unknown.                                 |
| `<mpeg-mode>`         | Channel mode (`Stereo`, `JointStereo`, `DualChannel`, `SingleChannel`).                |
| `<mpeg-original>`     | `Yes` or `No`.                                                                         |
| `<mpeg-protection>`   | `Yes` or `No` (CRC protection bit).                                                    |

**Arguments:** No argument (`<mpeg-bitrate>` only). A stray **`<mpeg-bitrate:…>`** fails at compile.

#### Image properties

Reads from **`Original.Image`** (read-only MetadataExtractor raster cache). Properties load from disk (**`ImageFileReader.Read`**) **on first `image-*` or `exif-*` token use** for that **file** row inside a **`Preview`** run (one MetadataExtractor open fills **`Image`** and **`Exif`**); **`RenameList.Commit`** clears the cache afterward so later previews reload from disk.

**Directory rows**, files whose format cannot be determined (typical `.txt`), and files that are **not a mapped raster** surface **`RenameStatus.PreviewError`** (exception as **`Cause`**). Mapped rasters are JPEG, PNG, GIF, BMP, TIFF, ICO, and WebP. MetadataExtractor **does** open MP3/WAV (and other audio/video), but **`image-*` still errors** on those types. A missing field on a mapped raster (no DPI, WebP bit depth, `0` dimensions) expands **empty**, not an error.

Keep **`<media-photo-width>`** / **`<media-photo-height>`** for TagLib photo dims; values may differ from **`<image-width>`** / **`<image-height>`**.

Unit tests via **`FilterTestHelpers.CreateRenameItem`** mark image properties as already loaded so seeded **`FileMeta.Image`** is used without disk I/O.

| Token                 | Output                                                                                                    |
| --------------------- | --------------------------------------------------------------------------------------------------------- |
| `<image-width>`       | Width (px); empty when `0`.                                                                               |
| `<image-height>`      | Height (px); empty when `0`.                                                                              |
| `<image-bit-depth>`   | Total bits per pixel; empty when `0`.                                                                     |
| `<image-format>`      | MetadataExtractor short type name (`JPEG`, `PNG`, `GIF`, `TIFF`, `BMP`, `ICO`, `WebP`); empty when unset. |
| `<image-horz-res>`    | Horizontal DPI; whole numbers without a decimal (`96`); otherwise general (`72.009`); empty when `≤ 0`.   |
| `<image-vert-res>`    | Vertical DPI; same formatting as horz-res; empty when `≤ 0`.                                              |
| `<image-frame-count>` | Frame count; empty when `0`. Stills with known dims are `1`.                                              |

**Arguments:** No argument (`<image-width>` only). A stray **`<image-width:…>`** fails at compile.

#### EXIF

Reads from **`Original.Exif`** (read-only MetadataExtractor EXIF cache). The same **`EnsureImagePropertiesLoaded`** path as **`image-*`** fills both caches on first **`image-*`** or **`exif-*`** token use; **`RenameList.Commit`** clears them. Text/camera fields are MetadataExtractor **`GetDescription`** strings (then `\n` → space, trim). **`DateTaken`** is SubIFD DateTimeOriginal via **`TryGetDateTime`** only (no DateTimeDigitized / IFD0 DateTime fallback).

**Directory rows**, files whose format cannot be determined (typical `.txt`), and files that are **not a mapped raster** surface **`RenameStatus.PreviewError`** — including MP3/WAV. A mapped raster with no EXIF (or a missing field) expands **empty**, not an error. PNG/TIFF/WebP with EXIF work; the allowlist is the same as **`image-*`**.

Keep later **`<imagetag-*>`** (TagLib Image Tag) separate; values may differ from **`<exif-*>`**. Typed GPS lat/lon and **`<geo-*>`** are not in this slice.

Unit tests via **`FilterTestHelpers.CreateRenameItem`** mark image/EXIF load as already attempted so seeded **`FileMeta.Exif`** is used without disk I/O.

| Token                | Output                                                                                                           |
| -------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `<exif-make>`        | Camera manufacturer; empty when unset.                                                                           |
| `<exif-model>`       | Camera model; empty when unset.                                                                                  |
| `<exif-exposure>`    | Exposure time description (e.g. `1/60 sec`); empty when unset.                                                   |
| `<exif-fnumber>`     | F-number description (e.g. `f/8.0`); empty when unset.                                                           |
| `<exif-iso>`         | ISO speed description; empty when unset.                                                                         |
| `<exif-focal>`       | Focal length description (e.g. `50 mm`); empty when unset.                                                       |
| `<exif-focal-35>`    | 35mm-equivalent focal length; empty when unset.                                                                  |
| `<exif-date:format>` | DateTaken with a .NET date format string (InvariantCulture); empty when unset.                                   |
| `<exif:source,name>` | Extended tag by directory alias and tag name or decimal id; empty when missing. Example: `<exif:ExifSub,36867>`. |

**Arguments:** No-arg tokens (`<exif-make>` only) reject a stray **`<exif-make:…>`** at compile. **`<exif-date>`** requires a non-empty format string (the pattern is not validated). **`<exif>`** requires **`source,name`** split on the first comma; both parts non-empty; **`source`** must be a known alias (`Exif`, `ExifSub`, `GPS`, `IPTC`, `Canon`, `Casio`, `FujiFilm`, `Nikon`, `Olympus`, `Interop`, or `Thumb`). Unknown tag names expand empty.

#### ID3v2 Custom Field (MFR7 `<id3v2:…>`)

Reads one **modeled ID3v2 frame** from **`Preview.AudioTagOverlay.Id3v2`** (same lazy load rules as `<audio-*>`). Unlike semantic `<audio-*>` tokens, this is **ID3v2-only** (other containers yield empty when no Id3v2 block is present).

| Token                  | Output                                                                                          |
| ---------------------- | ----------------------------------------------------------------------------------------------- |
| `<id3v2:TALB>`         | Album frame text; empty when unset.                                                             |
| `<id3v2:TIT2>`         | Title frame text; empty when unset.                                                             |
| `<id3v2:TXXX>`         | First `TXXX` frame in overlay order (MFR7-compatible; no content-descriptor picker).            |
| `<id3v2:TXXX:catalog>` | `TXXX` whose content descriptor is `catalog`; empty when no match.                              |
| `<id3v2:COMM>`         | Primary comment (empty content descriptor); empty when unset.                                   |
| `<id3v2:COMM:other>`   | Comment whose content descriptor is `other`.                                                    |
| `<id3v2:USLT>`         | Primary unsynced lyrics; optional content-descriptor suffix like `COMM`.                        |
| `<id3v2-version>`      | Tag version as `2.3` / `2.4` / … from the overlay block; empty when no ID3v2 tag. No arguments. |

**Arguments:** For `<id3v2:…>`, `field-code` is **required** (the four-character frame id, case-insensitive). Multi-instance frames (`TXXX` / `COMM` / `USLT`) may append a content-descriptor suffix after the id. Singleton frames reject that suffix at compile time. Multi-value text on one frame is joined with `; `. `TRCK` / `TPOS` return the full frame text (e.g. `3/12`); use `<audio-track>` / `<audio-track-count>` when a split is needed. `<id3v2-version>` accepts **no** argument.

#### File properties

| Token                          | Output                                                                                                                                                                                                                                                                                  |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `<file-date:format,date-kind>` | Arguments are **required**. **Both** parts: a .NET date format string, then a comma, then `date-kind`. Split uses the **last** comma so `format` may contain commas. `date-kind` (case-insensitive): `creation`, `lastWrite`, `lastAccess`. Example: `<file-date:dd-MM-yyyy,creation>`. |
| `<drive-letter>`               | Drive letter of the file's location (e.g. `C:`). Returns `$` for network (UNC) paths.                                                                                                                                                                                                   |
| `<label>`                      | Volume label of the drive that holds the file.                                                                                                                                                                                                                                          |
| `<file-count>`                 | Non-recursive file count (subfolders ignored). For a folder item: files inside that folder. For a file item: files in its parent directory. Empty when the counted directory does not exist.                                                                                            |
| `<file-size>`                  | File size, auto-selecting the largest unit (e.g. `1 KB`, `2 MB`).                                                                                                                                                                                                                       |
| `<file-size:unit>`             | File size in a specific unit. `unit`: omit or `auto` (auto-scale), `b`/`bytes`, `kb`, `mb`, `gb` (case-insensitive).                                                                                                                                                                    |
| `<file-size:unit,decimals>`    | File size with the specified number of decimal places (default `0`).                                                                                                                                                                                                                    |

#### Counter and time

| Token                                                        | Output                                                                                                                                                                                                                                                                                                                                                                        |
| ------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `<now>`                                                      | Current UTC time, ISO-8601 style.                                                                                                                                                                                                                                                                                                                                             |
| `<now:format>`                                               | Current UTC time formatted with a .NET format string.                                                                                                                                                                                                                                                                                                                         |
| `<item-count>`                                               | Total items in the current rename list (no arguments). Populated during preview/commit.                                                                                                                                                                                                                                                                                       |
| `<random-char:low,high>`                                     | One random character, uniformly chosen between inclusive endpoints (first character of each side is used; order may be reversed). Examples: `<random-char:A,Z>`, `<random-char:0,9>`.                                                                                                                                                                                         |
| `<counter>`                                                  | Rename-list index as `initial` + `step`×index using defaults `initial=1`, `step=1`, `padding=none`, `length=2`, `resetScope=global`. With `padding=none`, output has no leading zeros.                                                                                                                                                                                        |
| `<counter:initial=…,step=…,padding=…,length=…,resetScope=…>` | Named options, **order-independent** (spaces around `,` and `=` optional). Omitted options use `initial=1`, `step=1`, `padding=none`, `length=2`, `resetScope=global`. Value: `initial` + `step`×index. `padding`: `none`, `auto` (width from list scope), or `fixed` (pad to `length`, minimum digit width `1`). `resetScope`: `global` vs `perFolder` (restart per folder). |

#### Token extraction

| Token                                                                    | Output                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `<token:tokenNumber=…,separator=…,includeNext=…,includePrev=…,source=…>` | **Named** options, **order-independent** (spaces optional). Resolves `source`, splits by `separator`, then returns the 1-based `tokenNumber` part. With `includeNext=true`, returns that part through the end (rejoined with `separator`); with `includePrev=true`, returns from the start through that part (rejoined); both `true` returns the full resolved string. `source` may contain nested `<…>` tokens; commas inside balanced angle brackets are not option separators. `includeNext` / `includePrev`: `true` / `false` (case-insensitive). |
| `<substr:start=…,end=…,source=…>`                                        | Named options, **order-independent** (spaces optional). Extracts characters from `source` between two positions (inclusive). Positions are 1-based; negative positions count from the right (`-1` = last character). Out-of-range positions are clamped to the nearest boundary. When the resolved start exceeds the resolved end, the range `(end, start]` is returned. `source` may contain nested `<…>` tokens; commas inside balanced angle brackets are not option separators.                                                                   |

Unknown token names fail when the filter is set up (compile / preview), not left as literals. For embedded name lists, use the [NameList](NameList.md) filter (there is no `<name-list-entry>` token).

## Examples

Assume directory `Music\My Album\` when using `<parent-folder>`. Counter rows use the **global** index passed to the filter.

| Options                                                                                                    | Before                                   | After                               | Comment                                                                       |
| ---------------------------------------------------------------------------------------------------------- | ---------------------------------------- | ----------------------------------- | ----------------------------------------------------------------------------- |
| `template`: `"<file-name>"`                                                                                | `song`                                   | `song`                              |                                                                               |
| `template`: `"<parent-folder>"`<br>file under `Music\My Album\`                                            | `ignored`                                | `My Album`                          |                                                                               |
| `template`: `"<parent-folder:2>"`<br>file under `Music\My Album\`                                          | `ignored`                                | `Music`                             | Level 2 = grandparent.                                                        |
| `template`: `"<file-date:dd-MM-yyyy,creation>"`                                                            | `ignored`                                | `07-04-2023`                        | Creation time, common date layout.                                            |
| `template`: `"<file-date:yyyy,creation>"`                                                                  | `ignored`                                | `2024`                              | Creation year.                                                                |
| `template`: `"<file-date:yyyy,lastWrite>"`                                                                 | `ignored`                                | `2021`                              | Last-write year.                                                              |
| `template`: `"<file-size>"`                                                                                | `ignored`                                | `1 KB`                              | Auto unit, 0 decimals.                                                        |
| `template`: `"<file-size:mb,2>"`                                                                           | `ignored`                                | `1.50 MB`                           | MB, 2 decimal places.                                                         |
| `template`: `"<drive-letter>"`                                                                             | `ignored`                                | `C:`                                | Drive letter of the file.                                                     |
| `template`: `"<counter:initial=10,step=2,padding=fixed,length=4,resetScope=global>"`<br>global index: `3`  | `ignored`                                | `0016`                              | `10 + 2×3`, fixed width `4`.                                                  |
| `template`: `"<token:tokenNumber=1,separator=-,includeNext=false,includePrev=false,source=<full-name>>"`   | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `13_`                               | Track number prefix, split by `-`.                                            |
| `template`: `"<token:tokenNumber=2,separator=_-_,includeNext=false,includePrev=false,source=<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `Smog`                              | Artist name, split by `_-_`.                                                  |
| `template`: `"<token:tokenNumber=2,separator=_-_,includeNext=true,includePrev=false,source=<full-name>>"`  | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `Smog_-_Cold_Blooded_Old_Times.mp3` | Artist and title, include-next.                                               |
| `template`: `"<substr:start=1,end=5,source=<file-name>>"`                                                  | `MyTestFileName.123`                     | `MyTes`                             | First 5 chars of prefix.                                                      |
| `template`: `"<substr:start=5,end=-6,source=<full-name>>"`                                                 | `MyTestFileName.123`                     | `stFileNam`                         | Positive start, negative end.                                                 |
| `template`: `"<substr:start=-1,end=2,source=<file-extension>45>"`                                          | `MyTestFileName.123`                     | `2345`                              | Crossed positions: extension `.123` + literal `45` → `.12345`; range `(2,6]`. |

For sequential numbering without a full template, see [Counter](Counter.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). When targeting **Prefix**, `<file-name>` is the **preview** file prefix (same as original until an earlier filter mutates it).

```json
{
  "type": "Formatter",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "template": "<parent-folder> - <file-name>"
  }
}
```
