# Casing List factory words button

## Status

| Todo | Status |
|------|--------|
| curate-default-words | Done |
| editor-load-button | Done |
| sample-reuse | Done |
| docs-tests | Done |

## Decisions (locked)

- **List:** new longer curated factory set — English title-case exceptions **plus** common rename/media acronyms and a short set of multilingual particles (not only today’s ~70 skip-words).
- **Button:** replaces the Words box (no merge, no confirm).
- **Reuse:** one shared list for the button and the sample “clean names” preset; small-word subset stays the single source for Letters Case capitalize skip-words.
- **Add / title-bar Reset:** unchanged — new Casing List still starts with an **empty** word list; ↺ still resets to empty factory. The button is the only way to inject the curated list into an editor session (user can then 📌 Save as default if they want it on every add).

## Data model (`Mfr.Filters`)

Add a shared owner under Case, e.g. [`Mfr.Filters/Case/CasingListFilter.cs`](../../Mfr.Filters/Case/CasingListFilter.cs) (or a tiny sibling `CommonCasingWords.cs` if the array is long):

```csharp
// On CasingListOptions (or static helper used by it):
public static IReadOnlyList<string> DefaultWords { get; } = [ /* curated */ ];
```

**Composition (concrete categories to curate in code):**

1. **Title-case exceptions (lowercase)** — keep today’s [`LettersCaseOptions.DefaultCapitalizeSkipWords`](../../Mfr.Filters/Case/LettersCaseFilter.cs) entries (`a`, `and`, `the`, `of`, …).
2. **Multilingual particles (lowercase)** useful in world music/file titles — e.g. `de`, `del`, `des`, `di`, `da`, `du`, `el`, `la`, `le`, `les`, `und`, `von`, `van`, `der`, `die`, `das`, `y`, `et`, `en` (skip duplicates already in (1)).
3. **Common rename/media acronyms (exact casing)** — e.g. `DJ`, `MC`, `EP`, `LP`, `CD`, `DVD`, `OST`, `VIP`, `RMX`, `MIX`, `HD`, `UHD`, `4K`, `8K`, `HDR`, `MP3`, `FLAC`, `AAC`, `WAV`, `PDF`, `USA`, `UK`, `EU`, `NYC`, `TV`, `ID`, plus roman numerals `II`–`XII` (omit bare `I` / `V` to avoid pronoun/letter noise).

Refactor Letters Case so `DefaultCapitalizeSkipWords` is **exactly** the title-case exception subset of that shared source (same array reference or explicit slice) — no duplicated English list that can drift.

Wire sample preset in [`SamplePresetDefinitions.cs`](../../Mfr.Engine/Presets/Samples/SamplePresetDefinitions.cs) to `CasingListOptions.DefaultWords` instead of `LettersCaseOptions.DefaultCapitalizeSkipWords`.

Keep `CasingListFilter()` ctor Words = `[]`.

## UI

Mirror the DateTime Setter “Current” control pattern:

- [`CasingListFilterEditorView.axaml`](../../Mfr.App.Ui/Views/FilterEditors/Case/CasingListFilterEditorView.axaml) — under/beside Words: compact `Button` (`tool-chrome filter-editor-button`), e.g. content **Load defaults**, `x:Name="LoadDefaultsButton"`, rich tip explaining replace with the factory casing list.
- [`CasingListFilterEditorViewModel.cs`](../../Mfr.App.Ui/ViewModels/FilterEditors/Case/CasingListFilterEditorViewModel.cs) — `[RelayCommand] LoadDefaults()` sets `WordsText = CasingListParser.FormatEditorText(CasingListOptions.DefaultWords)` (property change → existing live apply). Flush any pending list-text debounce first if needed (same as checkbox path).

Do **not** change Filter Configuration title-bar ↺ / 📌 behavior.

## Docs / help

- [`Mfr.Filters/docs/Case/CasingList.md`](../../Mfr.Filters/docs/Case/CasingList.md) — note Load defaults + that the curated list is shared with the sample preset.
- [`help/filters/case/CasingList.html`](../../help/filters/case/CasingList.html) — document the button under Options; keep short examples as-is.
- Recapture [`help/images/CasingList.png`](../../help/images/CasingList.png) if layout height changes (see `help/SCREENSHOTS.md` / existing capture test hook).

## Tests

- Filter/unit: `DefaultWords` non-empty, within `ListEntryLength` limits, `CasingListParser.BuildMap` succeeds; Letters Case skip list still equals the expected exception subset.
- VM: `LoadDefaultsCommand` replaces `WordsText` / options Words with `DefaultWords` even when the box already had custom text.
- Sample: [`SamplePresetCatalogTests`](../../Mfr.Tests/Engine/SamplePresetCatalogTests.cs) asserts Casing List Words == `CasingListOptions.DefaultWords`.
- Headless (light): button present and command executable on the editor view (same style as DateTime Setter Current tests).

## Out of scope

- Changing filter-add factory to pre-fill the long list.
- Import/export of external `.txt` files (MFR7 file path UX).
- Merge/append mode or confirmation dialog.
