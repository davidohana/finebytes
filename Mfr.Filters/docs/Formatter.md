# Formatter

Replaces the target segment with text from a **template**. Templates can include tokens in angle brackets.

**Supported tokens (names before `:`)**

| Token | Role |
|--------|------|
| `<file-name>` | Original file name without extension |
| `<ext>` / `<file-ext>` | Original extension (with dot) |
| `<full-name>` | Prefix + extension |
| `<parent-folder>` | Name of the parent folder |
| `<full-path>` | Full path string |
| `<now>` or `<now:format>` | Current UTC time (optional .NET format string) |
| `<counter:…>` | Five comma-separated numbers (start, step, reset flag, width, pad mode)—same idea as the [Counter](Counter.md) filter token in replace lists |

**Example**

- Template: `<file-name> (remaster)<ext>`  
- If the file was `song.flac`, the result is `song (remaster).flac` (depending on which part of the name is targeted).

Use **Formatter** when the result should be a **pattern** mixing fixed text and metadata. For mostly sequential numbers with padding, see [Counter](Counter.md).
