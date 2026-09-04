# MFR7 formatter tokens

Formatter parameters in MFR7 live in `D:\Devl\mfr7\Core\MfrFilters\FormattingParams\`. Help index: `Help/fp.html` → group pages (`filenamefp.html`, `filepropsfp.html`, `generalfp.html`, `id3fp.html`, …).

finebytes implements tokens under `Mfr.Filters/Formatting/Tokens/` as `IFormatToken` types resolved by `FormatterFilter`.

## Parameter groups (help)

| Help page | MFR7 folder | finebytes folder |
|-----------|-------------|------------------|
| `filenamefp.html` | `FormattingParams/FileName/` | `Tokens/FileName/` |
| `filepropsfp.html` | `FormattingParams/FileProps/` | `Tokens/FileProperties/` |
| `generalfp.html` | `FormattingParams/General/` | `Tokens/Meta/`, `Tokens/Session/`, `Tokens/Generators/` |
| `id3fp.html`, `id3v2fp.html` | `FormattingParams/Audio/` | `Tokens/Audio/` |
| `basicimagefp.html`, `eximagefp.html` | (image props via MetadataExtractor) | `Tokens/Image/`, `Tokens/Exif/` |
| `clipboardtextfp.html` | clipboard | (not yet ported — check finebytes) |

## Class → token map

| MFR7 class | Help | finebytes token(s) |
|------------|------|-------------------|
| `FileNameFormattingParameter` | `filenamefp.html` | `FileNameToken` |
| `ExtensionFormattingParameter` | `filenamefp.html` | `FileExtensionToken` |
| `FullNameFormattingParameter` | `filenamefp.html` | `FullNameToken` |
| `FolderFormattingParameter` | `filepropsfp.html` | `ParentFolderToken` |
| `DriveFormattingParameter` | `filepropsfp.html` | `DriveLetterToken` |
| `VolumeLabelFormattingParameter` | `filepropsfp.html` | `VolumeLabelToken` |
| `SizeFormattingParameter` | `filepropsfp.html` | `FileSizeToken` |
| `DateFormattingParameter` | `filepropsfp.html` | `FileDateToken` |
| `FileCountFormattingParameter` | `filepropsfp.html` | `FileCountToken` |
| `CounterFormattingParameter` | `counterfp.html` | `CounterToken` |
| `ItemCountFormattingParameter` | `itemcountfp.html` | `ItemCountToken` |
| `NameListFormattingParameter` | `namelistfp.html` | `NameListEntryToken` |
| `SubstringFormattingParameter` | `substringfp.html` | `SubstringToken` |
| `TokenFormattingParameter` | `tokenfp.html` | `TokenExtractToken` |
| `RandomCharFormattingParameter` | (generalfp) | `RandomCharToken` |
| `Id3FieldFormattingParameter` | `id3fp.html` | `SemanticAudioFieldTokens` |
| `Id3V2FieldFormattingParameter` | `id3v2fp.html` | `Id3v2Token` |

Also check `FullPathToken`, `FullPathLengthToken`, `FileNameLengthToken`, `FileOrFolderToken`, `FileNameNumericValueToken`, `NowToken`, `ExifToken`, `ExifDateToken`, `ExifPropertyTokens`, `ImagePropertyTokens`, `MediaPropertyTokens`, `MpegAudioPropertyTokens` — added or reorganized in the rewrite; grep finebytes for the angle-bracket token name if no MFR7 class name match.

## How to read a token spec

1. Open the `*fp.html` help page for syntax (`<token:options>` patterns) and examples.
2. Open matching `Help/Images/*fp.gif` for the format editor UI.
3. Read `*FormattingParameter.cs` for parse rules, defaults, and edge cases.
4. Read finebytes `*Token.cs` and `*TokenTests.cs` for current parity and tests citing MFR7.

## Format editor

Shared UI: `Help/formateditor.html`, images `fmtbtn1.gif`, `fmtbtn2.gif`, `formateditor.gif`. Legacy editor code: search `FormatEditor` under `Core/MFRGui/` and `Core/MfrFilters`.
