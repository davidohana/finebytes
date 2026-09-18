# Filter help screenshots

Capture each filter’s Filter Configuration **options body** (no title bar) and
save as `help/images/{Type}.png` (catalog type name, e.g. `SpaceCharacter.png`).
HTML pages live under `help/filters/{group}/{Type}.html` and reference
`../../images/{Type}.png`.

Optionless filters (`RemoveSpaces`, `ShrinkSpaces`, `SeparateCapitalizedText`,
`StripSpacesLeft`, `StripSpacesRight`, `UppercaseInitials`) have **no** screenshot
figure — their help pages say the filter has no options.

## Capture tips

1. Regenerate with
   `MFR_CAPTURE_HELP_SCREENSHOTS=1 dotnet test ./Mfr.Tests/Mfr.Tests.csproj --filter FullyQualifiedName~HelpScreenshotCaptureTests`
   (uses Skia headless via `TestAppBuilder`).
1. Or run the UI (`just run-ui`), add the filter, select it, and crop the options body.
1. Prefer PNG; keep widths roughly 480–640px when practical.
1. Missing images still load: `help.css` shows a checkerboard placeholder behind broken `img`.
1. This file is **not** copied to the app output directory.

## Checklist

- [x] `images/CapitalizeAfter.png` — Capitalize After

- [x] `images/CasingList.png` — Casing List

- [x] `images/LettersCase.png` — Letters Case

- [x] `images/SentenceEndCharacters.png` — Sentence End Characters

- [x] `images/SpaceAfter.png` — Space After

- [x] `images/SpaceAround.png` — Space Around

- [x] `images/SpaceCharacter.png` — Space Character

- [x] `images/ExtractLeft.png` — Extract Left

- [x] `images/ExtractRight.png` — Extract Right

- [x] `images/ShrinkDuplicateCharacters.png` — Shrink Duplicate Characters

- [x] `images/TrimBetween.png` — Trim Between

- [x] `images/TrimLeft.png` — Trim Left

- [x] `images/TrimRight.png` — Trim Right

- [x] `images/Cleaner.png` — Cleaner

- [x] `images/ReplaceList.png` — Replace List

- [x] `images/Replacer.png` — Replacer

- [x] `images/Counter.png` — Counter

- [x] `images/Formatter.png` — Formatter

- [x] `images/Inserter.png` — Inserter

- [x] `images/NameList.png` — Name List

- [x] `images/TokenMover.png` — Token Mover

- [x] `images/AudioTagSetter.png` — Audio Tag Setter

- [x] `images/Id3v2FieldSetter.png` — ID3v2 Field Setter

- [x] `images/TagRemover.png` — Audio Tag Remover

- [x] `images/AttributesSetter.png` — Attributes Setter

- [x] `images/DateTimeSetter.png` — Date/Time Setter

- [x] `images/TimeShifter.png` — Time Shifter

- [x] `images/FixLeadingZeros.png` — Fix Leading 0's

- [x] `images/PathMover.png` — Path Mover

- [x] `images/StripParentheses.png` — Strip Parentheses
