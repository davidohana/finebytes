# Formatter

Replaces the **entire target segment** with the result of expanding a **template string**. Placeholders use angle brackets: `<token>` or `<name:arguments>`.

## Options

| Property | Type | Description |
|----------|------|-------------|
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

| Token | Output |
|--------|--------|
| `<file-name>` | Original prefix (no extension). |
| `<file-extension>` or `<ext>` | Original extension (with dot). |
| `<full-name>` | Prefix + extension. |
| `<parent-folder>` | Name of the immediate parent folder (level 1). |
| `<parent-folder:level>` | Ancestor folder name at the given level (1 = immediate parent, 2 = grandparent, …). Returns empty when level exceeds path depth. |
| `<full-path>` | Full path of the file. |

#### Audio tags (canonical overlay)

Reads from **`Preview.AudioTagOverlay`**. Tag-backed fields load from disk (**`AudioTagPersistence.Read`**) **on first `audio-*` / `id3v2` token use** for that **file** row inside a **`Preview`** run; **`RenameList.Commit`** clears cached overlays afterward so later previews reload from disk. **Directory rows** or **unsupported / unreadable** embedded metadata cause **`RenameStatus.PreviewError`** on that row; when TagLib or the reader throws, the surfaced **`RenameItem`** **`PreviewError`** entry keeps that exception as **`Cause`**.

**Contrast:** file-name tokens use **`Original`** paths; audio tokens deliberately use **preview** so later filters can mutate tags before a formatter runs.

Unit tests typically construct **`RenameItem`** via **`FilterTestHelpers.CreateRenameItem`**, which marks embedded tags as already loaded so **`EnsureEmbeddedTagsLoaded`** does not touch pre-seeded **`AudioTagOverlay`**; integration-style tests use real tagged temp files when exercising disk read.

| Token | Output |
|--------|--------|
| `<audio-title>` | Title; empty when unset. |
| `<audio-artist>` | Performers (joined overlay string); empty when unset. |
| `<audio-album-artist>` | Album artists; empty when unset. |
| `<audio-album>` | Album; empty when unset. |
| `<audio-year>` | Year as decimal digits; empty when unset. |
| `<audio-genre>` | Genre; empty when unset. |
| `<audio-track>` | Track number; decimal digits; empty when unset. |
| `<audio-track-count>` | Track count (of *n/m* pair); decimal digits; empty when unset. |
| `<audio-disc>` | Disc number; decimal digits; empty when unset. |
| `<audio-disc-count>` | Disc count; decimal digits; empty when unset. |
| `<audio-comment>` | Comment; empty when unset. |
| `<audio-composer>` | Composers; empty when unset. |
| `<audio-lyrics>` | Lyrics text; empty when unset. |
| `<audio-copyright>` | Copyright; empty when unset. |
| `<audio-grouping>` | Grouping; empty when unset. |
| `<audio-bpm>` | Beats per minute as decimal digits; empty when unset. |
| `<audio-conductor>` | Conductor; empty when unset. |
| `<audio-mb-artist-id>` | MusicBrainz artist ID; empty when unset. |
| `<audio-mb-release-id>` | MusicBrainz release (album) ID; empty when unset. |
| `<audio-mb-release-artist-id>` | MusicBrainz release artist ID; empty when unset. |
| `<audio-mb-track-id>` | MusicBrainz track ID; empty when unset. |
| `<audio-mb-disc-id>` | MusicBrainz disc ID; empty when unset. |
| `<audio-mb-release-status>` | MusicBrainz release status; empty when unset. |
| `<audio-mb-release-type>` | MusicBrainz release type; empty when unset. |
| `<audio-mb-release-country>` | MusicBrainz release country; empty when unset. |
| `<audio-musicip-id>` | MusicIP PUID; empty when unset. |
| `<audio-amazon-id>` | Amazon ASIN; empty when unset. |

**Arguments:** Unlike some design-draft examples (**`:0`** suffixes), these tokens accept **no** argument (`<audio-title>` only). A stray **`<audio-title:…>`** fails at compile with a formatter error listing the token name.

