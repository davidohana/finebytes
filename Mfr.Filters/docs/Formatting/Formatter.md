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

Assume directory `Music\My Album\` when using `<parent-folder>`. Counter rows use the **global** index passed to the filter.

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `template`: `"<file-name>"` | `song` | `song` | |
| `template`: `"<parent-folder>"`<br>file under `Music\My Album\` | `ignored` | `My Album` | |
| `template`: `"<counter:10,2,0,4,0>"`<br>global index: `3` | `ignored` | `0016` | `10 + 2×3`, width `4`, pad `0`. |

For sequential numbering without a full template, see [Counter](Counter.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). When targeting **Prefix**, `<file-name>` is still the original prefix.

```json
{
  "type": "Formatter",
  "target": {
    "family": "FileName",
    "fileNamePart": "Prefix"
  },
  "options": {
    "template": "<parent-folder> - <file-name>"
  }
}
```
