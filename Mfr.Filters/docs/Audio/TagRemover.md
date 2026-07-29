# TagRemover

Removes embedded tag blocks — either **selected** types, or **everything** TagLibSharp controls when `all` is true. Has **no** `target`.

## Modes

| `options` | Preview | Commit |
|-----------|---------|--------|
| `"all": true` | Clears all modeled overlay blocks (`ClearAllBlocks`) | `RemoveTags(AllTags)` — every TagLib type, including unmodeled ones (XMP, DivX, Matroska, …) |
| `"blocks": [...]` | Nulls each listed block | `RemoveTags` for those types only; surviving blocks stay writable by later filters |

When `all` is true, `blocks` is ignored. When `all` is false (or omitted), `blocks` must list at least one type.

Removing a tag type also deletes content the overlay does not model, such as **embedded art stored on that block**. **You cannot undo this with the app's Undo control**—only filesystem or backup restore.

**Directory rows** are not supported (preview error, same as other embedded-tag filters). **Invalid or non-TagLib files** surface preview errors when tags cannot be read.

## Options

| Option | Type | Description |
|--------|------|-------------|
| `all` | boolean | When `true`, nuclear strip of all TagLib tags. Default `false`. |
| `blocks` | array of strings | Tag block types to remove when `all` is false. At least one entry required unless `all` is true. |

Valid `blocks` values and the containers that can hold them:

| Value | Tag block | Containers |
|-------|-----------|------------|
| `id3v1` | ID3v1 trailer | MP3 |
| `id3v2` | ID3v2 frames | MP3 |
| `xiph` | Xiph/Vorbis comment | FLAC, Ogg, Opus |
| `ape` | APEv2 | FLAC, Monkey's Audio |
| `apple` | Apple/iTunes `ilst` | MP4, M4A |
| `asf` | ASF descriptors | WMA |
| `riffInfo` | RIFF `LIST/INFO` | WAV |

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

| Options | Before | After | Comment |
|--------|--------|-------|---------|
| `all: true` | Tagged `.wav` / `.mp3` / image with XMP | No embedded tags left | After **Apply**; preview columns show cleared overlay |
| `all: true` | Chain: TagRemover → Formatter on `audio-title` | New title written after strip | Commit runs strip **then** overlay merge |
| `blocks: ["id3v1"]` | `.mp3` with ID3v1 + ID3v2 | ID3v2 only | ID3v2 frames, including art, are untouched |
| `blocks: ["id3v1"]` | `.mp3` with ID3v2 only | Unchanged | Supported block that is absent is a no-op |
| `blocks: ["id3v1", "id3v2"]` | `.mp3` with ID3v1 + ID3v2 | No modeled tags | Unlike `all: true`, unmodeled TagLib types (if any) are left alone |
| `blocks: ["id3v2"]` | `.flac` | Preview error | FLAC cannot hold ID3v2; supported blocks are listed in the message |
| `blocks: ["id3v1"]` → Formatter on `audio-title` | `.mp3` with conflicting ID3v1/ID3v2 titles | Title written to ID3v2 only | Removing the block first keeps the generic write off the trailer |