**Rows without readable tag data (real `RenameList` rows):** Matches the disk-read rule above: **directory rows** cannot load tags (`InvalidOperationException`), and **file rows** where TagLib cannot open or read embedded metadata (typical plain text or other non-tagged files) surface **`RenameStatus.PreviewError`** with the exception as **`Cause`**. There is no “empty token” fallback on that path.

**Unit tests:** **`FilterTestHelpers.CreateRenameItem`** marks embedded tags as already loaded, so **`EnsureEmbeddedTagsLoaded`** is skipped and the overlay stays at its initial state (usually the default empty overlay), meaning **`<audio-*>`** tokens expand to **empty** strings without touching disk.

Stream properties (duration, bitrate, channels, …) are under **Media properties** (`<media-*>`) and **MPEG audio properties** (`<mpeg-*>`), not `<audio-*>`.

#### Media properties

Reads from **`Original.Media`** (read-only TagLib cache). Properties load from disk (**`MediaPropertiesReader.Read`**) **on first `media-*` or `mpeg-*` token use** for that **file** row inside a **`Preview`** run (one TagLib open; MPEG header nested on **`Media.Mpeg`** when present); **`RenameList.Commit`** clears the cache afterward so later previews reload from disk. **Directory rows** or files TagLib cannot open surface **`RenameStatus.PreviewError`** (exception as **`Cause`**), same policy as audio tags. Wrong stream kind (e.g. video width on a pure MP3) expands to **empty**, not an error.

Unit tests via **`FilterTestHelpers.CreateRenameItem`** mark stream properties as already loaded so seeded **`FileMeta.Media`** (including nested **`Media.Mpeg`**) is used without disk I/O.

| Token | Output |
|--------|--------|
| `<media-mime>` | MIME type; empty when unset. |
| `<media-corrupt>` | `True` or `False` (invariant). |
| `<media-duration>` | Duration as `h:mm:ss` (total hours unpadded); empty when zero. |
| `<media-duration-sec>` | Whole seconds (floor); empty when zero. |
| `<media-types>` | TagLib media-type flags text (e.g. `Audio`); empty when none. |
| `<media-description>` | Codec description; empty when unset. |
| `<media-audio-bitrate>` | Audio bitrate (kbps); empty when `0`. |
| `<media-samplerate>` | Sample rate (Hz); empty when `0`. |
| `<media-bits-per-sample>` | Bits per sample; empty when `0`. |
| `<media-channels>` | Channel count; empty when `0`. |
| `<media-video-width>` | Video width (px); empty when `0`. |
| `<media-video-height>` | Video height (px); empty when `0`. |
| `<media-photo-width>` | Photo width (px); empty when `0`. |
| `<media-photo-height>` | Photo height (px); empty when `0`. |
| `<media-photo-quality>` | Photo quality; empty when `0`. |

**Arguments:** No argument (`<media-mime>` only). A stray **`<media-mime:…>`** fails at compile.

#### MPEG audio properties

Reads from **`Original.Media.Mpeg`** (nested read-only TagLib `Mpeg.AudioHeader` on the media cache). Loaded by the same **`MediaPropertiesReader.Read`** path as media properties. Files without an MPEG audio header (e.g. WAV/FLAC/AAC) leave **`Media.Mpeg`** null and expand tokens to **empty** (not PreviewError). Replaces MFR7’s legacy **`mp3-*`** names.

| Token | Output |
|--------|--------|
| `<mpeg-bitrate>` | Bitrate (kbps); prefixed `VBR` when Xing/VBRI present (e.g. `VBR128`); empty when `0`. |
| `<mpeg-copyright>` | `Yes` or `No`. |
| `<mpeg-duration>` | Header duration as `h:mm:ss`; empty when zero. |
| `<mpeg-duration-sec>` | Whole seconds (floor); empty when zero. |
| `<mpeg-encoding>` | `CBR` or `VBR`; empty when no MPEG header. |
| `<mpeg-frequency>` | Sample rate (Hz); empty when `0`. |
| `<mpeg-layer>` | `I`, `II`, or `III`; empty when unset. |
| `<mpeg-ver>` | MPEG version (`1`, `2`, or `2.5`); empty when unknown. |
| `<mpeg-mode>` | Channel mode (`Stereo`, `JointStereo`, `DualChannel`, `SingleChannel`). |
| `<mpeg-original>` | `Yes` or `No`. |
| `<mpeg-protection>` | `Yes` or `No` (CRC protection bit). |

