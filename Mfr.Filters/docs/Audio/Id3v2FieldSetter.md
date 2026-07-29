# Id3v2FieldSetter

Sets **one** modeled ID3v2 text frame on each **MPEG/MP3** file row (legacy **ID3v2 Field Setter**). Values update the preview `AudioTagOverlay`; commit writes through the same path as other audio-tag changes.

Creates an **ID3v2.3** block when the file has no ID3v2 tag. If a tag already exists, its version is **preserved** (no silent upgrade/downgrade). Writing a **v2.4-only** frame (for example `TDRC`) into a v2.3 tag is a **preview error**.

**Non-MPEG containers** (FLAC, M4A, …) and **directory rows** fail preview.

Has **no** `target`. Put `frameId` (and optional multi-instance identity) under **`options`**. Add multiple instances of this filter to set several frames in one chain.

String filters such as [Formatter](../Formatting/Formatter.md) with an `Id3v2Frame` target can also set individual frames; this filter adds **`onlyIfEmpty`** and a dedicated preset shape. For common cross-format fields, prefer [AudioTagSetter](AudioTagSetter.md).

## Options

| Option | Type | Description |
|--------|------|-------------|
| `frameId` | string | Four-character frame id (case-insensitive; stored uppercase). **Required.** |
| `text` | string | Plain text, **or** a formatter template when it contains at least one balanced `<...>` span that looks like a formatter token (same rules as [AudioTagSetter](AudioTagSetter.md) / [Formatter](../Formatting/Formatter.md)). Default empty (clears the frame instance). |
| `onlyIfEmpty` | boolean | When `true`, set only if the current frame text is empty/whitespace. Default `false` (overwrite). |
| `language` | string or null | ISO-639-2 language for `COMM` / `USLT`. Omit for primary create (`eng`). |
| `description` | string or null | Content descriptor for `COMM` / `USLT` / `TXXX`. Omit for the primary instance. |

`TRCK` is written as a single text frame (e.g. `3` or `3/12`); there is no separate track-number / track-count option. Field format constraints (e.g. year as digits in `TYER`) are the caller’s responsibility.

## Examples

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `frameId`: `TIT2`<br>`text`: `Fixed` | TIT2 empty or any | TIT2 `Fixed` | Overwrites. |
| `frameId`: `TIT2`<br>`text`: `Fill`<br>`onlyIfEmpty`: `true` | TIT2 `Already` | unchanged | |
| `frameId`: `TIT2`<br>`text`: `Fill`<br>`onlyIfEmpty`: `true` | TIT2 empty | TIT2 `Fill` | |
| `frameId`: `TIT2`<br>`text`: `<file-name>` | prefix `Song` | TIT2 `Song` | Formatter token. |
| `frameId`: `COMM`<br>`text`: `Hi` | no COMM | COMM text `Hi`, language `eng` | Primary comment. |
| `frameId`: `TIT2`<br>`text`: `X` | `.flac` row | preview error | ID3v2 not supported. |

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). No `target` property.

```json
{
  "type": "Id3v2FieldSetter",
  "options": {
    "frameId": "TIT2",
    "text": "<file-name>",
    "onlyIfEmpty": false
  }
}
```

```json
{
  "type": "Id3v2FieldSetter",
  "options": {
    "frameId": "TXXX",
    "text": "my custom value",
    "description": "custom-key"
  }
}
```

Property names are case-insensitive with the default preset JSON options.
