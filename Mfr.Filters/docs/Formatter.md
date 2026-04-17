# Formatter

Replaces the **entire target segment** with the result of expanding a **template string**. Placeholders use angle brackets: `<token>` or `<name:arguments>`.

## Preset fields

| Field | Type | Description |
|--------|------|-------------|
| `type` | string | Must be `Formatter`. |
| `enabled` | bool | When `false`, the filter does nothing. |
| `target` | object | See [LettersCase](LettersCase.md). |
| `options` | object | See below. |

### Options (`options`)

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

**Rename with suffix in name**

- `template`: `"<file-name> (remaster)<ext>"`  
- If original base is `song` and extension `.flac`, result is `song (remaster).flac` (for a prefix target, the segment is replaced by that string).

**Counter token**

- `<counter:10,1,0,2,0>` — start 10, step 1, global index, width 2, pad with `0` (matches patterns used in [ReplaceList](ReplaceList.md) replacements).

**Example preset fragment**

```json
{
  "type": "Formatter",
  "enabled": true,
  "target": { "family": "FileName", "fileNamePart": "Prefix" },
  "options": {
    "template": "<parent-folder> - <file-name>"
  }
}
```

For sequential numbering without a full template, see [Counter](Counter.md).
