# MFR7 filter map

Maps legacy `[FilterInfo]` entries to finebytes types.

**finebytes Help (shipped):** repo-root `help/{Type}.html` (e.g. `SpaceCharacter.html`), copied beside the exe. Catalog sets `HelpFileName` by convention — see `docs/plans/filter-html-help.plan.md`.

**MFR7 Help (legacy reference only):** install or `D:\Devl\mfr7\Site\finebytes\mfr\Help\`. Screenshots: `Help/Images/<stem>.gif` unless noted. The **Help file** column below is the MFR7 basename — do not use these names for finebytes shipped help.

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

| MFR7 display name           | MFR7 class                | MFR7 Help file (legacy)               | finebytes `Type`            | finebytes Help                   | Notes                                                            |
| --------------------------- | ------------------------- | ------------------------------------- | --------------------------- | -------------------------------- | ---------------------------------------------------------------- |
| Capitalize After            | `CapitalizeAfterFilter`   | `capafterfilter.html`                 | `CapitalizeAfter`           | `CapitalizeAfter.html`           |                                                                  |
| Casing List                 | `CasingListFilter`        | `casinglistfilter.html`               | `CasingList`                | `CasingList.html`                |                                                                  |
| Letters Case                | `LettersCaseFilter`       | `letterscasefilter.html`              | `LettersCase`               | `LettersCase.html`               |                                                                  |
| Sentence End Characters     | `SentenceEndCharsFilter`  | `sentendcharsfilter.html`             | `SentenceEndCharacters`     | `SentenceEndCharacters.html`     |                                                                  |
| Uppercase Initials          | `UppercaseInitialsFilter` | `uppercaseinitialsfilter.html`        | `UppercaseInitials`         | `UppercaseInitials.html`         |                                                                  |
| Remove Spaces               | `RemoveSpacesFilter`      | `remspacesfilter.html`                | `RemoveSpaces`              | `RemoveSpaces.html`              |                                                                  |
| Separate Capitalized Text   | `SeparateCapTextFilter`   | `sepcaptextfilter.html`               | `SeparateCapitalizedText`   | `SeparateCapitalizedText.html`   |                                                                  |
| Shrink Spaces               | `ShrinkSpacesFilter`      | `shrinkspacesfilter.html`             | `ShrinkSpaces`              | `ShrinkSpaces.html`              |                                                                  |
| Space After                 | `SpaceAfterFilter`        | `spaceafterfilter.html`               | `SpaceAfter`                | `SpaceAfter.html`                |                                                                  |
| Space Around                | `SpaceAroundFilter`       | `spacearoundfilter.html`              | `SpaceAround`               | `SpaceAround.html`               |                                                                  |
| Space Character             | `SpaceCharFilter`         | `spacecharfilter.html`                | `SpaceCharacter`            | `SpaceCharacter.html`            | Sets word separator for following filters                        |
| Extract Left                | `LeftExtractFilter`       | `leftextractfilter.html`              | `ExtractLeft`               | `ExtractLeft.html`               |                                                                  |
| Extract Right               | `RightExtractFilter`      | `rightextractfilter.html`             | `ExtractRight`              | `ExtractRight.html`              |                                                                  |
| Remove Duplicate Characters | `RemoveDuplicatesFilter`  | `remdupsfilter.html`                  | `ShrinkDuplicateCharacters` | `ShrinkDuplicateCharacters.html` | Renamed in rewrite                                               |
| Trim Between                | `TrimBetweenFilter`       | `trimbetweenfilter.html`              | `TrimBetween`               | `TrimBetween.html`               |                                                                  |
| Trim Left                   | `LeftTrimFilter`          | `lefttrimfilter.html`                 | `TrimLeft`                  | `TrimLeft.html`                  |                                                                  |
| Trim Right                  | `RightTrimFilter`         | `righttrimfilter.html`                | `TrimRight`                 | `TrimRight.html`                 |                                                                  |
| Trim Spaces Left            | `LeftTrimSpacesFilter`    | `lefttrimspacesfilter.html`           | `StripSpacesLeft`           | `StripSpacesLeft.html`           | Renamed in rewrite                                               |
| Trim Spaces Right           | `RightTrimSpacesFilter`   | `righttrimspacesfilter.html`          | `StripSpacesRight`          | `StripSpacesRight.html`          | Renamed in rewrite                                               |
| Cleaner                     | `CleanerFilter`           | `cleanerfilter.html`                  | `Cleaner`                   | `Cleaner.html`                   |                                                                  |
| Replace List                | `ReplaceListFilter`       | `replacelistfilter.html`              | `ReplaceList`               | `ReplaceList.html`               |                                                                  |
| Replacer                    | `ReplacerFilter`          | `replacerfilter.html`                 | `Replacer`                  | `Replacer.html`                  |                                                                  |
| Counter                     | `CounterFilter`           | `counterfilter.html`                  | `Counter`                   | `Counter.html`                   |                                                                  |
| Formatter                   | `FormatterFilter`         | `formatterfilter.html`                | `Formatter`                 | `Formatter.html`                 |                                                                  |
| Inserter                    | `InserterFilter`          | `inserterfilter.html`                 | `Inserter`                  | `Inserter.html`                  |                                                                  |
| Name List                   | `NameListFilter`          | `namelistfilter.html`                 | `NameList`                  | `NameList.html`                  |                                                                  |
| Token Mover                 | `TokenMoverFilter`        | `tokenmoverfilter.html`               | `TokenMover`                | `TokenMover.html`                |                                                                  |
| Audio Tag Remover           | `AudioTagRemoverFilter`   | `id3tagremoverfilter.html`            | `TagRemover`                | `TagRemover.html`                | Display name kept                                                |
| Audio Tag Setter            | `AudioTagSetterFilter`    | `id3tagsetterfilter.html`             | `AudioTagSetter`            | `AudioTagSetter.html`            |                                                                  |
| ID3v2 Field Setter          | `ID3V2SetterFilter`       | `id3v2fieldsetterfilter.html`         | `Id3v2FieldSetter`          | `Id3v2FieldSetter.html`          | Display name kept                                                |
| Attributes Setter           | `AttrsSetterFilter`       | `attrfilter.html`                     | `AttributesSetter`          | `AttributesSetter.html`          |                                                                  |
| Date/Time Setter            | `DateTimeSetterFilter`    | `datefilter.html` / `timefilter.html` | `DateTimeSetter`            | `DateTimeSetter.html`            | Combined MFR7 Date + Time Setter; optional `setDate` / `setTime` |
| Time Shifter                | `TimeShifterFilter`       | `timeshiftfilter.html`                | `TimeShifter`               | `TimeShifter.html`               | MFR7 screenshot: `timeshifterfilter.png`                         |
| Fix Leading 0's             | `FixLZFilter`             | `fixlzfilter.html`                    | `FixLeadingZeros`           | `FixLeadingZeros.html`           | Display name kept                                                |
| Path Mover                  | `PathMoverFilter`         | `moverfilter.html`                    | `PathMover`                 | `PathMover.html`                 |                                                                  |
| Strip Parentheses           | `StripParFilter`          | `stripparfilter.html`                 | `StripParentheses`          | `StripParentheses.html`          |                                                                  |

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
