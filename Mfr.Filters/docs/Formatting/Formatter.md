# Formatter

Replaces the **entire target segment** with the result of expanding a **template string**. Placeholders use angle brackets: `<token>` or `<name:arguments>`.

## Options

| Property | Type | Description |
|----------|------|-------------|
| `template` | string | Output text; see **Tokens** below. |

### Tokens

#### File name

| Token | Output |
|--------|--------|
| `<file-name>` | Original prefix (no extension). |
| `<file-extension>` or `<ext>` | Original extension (with dot). |
| `<full-name>` | Prefix + extension. |
| `<parent-folder>` | Name of the immediate parent folder (level 1). |
| `<parent-folder:level>` | Ancestor folder name at the given level (1 = immediate parent, 2 = grandparent, …). Returns empty when level exceeds path depth. |
| `<full-path>` | Full path of the file. |

#### File properties

| Token | Output |
|--------|--------|
| `<file-date>` | Creation date formatted as `dd-MM-yyyy` (default). |
| `<file-date:format>` | Creation date using a .NET date format string. |
| `<file-date:format,date-type>` | Date/time stamp. `date-type`: `0` = creation (default), `1` = last write, `2` = last access. |
| `<drive-letter>` | Drive letter of the file's location (e.g. `C:`). Returns `$` for network (UNC) paths. |
| `<label>` | Volume label of the drive that holds the file. |
| `<file-count>` | Number of files and folders in the same directory (non-recursive). Empty when directory does not exist. |
| `<file-size>` | File size, auto-selecting the largest unit (e.g. `1 KB`, `2 MB`). |
| `<file-size:unit>` | File size in a specific unit. `unit`: `0`/`auto`, `1`/`b`/`bytes`, `2`/`kb`, `3`/`mb`, `4`/`gb`. |
| `<file-size:unit,decimals>` | File size with the specified number of decimal places (default `0`). |

#### Counter and time

| Token | Output |
|--------|--------|
| `<now>` | Current UTC time, ISO-8601 style. |
| `<now:format>` | Current UTC time formatted with a .NET format string. |
| `<counter>` | Same as `<counter:1,1,0,2,0>` (no leading zeros). |
| `<counter:initial,step,leading-zeroes-mode,length,reset-folder>` | Position in rename list: `initial` + `step`×index. `leading-zeroes-mode`: `0` = none, `1` = automatic width from list size, `2` = pad to `length` (digits; minimum `1`). `reset-folder`: `0` = global index, `1` = index restarts per folder. |

Unknown token names cause an error at runtime.

## Examples

Assume directory `Music\My Album\` when using `<parent-folder>`. Counter rows use the **global** index passed to the filter.

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `template`: `"<file-name>"` | `song` | `song` | |
| `template`: `"<parent-folder>"`<br>file under `Music\My Album\` | `ignored` | `My Album` | |
| `template`: `"<parent-folder:2>"`<br>file under `Music\My Album\` | `ignored` | `Music` | Level 2 = grandparent. |
| `template`: `"<file-date>"` | `ignored` | `07-04-2023` | Creation date, default format. |
| `template`: `"<file-date:yyyy,1>"` | `ignored` | `2021` | Last-write year. |
| `template`: `"<file-size>"` | `ignored` | `1 KB` | Auto unit, 0 decimals. |
| `template`: `"<file-size:mb,2>"` | `ignored` | `1.50 MB` | MB, 2 decimal places. |
| `template`: `"<drive-letter>"` | `ignored` | `C:` | Drive letter of the file. |
| `template`: `"<counter:10,2,2,4,0>"`<br>global index: `3` | `ignored` | `0016` | `10 + 2×3`, custom width `4`. |

For sequential numbering without a full template, see [Counter](Counter.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). When targeting **Prefix**, `<file-name>` is still the original prefix.

```json
{
  "type": "Formatter",
  "target": {
    "targetType": "FilePrefix"
  },
  "options": {
    "template": "<parent-folder> - <file-name>"
  }
}
```
