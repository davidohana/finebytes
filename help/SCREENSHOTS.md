# Help screenshots

## Filter options

Capture each filter’s Filter Configuration **options body** (no title bar) and
save as `help/images/{Type}.png` (catalog type name, e.g. `SpaceCharacter.png`).
HTML pages live under `help/filters/{group}/{Type}.html` and reference
`../../images/{Type}.png`.

Optionless filters (`RemoveSpaces`, `ShrinkSpaces`, `SeparateCapitalizedText`,
`StripSpacesLeft`, `StripSpacesRight`, `UppercaseInitials`) have **no** screenshot
figure — their help pages say the filter has no options.

### Capture tips (filters)

1. Regenerate with `just capture-help-filters` (or
   `MFR_CAPTURE_HELP_SCREENSHOTS=1 dotnet test ./Mfr.Tests/Mfr.Tests.csproj --filter FullyQualifiedName~HelpScreenshotCaptureTests`
   then `just sync-help-img-dims`).
1. Or run the UI (`just run-ui`), add the filter, select it, and crop the options body.
1. Prefer PNG; keep widths roughly 400–640px when practical. Help CSS uses
   `max-width: 640px` with `height: auto` so HTML width/height attrs do not stretch.
1. Missing images still load: `help.css` shows a checkerboard placeholder behind broken `img`.
1. This file is **not** copied to the app output directory.

### Filter checklist

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

## UI / shell

Save under `help/images/ui/`. Pages under `help/ui/` reference `../images/ui/…`.

### Capture tips (UI)

1. Regenerate with `just capture-help-ui` (P0 shell + P1 dialogs/tools + P2 guide, then
   hotspots annotate + HTML width/height sync). Or run both filter + UI captures with
   `just capture-help`.
1. Seed a sample folder + Rename List rows + a short Filter Chain so panes look live
   (the capture tests do this automatically).
1. Hotspots: `just annotate-help-hotspots` redraws `images/ui/hotspots.png` from
   `main-window.png` and refreshes percentage hotspot boxes in `help/ui/parts.html`.
   Needs Pillow once: `.venv/bin/pip install -r help/tools/requirements.txt`.
1. `just sync-help-img-dims` rewrites `<img width/height>` from on-disk PNG sizes (stdlib).

### UI checklist (P0)

- [x] `images/ui/main-window.png` — Main window (`ui.html`, `parts.html` base shot)

- [x] `images/ui/hotspots.png` — Labeled main-window regions + percentage hotspots (`parts.html`;
  regenerate with `just annotate-help-hotspots` after recapturing `main-window.png`)

- [x] `images/ui/file-list.png` — File List (`fileexp.html`)

- [x] `images/ui/rename-list.png` — Rename List (`renamelist.html`)

- [x] `images/ui/available-filters.png` — Available Filters (`availfilterlist.html`)

- [x] `images/ui/filter-chain.png` — Filter Chain (`appliedfilterlist.html`)

- [x] `images/ui/filter-configuration.png` — Filter Configuration pane (`filterconfigpanel.html`)

### UI checklist (P1)

- [x] `images/ui/filter-options.png` — Filter Options dialog (`filteropts.html`)

- [x] `images/ui/options.png` — Options dialog (`optionswin.html`)

- [x] `images/ui/preset-manager.png` — Preset Manager (`presetmanager.html`)

- [x] `images/ui/rename-log.png` — Rename Log (`log.html`)

- [x] `images/ui/format-editor.png` — Format Editor (`formateditor.html`)

- [x] `images/ui/field-shuttle.png` — Select Fields (`fieldselector.html`)

- [x] `images/ui/auto-sort.png` — Auto-Sort (`sorteditor.html`)

- [x] `images/ui/visual-trim.png` — Visual Trim Helper (`visualtrimmer.html`)

- [x] `images/ui/status-bar.png` — Status bar (`statusbar.html`)

### Guide checklist (P2)

- [x] `images/guide/tutorial-overview.png` — Tutorial overview (reuses `ui/main-window.png` when present)
  (`tutorial.html`)

- [x] `images/guide/apply-go.png` — Toolbar GO (`applychanges.html`)

- [x] `images/guide/undo-last.png` — Undo Last prepared session (`undolast.html`)

- [x] `images/guide/save-preset.png` — Save preset dialog (`savepreset.html`)

- [x] `images/guide/reset-config.png` — Reset Configuration confirm (`resetconfig.html`)