**Arguments:** No argument (`<mpeg-bitrate>` only). A stray **`<mpeg-bitrate:…>`** fails at compile.

#### ID3v2 Custom Field (MFR7 `<id3v2:…>`)

Reads one **modeled ID3v2 frame** from **`Preview.AudioTagOverlay.Id3v2`** (same lazy load rules as `<audio-*>`). Unlike semantic `<audio-*>` tokens, this is **ID3v2-only** (other containers yield empty when no Id3v2 block is present).

| Token | Output |
|--------|--------|
| `<id3v2:TALB>` | Album frame text; empty when unset. |
| `<id3v2:TIT2>` | Title frame text; empty when unset. |
| `<id3v2:TXXX>` | First `TXXX` frame in overlay order (MFR7-compatible; no content-descriptor picker). |
| `<id3v2:TXXX:catalog>` | `TXXX` whose content descriptor is `catalog`; empty when no match. |
| `<id3v2:COMM>` | Primary comment (empty content descriptor); empty when unset. |
| `<id3v2:COMM:other>` | Comment whose content descriptor is `other`. |
| `<id3v2:USLT>` | Primary unsynced lyrics; optional content-descriptor suffix like `COMM`. |

**Arguments:** `field-code` is **required** (the four-character frame id, case-insensitive). Multi-instance frames (`TXXX` / `COMM` / `USLT`) may append a content-descriptor suffix after the id. Singleton frames reject that suffix at compile time. Multi-value text on one frame is joined with `; `. `TRCK` / `TPOS` return the full frame text (e.g. `3/12`); use `<audio-track>` / `<audio-track-count>` when a split is needed.

#### File properties

| Token | Output |
|--------|--------|
| `<file-date:format,date-kind>` | Arguments are **required**. **Both** parts: a .NET date format string, then a comma, then `date-kind`. Split uses the **last** comma so `format` may contain commas. `date-kind` (case-insensitive): `creation`, `lastWrite`, `lastAccess`. Example: `<file-date:dd-MM-yyyy,creation>`. |
| `<drive-letter>` | Drive letter of the file's location (e.g. `C:`). Returns `$` for network (UNC) paths. |
| `<label>` | Volume label of the drive that holds the file. |
| `<file-count>` | Number of files and folders in the same directory (non-recursive). Empty when directory does not exist. |
| `<file-size>` | File size, auto-selecting the largest unit (e.g. `1 KB`, `2 MB`). |
| `<file-size:unit>` | File size in a specific unit. `unit`: omit or `auto` (auto-scale), `b`/`bytes`, `kb`, `mb`, `gb` (case-insensitive). |
| `<file-size:unit,decimals>` | File size with the specified number of decimal places (default `0`). |

#### Counter and time

| Token | Output |
|--------|--------|
| `<now>` | Current UTC time, ISO-8601 style. |
| `<now:format>` | Current UTC time formatted with a .NET format string. |
| `<item-count>` | Total items in the current rename list (no arguments). Populated during preview/commit. |
| `<name-list-entry:name-list-file-path>` | Uses Name List parsing rules (comment lines are skipped; blank lines are preserved; at least one entry required), then returns the entry at the item's rename-list position. Throws a user-facing error when item index exceeds the parsed entry count. |
| `<random-char:low,high>` | One random character, uniformly chosen between inclusive endpoints (first character of each side is used; order may be reversed). Examples: `<random-char:A,Z>`, `<random-char:0,9>`. |
| `<counter>` | Rename-list index as `initial` + `step`×index using defaults `initial=1`, `step=1`, `padding=none`, `length=2`, `resetScope=global`. With `padding=none`, output has no leading zeros. |
| `<counter:initial=…,step=…,padding=…,length=…,resetScope=…>` | Named options, **order-independent** (spaces around `,` and `=` optional). Omitted options use `initial=1`, `step=1`, `padding=none`, `length=2`, `resetScope=global`. Value: `initial` + `step`×index. `padding`: `none`, `auto` (width from list scope), or `fixed` (pad to `length`, minimum digit width `1`). `resetScope`: `global` vs `perFolder` (restart per folder). |

