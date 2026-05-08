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
| `<item-count>` | Total items in the current rename list (no arguments). Populated during preview/commit. |
| `<random-char:low,high>` | One random character, uniformly chosen between inclusive endpoints (first character of each side is used; order may be reversed). Examples: `<random-char:A,Z>`, `<random-char:0,9>`. |
| `<counter>` | Same as `<counter:1,1,0,2,0>` (no leading zeros). |
| `<counter:initial,step,leading-zeroes-mode,length,reset-folder>` | Position in rename list: `initial` + `step`×index. `leading-zeroes-mode`: `0` = none, `1` = automatic width from list size, `2` = pad to `length` (digits; minimum `1`). `reset-folder`: `0` = global index, `1` = index restarts per folder. |

#### Token extraction

| Token | Output |
|-------|--------|
| `<token:token-number,separator,include-next,include-prev,source-format-string>` | Splits `source-format-string` by `separator` and returns the 1-based `token-number`-th part. `include-next`: `1` = return from `token-number` to end (rejoined with `separator`). `include-prev`: `1` = return from start through `token-number` (rejoined). Both flags `1` = full source string. `source-format-string` may be any legal format token such as `<full-name>` or a literal string. |
| `<substr:start-position,end-position,source-format-string>` | Extracts characters from `source-format-string` between two positions (inclusive). Positions are 1-based; negative positions count from the right (`-1` = last character). Out-of-range positions are clamped to the nearest boundary. When the resolved start exceeds the resolved end, the range `(end, start]` is returned. `source-format-string` may be any legal format token or a literal. |

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
| `template`: `"<token:1,-,0,0,<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `13_` | Track number prefix, split by `-`. |
| `template`: `"<token:2,_-_,0,0,<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `Smog` | Artist name, split by `_-_`. |
| `template`: `"<token:2,_-_,1,0,<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `Smog_-_Cold_Blooded_Old_Times.mp3` | Artist and title, include-next. |
| `template`: `"<substr:1,5,<file-name>>"` | `MyTestFileName.123` | `MyTes` | First 5 chars of prefix. |
| `template`: `"<substr:5,-6,<full-name>>"` | `MyTestFileName.123` | `stFileNam` | Positive start, negative end. |
| `template`: `"<substr:-1,2,<file-extension>45>"` | `MyTestFileName.123` | `2345` | Crossed positions: extension `.123` + literal `45` → `.12345`; range `(2,6]`. |

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
