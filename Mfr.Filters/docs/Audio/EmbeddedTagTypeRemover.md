# EmbeddedTagTypeRemover

Removes **selected** embedded tag types and leaves the file's other tag blocks alone. Use it to drop the legacy ID3v1 trailer from an MP3 that also has ID3v2, or to clear one competing block before a later filter writes new values. To remove *everything* TagLibSharp controls, use [EmbeddedTagRemover](EmbeddedTagRemover.md) instead.

Has **no** `target`.

On **preview**, the chosen blocks are nulled on the row's overlay after tags load from disk. Filters later in the chain that write generic fields (title, album, …) only reach the blocks that are still present.

On **commit**, each dropped block is deleted with `RemoveTags` for that tag type before the surviving blocks are written. Removing a tag type also deletes content the overlay does not model, such as **embedded art stored on that block**. **You cannot undo this with the app's Undo control**—only filesystem or backup restore.

## Options

| Option | Type | Description |
|--------|------|-------------|
| `blocks` | array of strings | Tag block types to remove. At least one entry is required. |

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

**Directory rows** are not supported (preview error, same as other embedded-tag filters). **Invalid or non-TagLib files** surface preview errors when tags cannot be read.

## Preset shape

```json
{
  "type": "EmbeddedTagTypeRemover",
  "enabled": true,
  "options": {
    "blocks": ["id3v1"]
  }
}
```

## Examples

| Options | Before | After | Comment |
|--------|--------|-------|---------|
| `blocks: ["id3v1"]` | `.mp3` with ID3v1 + ID3v2 | ID3v2 only | ID3v2 frames, including art, are untouched |
| `blocks: ["id3v1"]` | `.mp3` with ID3v2 only | Unchanged | Supported block that is absent is a no-op |
| `blocks: ["id3v1", "id3v2"]` | `.mp3` with ID3v1 + ID3v2 | No embedded tags | Same end state as EmbeddedTagRemover for MP3, without the global strip flag |
| `blocks: ["id3v2"]` | `.flac` | Preview error | FLAC cannot hold ID3v2; supported blocks are listed in the message |
| `blocks: ["id3v1"]` → Formatter on `audio-title` | `.mp3` with conflicting ID3v1/ID3v2 titles | Title written to ID3v2 only | Removing the block first keeps the generic write off the trailer |
