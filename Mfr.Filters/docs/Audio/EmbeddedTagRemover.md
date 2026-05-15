# EmbeddedTagRemover

Removes **all** embedded metadata that TagLibSharp controls for the file (ID3, Vorbis comments, MP4 ilst, RIFF LIST chunks, image keywords/XMP, etc.). Has **no** `target` and **no** `options`.

On **preview**, modeled `AudioTagOverlay` columns are cleared after tags are loaded from disk. The rename list can still show other changes (paths, other filters).

On **commit**, the app calls `RemoveTags(TagTypes.AllTags)` and saves the destination file. **You cannot undo this with the app’s Undo control**—only filesystem or backup restore.

**Directory rows** are not supported (preview error, same as other embedded-tag filters). **Invalid or non-TagLib files** surface preview errors when tags cannot be read.

## Preset shape

```json
{
  "type": "EmbeddedTagRemover",
  "enabled": true
}
```

## Examples

| Options | Before | After | Comment |
|--------|--------|-------|---------|
| *(none)* | Tagged `.wav` / `.mp3` / image with XMP | No embedded tags left | After **Apply**; preview columns show cleared overlay |
| *(none)* | Chain: EmbeddedTagRemover → Formatter on `audio-title` | New title written after strip | Commit runs strip **then** overlay merge |
