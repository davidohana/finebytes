# AudioTagSetter

Sets common embedded audio-tag fields on each **file** row in one step (legacy **Audio / ID3 Tag Setter** style). Values update the preview `AudioTagOverlay`; commit writes through the same path as other audio-tag changes.

Generic writes **broadcast** each set field onto every present tag block (for example Title updates both ID3v1 and ID3v2 when both exist). When the overlay carries **no** blocks (untagged file, or after **TagRemover** with `all: true`), the filter creates the container’s **recommended** empty block first (ID3v2 on MP3, Xiph on FLAC, Apple on M4A, ASF on WMA, RIFF INFO on WAV, …) then writes into that block only — sibling types are never invented.

**Directory rows** cannot load embedded tags; applying this filter to a folder row fails preview with an error (same as other audio-overlay operations).

Omit a property under **`options`** (or set it to JSON **`null`**) to leave that tag field unchanged.

Each included field object uses:

- **`text`** — plain text, **or** a formatter template when it contains at least one balanced `<...>` span that looks like a formatter token (ASCII letter, then letters/digits/`-`/`_`, at least two characters before an optional `:`), same token language as [Formatter](../Formatting/Formatter.md). Text without such a span is left **literal**.
- **`onlyIfEmpty`** (optional, default **`false`**) — when **`true`**, set the value only when the field is empty in the preview overlay after tags are loaded (strings: null/whitespace; year/beatsPerMinute/track/trackCount/disc/discCount: unset). When **`false`** or omitted, always set (overwrite).

**String fields** (`performers`, `albumArtists`, `title`, `album`, `genre`, `comment`, `composers`, `lyrics`, `grouping`, `copyright`, `conductor`, **`year`**, **`beatsPerMinute`**, **`track`**, **`trackCount`**, **`disc`**, **`discCount`**):

- **Year-specific:** after formatting (or as literal `text`), the result is trimmed. **Empty** clears the year. **`0`** clears. Otherwise the value must parse as an integer **1–9999**; anything else (non-numeric, out of range) causes a **preview error** for that row (`FormatException`, surfaced like other filter failures).
- **`beatsPerMinute`:** same shape as year — **empty** or **`0`** clears; **1–65535** sets; non-numeric or **&gt; 65535** → preview error.
- **Track-specific:** after formatting, **empty** clears the track. Otherwise the value must parse as an integer **0–255** (base value before increment). **`0`** with **`trackAutoIncrement`** `false` clears; with **`trackAutoIncrement`** `true`, **0 + RenameListIndex** is applied (then clamped to **255**). Base **&gt; 255** or non-numeric → preview error.
- **`trackCount` / `disc` / `discCount`:** same parse rules as **track** without auto-increment — **empty** or **`0`** clears; **1–255** sets; non-numeric or **&gt; 255** → preview error.
- **`trackAutoIncrement`** (on `options`, sibling of `track`) — add **`RenameListIndex`** to the parsed base track before clamping to **255**.
- Multiple performers/composers/genres in one string: use **`;`** as separator in `text` (normalized with `; ` when persisting).

## Examples

| Options                                             | Before (overlay)   | After                    | Comment               |
|-----------------------------------------------------|--------------------|--------------------------|-----------------------|
| `text`: `Fixed`                                     | title empty or any | title `Fixed`            | Overwrites.           |
| `onlyIfEmpty`, `text`: `Fill`                       | title `Already`    | unchanged                |                       |
| `onlyIfEmpty`, `text`: `Fill`                       | title empty        | title `Fill`             |                       |
| `track`: `text`: `1`; `trackAutoIncrement`: `true`  | (list index `0`)   | track `1`                |                       |
| `track`: `text`: `1`; `trackAutoIncrement`: `true`  | (list index `4`)   | track `5`                |                       |
| `track`: `text`: `0`; `trackAutoIncrement`: `false` | track `3`          | cleared                  |                       |
| `trackCount`: `text`: `12`                          | any                | track count `12`         |                       |
| `disc`: `text`: `2`; `discCount`: `text`: `3`       | any                | disc `2` of `3`          |                       |
| `composers`: `text`: `Bach ; Handel`                | any                | composers `Bach; Handel` |                       |
| `lyrics`: `text`: `La la`                           | any                | lyrics `La la`           |                       |
| `grouping`: `text`: `Suite`                         | any                | grouping `Suite`         |                       |
| `copyright`: `text`: `© 2004`                       | any                | copyright set            |                       |
| `conductor`: `text`: `Karajan`                      | any                | conductor set            |                       |
| `beatsPerMinute`: `text`: `120`                     | any                | BPM `120`                |                       |
| `year`: `text`: `0`                                 | year `1999`        | cleared                  |                       |
| `year`: `text`: `12000`                             | any                | preview error            | Not a valid tag year. |

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). No `target` property.

```json
{
  "type": "AudioTagSetter",
  "options": {
    "albumArtists": {
      "text": "<parent-folder:1>"
    },
    "title": {
      "text": "<file-name>"
    },
    "composers": {
      "text": "J. S. Bach"
    },
    "lyrics": {
      "text": "Verse one"
    },
    "grouping": {
      "text": "Part I"
    },
    "copyright": {
      "text": "© 2004 Label"
    },
    "conductor": {
      "text": "Karajan"
    },
    "beatsPerMinute": {
      "text": "120"
    },
    "track": {
      "text": "1"
    },
    "trackCount": {
      "text": "12"
    },
    "disc": {
      "text": "1"
    },
    "discCount": {
      "text": "2"
    },
    "trackAutoIncrement": true,
    "year": {
      "text": "2004",
      "onlyIfEmpty": true
    },
    "comment": {
      "text": "Tagged via preset",
      "onlyIfEmpty": true
    }
  }
}
```

Property names are case-insensitive with the default preset JSON options.
