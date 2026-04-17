# Formatting filters

Filters that build text from templates or inject numbers.

---

## Formatter

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
| `<counter:…>` | Five comma-separated numbers (start, step, reset flag, width, pad mode)—same idea as the Counter filter token in replace lists |

**Example**

- Template: `<file-name> (remaster)<ext>`  
- If the file was `song.flac`, the result is `song (remaster).flac` (depending on which part of the name is targeted).

---

## Counter

Inserts a numeric counter based on each file’s index (global or per-folder). You can **prepend**, **append**, or **replace** the whole segment with the formatted number.

**Typical options**

- Start value, step, width, pad character  
- Whether the counter resets per folder  
- Separator when prepending or appending  

**Example**

- Replace mode, width 3, pad `0`, start 1: first file → `001`, second → `002`, …

---

### Tips

- Use **Formatter** when the result should be a **pattern** mixing fixed text and metadata.
- Use **Counter** when you mainly need **sequential numbers** with padding and placement control.
