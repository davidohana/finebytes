# MFR7 filter map

Maps legacy `[FilterInfo]` entries to finebytes types. Help files live under `Help/` (install or `D:\Devl\mfr7\Site\finebytes\mfr\Help\`). Screenshots: `Help/Images/<stem>.gif` unless noted.

## Group names

| MFR7 `FilterInfo` group | finebytes `FilterGroup` |
| ----------------------- | ----------------------- |
| Case                    | `Case`                  |
| Space                   | `Space`                 |
| Trimmer                 | `Trimming`              |
| Replace                 | `Replace`               |
| Format                  | `Formatting`            |
| ID3                     | `Audio`                 |
| Attrs                   | `Attributes`            |
| Misc                    | `Misc`                  |

## Shipped filters

| MFR7 display name           | MFR7 class                | Help file                             | finebytes `Type`            | Notes                                                            |
| --------------------------- | ------------------------- | ------------------------------------- | --------------------------- | ---------------------------------------------------------------- |
| Capitalize After            | `CapitalizeAfterFilter`   | `capafterfilter.html`                 | `CapitalizeAfter`           |                                                                  |
| Casing List                 | `CasingListFilter`        | `casinglistfilter.html`               | `CasingList`                |                                                                  |
| Letters Case                | `LettersCaseFilter`       | `letterscasefilter.html`              | `LettersCase`               |                                                                  |
| Sentence End Characters     | `SentenceEndCharsFilter`  | `sentendcharsfilter.html`             | `SentenceEndCharacters`     |                                                                  |
| Uppercase Initials          | `UppercaseInitialsFilter` | `uppercaseinitialsfilter.html`        | `UppercaseInitials`         |                                                                  |
| Remove Spaces               | `RemoveSpacesFilter`      | `remspacesfilter.html`                | `RemoveSpaces`              |                                                                  |
| Separate Capitalized Text   | `SeparateCapTextFilter`   | `sepcaptextfilter.html`               | `SeparateCapitalizedText`   |                                                                  |
| Shrink Spaces               | `ShrinkSpacesFilter`      | `shrinkspacesfilter.html`             | `ShrinkSpaces`              |                                                                  |
| Space After                 | `SpaceAfterFilter`        | `spaceafterfilter.html`               | `SpaceAfter`                |                                                                  |
| Space Around                | `SpaceAroundFilter`       | `spacearoundfilter.html`              | `SpaceAround`               |                                                                  |
| Space Character             | `SpaceCharFilter`         | `spacecharfilter.html`                | `SpaceCharacter`            | Sets word separator for following filters                        |
| Extract Left                | `LeftExtractFilter`       | `leftextractfilter.html`              | `ExtractLeft`               |                                                                  |
| Extract Right               | `RightExtractFilter`      | `rightextractfilter.html`             | `ExtractRight`              |                                                                  |
| Remove Duplicate Characters | `RemoveDuplicatesFilter`  | `remdupsfilter.html`                  | `ShrinkDuplicateCharacters` | Renamed in rewrite                                               |
| Trim Between                | `TrimBetweenFilter`       | `trimbetweenfilter.html`              | `TrimBetween`               |                                                                  |
| Trim Left                   | `LeftTrimFilter`          | `lefttrimfilter.html`                 | `TrimLeft`                  |                                                                  |
| Trim Right                  | `RightTrimFilter`         | `righttrimfilter.html`                | `TrimRight`                 |                                                                  |
| Trim Spaces Left            | `LeftTrimSpacesFilter`    | `lefttrimspacesfilter.html`           | `StripSpacesLeft`           | Renamed in rewrite                                               |
| Trim Spaces Right           | `RightTrimSpacesFilter`   | `righttrimspacesfilter.html`          | `StripSpacesRight`          | Renamed in rewrite                                               |
| Cleaner                     | `CleanerFilter`           | `cleanerfilter.html`                  | `Cleaner`                   |                                                                  |
| Replace List                | `ReplaceListFilter`       | `replacelistfilter.html`              | `ReplaceList`               |                                                                  |
| Replacer                    | `ReplacerFilter`          | `replacerfilter.html`                 | `Replacer`                  |                                                                  |
| Counter                     | `CounterFilter`           | `counterfilter.html`                  | `Counter`                   |                                                                  |
| Formatter                   | `FormatterFilter`         | `formatterfilter.html`                | `Formatter`                 |                                                                  |
| Inserter                    | `InserterFilter`          | `inserterfilter.html`                 | `Inserter`                  |                                                                  |
| Name List                   | `NameListFilter`          | `namelistfilter.html`                 | `NameList`                  |                                                                  |
| Token Mover                 | `TokenMoverFilter`        | `tokenmoverfilter.html`               | `TokenMover`                |                                                                  |
| Audio Tag Remover           | `AudioTagRemoverFilter`   | `id3tagremoverfilter.html`            | `TagRemover`                | Display name kept                                                |
| Audio Tag Setter            | `AudioTagSetterFilter`    | `id3tagsetterfilter.html`             | `AudioTagSetter`            |                                                                  |
| ID3v2 Field Setter          | `ID3V2SetterFilter`       | `id3v2fieldsetterfilter.html`         | `Id3v2FieldSetter`          | Display name kept                                                |
| Attributes Setter           | `AttrsSetterFilter`       | `attrfilter.html`                     | `AttributesSetter`          |                                                                  |
| Date/Time Setter            | `DateTimeSetterFilter`    | `datefilter.html` / `timefilter.html` | `DateTimeSetter`            | Combined MFR7 Date + Time Setter; optional `setDate` / `setTime` |
| Time Shifter                | `TimeShifterFilter`       | `timeshiftfilter.html`                | `TimeShifter`               | Screenshot: `timeshifterfilter.png`                              |
| Fix Leading 0's             | `FixLZFilter`             | `fixlzfilter.html`                    | `FixLeadingZeros`           | Display name kept                                                |
| Path Mover                  | `PathMoverFilter`         | `moverfilter.html`                    | `PathMover`                 |                                                                  |
| Strip Parentheses           | `StripParFilter`          | `stripparfilter.html`                 | `StripParentheses`          |                                                                  |

## Not yet in finebytes (MFR7 only)

| MFR7 display name | MFR7 class     | Help file           |
| ----------------- | -------------- | ------------------- |
| Set from FreeDB   | `FreeDbFilter` | `freedbfilter.html` |

## Finding unmapped filters

Regenerate the shipped list when in doubt:

```powershell
rg "\[FilterInfo" "D:\Devl\mfr7\Core\MfrFilters\Filters" -g "*Filter.cs" -A 4
```

Compare output to `Mfr.Filters` types and `FilterCatalog.Entries`.

## Legacy source layout

```text
D:\Devl\mfr7\Core\
  MfrFilters\Filters\     # filter + *FilterEditor.cs
  MfrFilters\FormattingParams\
  MFRGui\Forms\           # shell, rename list, filter lists
  MfrLib\Filters\         # FilterGroups, preset plumbing
  MfrConsole\             # MFRC CLI
  FiltersBase\            # Filter base types, FilterInfoAttribute
```
