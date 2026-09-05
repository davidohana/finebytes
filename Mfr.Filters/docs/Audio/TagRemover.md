# TagRemover

Removes embedded tag blocks — either **selected** types, or a **nuclear** wipe of everything TagLibSharp controls when `all` is true. Has **no** `target`.

## Modes

- **`"all": true`**
  - Preview: Clears all modeled overlay blocks (`ClearAllBlocks`)
  - Commit: Nuclear: `RemoveTags(AllTags)` (see below)
- **`"blocks": [...]`**
  - Preview: Nulls each listed block
  - Commit: `RemoveTags` for those types only; surviving blocks stay writable by later filters

When `all` is true, `blocks` is ignored. When `all` is false (or omitted) and `blocks` is missing or empty, the filter is a **no-op** (nothing removed).

Removing a tag type also deletes content the overlay does not model on that block, such as **embedded art**. **You cannot undo this with the app's Undo control**—only filesystem or backup restore.

**Directory rows** are not supported (preview error, same as other embedded-tag filters). **Invalid or non-TagLib files** surface preview errors when tags cannot be read.

## Nuclear option (`all: true`) — what it does extra

On **preview**, nuclear looks the same as clearing every modeled block: the overlay’s seven block properties go null; `ContainerFormat` is kept so a later filter can recreate the recommended empty block.

On **commit**, nuclear does **more** than listing all seven `blocks` values. Selective Apply only deletes tag types that appear in the Original→Preview diff for modeled kinds. Nuclear sets `StripAllEmbeddedTagsOnCommit` and the engine calls TagLib `RemoveTags(AllTags)`, which also strips types **outside** `AudioTagBlockKind`:

| TagLib `TagTypes`                    | Typical meaning                                    |
| ------------------------------------ | -------------------------------------------------- |
| `MovieId`                            | RIFF / MOVIEID                                     |
| `DivX`                               | DivX tags                                          |
| `FlacMetadata`                       | FLAC native metadata (separate from Xiph comments) |
| `TiffIFD`                            | TIFF IFD                                           |
| `XMP`                                | XMP packets                                        |
| `JpegComment` / `GifComment` / `Png` | image comment / PNG chunks                         |
| `IPTCIIM`                            | IPTC-IIM                                           |
| `AudibleMetadata`                    | Audible                                            |
| `Matroska`                           | Matroska / WebM tags                               |

So for a normal MP3/FLAC/M4A that only carries modeled blocks, `all: true` and `blocks: [all seven]` end the same way for those blocks. Prefer `all: true` when you want a hard wipe of whatever TagLib can see (including leftovers the overlay never parsed).

## Options

- **`all`** (boolean) — When `true`, nuclear strip (see above). Default `false`.
- **`blocks`** (array of strings)
  - Tag block types to remove when `all` is false. Empty or omitted → no-op.

Valid `blocks` values and the containers that can hold them:

| Value      | Tag block           | Containers           |
| ---------- | ------------------- | -------------------- |
| `id3v1`    | ID3v1 trailer       | MP3                  |
| `id3v2`    | ID3v2 frames        | MP3                  |
| `xiph`     | Xiph/Vorbis comment | FLAC, Ogg, Opus      |
| `ape`      | APEv2               | FLAC, Monkey's Audio |
| `apple`    | Apple/iTunes `ilst` | MP4, M4A             |
| `asf`      | ASF descriptors     | WMA                  |
| `riffInfo` | RIFF `LIST/INFO`    | WAV                  |

Naming a block the row's container cannot hold is a **preview error** (for example `id3v2` on a FLAC), not a silent skip. Naming a supported block the file does not carry is a no-op.

## Preset shape

```json
{
  "type": "TagRemover",
  "enabled": true,
  "options": {
    "all": true
  }
}
```

```json
{
  "type": "TagRemover",
  "enabled": true,
  "options": {
    "blocks": ["id3v1"]
  }
}
```

## Examples

- `all: true`
  - Before: Tagged `.wav` / `.mp3` / image with XMP
  - After: `No embedded tags left`
  - Comment: XMP and other unmodeled TagLib types go too
- `all: true`
  - Before: Chain: TagRemover → Formatter on `audio-title`
  - After: `New title written after strip`
  - Comment: Commit runs strip **then** overlay merge
- `blocks: ["id3v1"]`: `.mp3` with ID3v1 + ID3v2 → `ID3v2 only` — ID3v2 frames, including art, are untouched
- `blocks: ["id3v1"]`: `.mp3` with ID3v2 only → `Unchanged` — Supported block that is absent is a no-op
- `blocks: ["id3v1", "id3v2"]`
  - Before: `.mp3` with ID3v1 + ID3v2
  - After: `No modeled tags`
  - Comment: Unlike `all: true`, unmodeled TagLib types (if any) stay
- `blocks: ["id3v2"]` — `.flac` → `Preview error` — FLAC cannot hold ID3v2; supported blocks are listed in the message
- `blocks: ["id3v1"]` → Formatter on `audio-title`
  - Before: `.mp3` with conflicting ID3v1/ID3v2 titles
  - After: `Title written to ID3v2 only`
  - Comment: Removing the block first keeps the generic write off the trailer
