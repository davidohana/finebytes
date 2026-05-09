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
| `<file-date:format,date-kind>` | Arguments are **required**. **Both** parts: a .NET date format string, then a comma, then `date-kind`. Split uses the **last** comma so `format` may contain commas. `date-kind` (case-insensitive): `creation`, `lastWrite`, `lastAccess`. Example: `<file-date:dd-MM-yyyy,creation>`. |
| `<drive-letter>` | Drive letter of the file's location (e.g. `C:`). Returns `$` for network (UNC) paths. |
| `<label>` | Volume label of the drive that holds the file. |
| `<file-count>` | Number of files and folders in the same directory (non-recursive). Empty when directory does not exist. |
| `<file-size>` | File size, auto-selecting the largest unit (e.g. `1 KB`, `2 MB`). |
| `<file-size:unit>` | File size in a specific unit. `unit`: omit or `auto` (auto-scale), `b`/`bytes`, `kb`, `mb`, `gb` (case-insensitive). |
| `<file-size:unit,decimals>` | File size with the specified number of decimal places (default `0`). |

#### Counter and time

| Token | Output |
|--------|--------|
| `<now>` | Current UTC time, ISO-8601 style. |
| `<now:format>` | Current UTC time formatted with a .NET format string. |
| `<item-count>` | Total items in the current rename list (no arguments). Populated during preview/commit. |
| `<name-list-entry:name-list-file-path>` | Uses Name List parsing rules (comment lines are skipped; blank lines are preserved; at least one entry required), then returns the entry at the item's rename-list position. Throws a user-facing error when item index exceeds the parsed entry count. |
| `<random-char:low,high>` | One random character, uniformly chosen between inclusive endpoints (first character of each side is used; order may be reversed). Examples: `<random-char:A,Z>`, `<random-char:0,9>`. |
| `<counter>` | Rename-list index as `initial` + `step`×index using defaults `initial=1`, `step=1`, `padding=none`, `length=2`, `resetScope=global`. With `padding=none`, output has no leading zeros. |
| `<counter:initial=…,step=…,padding=…,length=…,resetScope=…>` | Named options, **order-independent** (spaces around `,` and `=` optional). Omitted options use `initial=1`, `step=1`, `padding=none`, `length=2`, `resetScope=global`. Value: `initial` + `step`×index. `padding`: `none`, `auto` (width from list scope), or `fixed` (pad to `length`, minimum digit width `1`). `resetScope`: `global` vs `perFolder` (restart per folder). |

#### Token extraction

| Token | Output |
|-------|--------|
| `<token:tokenNumber=…,separator=…,includeNext=…,includePrev=…,source=…>` | **Named** options, **order-independent** (spaces optional). Resolves `source`, splits by `separator`, then returns the 1-based `tokenNumber` part. With `includeNext=true`, returns that part through the end (rejoined with `separator`); with `includePrev=true`, returns from the start through that part (rejoined); both `true` returns the full resolved string. `source` may contain nested `<…>` tokens; commas inside balanced angle brackets are not option separators. `includeNext` / `includePrev`: `true` / `false` (case-insensitive). |
| `<substr:start=…,end=…,source=…>` | Named options, **order-independent** (spaces optional). Extracts characters from `source` between two positions (inclusive). Positions are 1-based; negative positions count from the right (`-1` = last character). Out-of-range positions are clamped to the nearest boundary. When the resolved start exceeds the resolved end, the range `(end, start]` is returned. `source` may contain nested `<…>` tokens; commas inside balanced angle brackets are not option separators. |

Unknown token names cause an error at runtime.

## Examples

Assume directory `Music\My Album\` when using `<parent-folder>`. Counter rows use the **global** index passed to the filter.

| Options | Before | After | Comment |
|---------|--------|-------|---------|
| `template`: `"<file-name>"` | `song` | `song` | |
| `template`: `"<parent-folder>"`<br>file under `Music\My Album\` | `ignored` | `My Album` | |
| `template`: `"<parent-folder:2>"`<br>file under `Music\My Album\` | `ignored` | `Music` | Level 2 = grandparent. |
| `template`: `"<file-date:dd-MM-yyyy,creation>"` | `ignored` | `07-04-2023` | Creation time, common date layout. |
| `template`: `"<file-date:yyyy,creation>"` | `ignored` | `2024` | Creation year. |
| `template`: `"<file-date:yyyy,lastWrite>"` | `ignored` | `2021` | Last-write year. |
| `template`: `"<file-size>"` | `ignored` | `1 KB` | Auto unit, 0 decimals. |
| `template`: `"<file-size:mb,2>"` | `ignored` | `1.50 MB` | MB, 2 decimal places. |
| `template`: `"<drive-letter>"` | `ignored` | `C:` | Drive letter of the file. |
| `template`: `"<counter:initial=10,step=2,padding=fixed,length=4,resetScope=global>"`<br>global index: `3` | `ignored` | `0016` | `10 + 2×3`, fixed width `4`. |
| `template`: `"<token:tokenNumber=1,separator=-,includeNext=false,includePrev=false,source=<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `13_` | Track number prefix, split by `-`. |
| `template`: `"<token:tokenNumber=2,separator=_-_,includeNext=false,includePrev=false,source=<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `Smog` | Artist name, split by `_-_`. |
| `template`: `"<token:tokenNumber=2,separator=_-_,includeNext=true,includePrev=false,source=<full-name>>"` | `13_-_Smog_-_Cold_Blooded_Old_Times.mp3` | `Smog_-_Cold_Blooded_Old_Times.mp3` | Artist and title, include-next. |
| `template`: `"<substr:start=1,end=5,source=<file-name>>"` | `MyTestFileName.123` | `MyTes` | First 5 chars of prefix. |
| `template`: `"<substr:start=5,end=-6,source=<full-name>>"` | `MyTestFileName.123` | `stFileNam` | Positive start, negative end. |
| `template`: `"<substr:start=-1,end=2,source=<file-extension>45>"` | `MyTestFileName.123` | `2345` | Crossed positions: extension `.123` + literal `45` → `.12345`; range `(2,6]`. |

For sequential numbering without a full template, see [Counter](Counter.md).

## Sample preset (JSON)

The `filter` object inside a chain step ([preset shape](../README.md#preset-shape)). When targeting **Prefix**, `<file-name>` is the original file prefix.

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
