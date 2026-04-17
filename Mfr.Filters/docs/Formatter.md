# Formatter

Replaces the **entire target segment** with the result of expanding a **template string**. Placeholders use angle brackets: `<token>` or `<name:arguments>`.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `template` | string | Output text; see **Tokens** below. |

### Tokens

| Token | Output |
|--------|--------|
| `<file-name>` | Original prefix (no extension). |
| `<file-ext>` or `<ext>` | Original extension (with dot). |
| `<full-name>` | Prefix + extension. |
| `<parent-folder>` | Name of the parent folder (last segment of directory path). |
| `<full-path>` | Full path of the file. |
| `<now>` | Current UTC time, ISO-8601 style. |
| `<now:format>` | Current UTC time formatted with a .NET format string (after `:`). |
| `<counter:start,step,reset,width,pad>` | Five comma-separated integers: start, step, reset flag (1 = per folder index, other = global index), minimum width, pad mode (0 = `0`, 1 = space). |

Unknown token names cause an error at runtime.

## Examples

- `template`: `"<file-name> (remaster)<ext>"` — if base is `song` and extension `.flac`, result is `song (remaster).flac` (when targeting the prefix segment).
- `<counter:10,1,0,2,0>` — same counter pattern as in [ReplaceList](ReplaceList.md) replacements.

For sequential numbering without a full template, see [Counter](Counter.md).

```json
{ "template": "<parent-folder> - <file-name>" }
```