#### Token extraction

| Token | Output |
|-------|--------|
| `<token:tokenNumber=…,separator=…,includeNext=…,includePrev=…,source=…>` | **Named** options, **order-independent** (spaces optional). Resolves `source`, splits by `separator`, then returns the 1-based `tokenNumber` part. With `includeNext=true`, returns that part through the end (rejoined with `separator`); with `includePrev=true`, returns from the start through that part (rejoined); both `true` returns the full resolved string. `source` may contain nested `<…>` tokens; commas inside balanced angle brackets are not option separators. `includeNext` / `includePrev`: `true` / `false` (case-insensitive). |
| `<substr:start=…,end=…,source=…>` | Named options, **order-independent** (spaces optional). Extracts characters from `source` between two positions (inclusive). Positions are 1-based; negative positions count from the right (`-1` = last character). Out-of-range positions are clamped to the nearest boundary. When the resolved start exceeds the resolved end, the range `(end, start]` is returned. `source` may contain nested `<…>` tokens; commas inside balanced angle brackets are not option separators. |

Unknown token names cause an error at runtime.

## Examples

Assume directory `Music\My Album\` when using `<parent-folder>`. Counter rows use the **global** index passed to the filter.

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `template`: `"<file-name>"` | `song` | `song` | |
| `template`: `"<parent-folder>"`<br>file under `Music\My Album\` | `ignored` | `My Album` | |
| `template`: `"<parent-folder:2>"`<br>file under `Music\My Album\` | `ignored` | `Music` | Level 2 = grandparent. |
| `template`: `"<file-date:dd-MM-yyyy,creation>"` | `ignored` | `07-04-2023` | Creation time, common date layout. |
| `template`: `"<file-date:yyyy,creation>"` | `ignored` | `2024` | Creation year. |
| `template`: `"<file-date:yyyy,lastWrite>"` | `ignored` | `2021` | Last-write year. |
| `template`: `"<file-size>"` | `ignored` | `1 KB` | Auto unit, 0 decimals. |
| `template`: `"<file-size:mb,2>"` | `ignored` | `1.50 MB` | MB, 2 decimal places. |
| `template`: `"<drive-letter>"` | `ignored` | `C:` | Drive letter of the file. |
| `template`: `"<counter:initial=10,step=2,padding=fixed,length=4,resetScope=global>"`<br>global index: `3` | `ignored` | `0016` | `10 + 2×3`, fixed width `4`. |
| `template`: `"<token:tokenNumber=1,separator=-,includeNext=false,includePrev=false,source=<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `13_` | Track number prefix, split by `-`. |
| `template`: `"<token:tokenNumber=2,separator=_-_,includeNext=false,includePrev=false,source=<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `Smog` | Artist name, split by `_-_`. |
| `template`: `"<token:tokenNumber=2,separator=_-_,includeNext=true,includePrev=false,source=<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `Smog_-_Cold_Blooded_Old_Times.mp3` | Artist and title, include-next. |
| `template`: `"<substr:start=1,end=5,source=<file-name>>"` | `MyTestFileName.123` | `MyTes` | First 5 chars of prefix. |
| `template`: `"<substr:start=5,end=-6,source=<full-name>>"` | `MyTestFileName.123` | `stFileNam` | Positive start, negative end. |
| `template`: `"<substr:start=-1,end=2,source=<file-extension>45>"` | `MyTestFileName.123` | `2345` | Crossed positions: extension `.123` + literal `45` → `.12345`; range `(2,6]`. |

For sequential numbering without a full template, see [Counter](Counter.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). When targeting **Prefix**, `<file-name>` is the original file prefix.

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
