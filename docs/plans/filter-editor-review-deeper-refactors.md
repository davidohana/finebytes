---
name: Filter editor review deeper refactors
overview: Ranked follow-ups from the F5 filter-editor + filter code-review pass that were proposed but not applied. Prefer high cost-to-value first.
---

# Filter editor review — deeper refactors

Synthesized from per-filter worktree reviews and [cross-editor dedup explore](https://github.com/davidohana/finebytes). Applied fixes landed via PRs #4, #6–#23 (and early local cherry-picks that were later opened as #12–#23). This doc lists **not done** work only.

## Already shared (do not re-merge)

- Count L/R ×4 → one VM + `ICountOptionsFilter`
- Space After + Around → one SpaceTrigger editor
- Capitalize After + Sentence End → one CharacterList editor
- Date + Time Setter → one DateTimeSetter **filter** + editor
- Replacer mode/match UI fieldsets reused by Replace List
- Replace List engine → `ReplacerFilter.ReplaceSegment`
- Timestamp field combo catalog → `TimestampFieldChoice.All` / `For`

## Ranked follow-ups (best cost-to-value first)

### 1. Shared timestamp ApplyCore mutator — **done**

`TimestampFields.Update` owns the Creation / LastWrite / LastAccess switch; Date Setter, Time Setter, and Time Shifter pass field-specific transforms.

### 2. Shared `ReplacerMatchOptions` record — **done**

`ReplacerOptions` / `ReplaceListOptions` nest one `match` record (Mode + CaseSensitive + ReplaceAll + WholeWord); editor VMs share `ReplacerMatchOptionsEditor`. Defaults: Replacer `WholeWord` false, Replace List true.

### 3. Sentence End side-effect outside scoped transform — **done**

`SentenceEndCharactersFilter` is a state-only `BaseFilter` (no `Target` / `ApplyScope`); `ApplyCore` always sets `SentenceEndChars`.

### 4. Isolate remaining editor tests like Mover — **done**

Per-editor VM/view suites live under `Mfr.Tests/Ui/FilterEditors/<Group>/`; shared host selection/chrome stays in `FilterEditorViewModelTests` / `FilterEditorViewTests`. Headless hosts use `FilterEditorTestUi.ShowFilterEditorPanes`.

### 5. Multiline list editor / line-iteration helper — **medium**

- **Sites:** Name List + Replace List AXAML Entries fieldsets; parsers’ Format/Parse line loops (blank-preserving vs blank-skipping)
- **Target:** shared multiline Entries control (DPs for tip/watermark); optional shared `EnumerateLines` only where semantics match — **do not** force Casing List into multiline
- **Value:** less AXAML drift; one line-iteration owner where safe
- **Cost:** medium (control + tip parameterization + headless name updates)

### 6. CasingList Format/Parse ownership — **done in review**

Editor now uses `CasingListParser.ParseEditorText` / `FormatEditorText`. Keep domain validation at `BuildMap` / Setup.

### 7. Space Character MFR7 empty-Other validation — **medium**

- **Sites:** `_ResolveSpaceCharacter` empty Other → `' '`
- **Target:** don’t persist / don’t apply when Other + empty (or clear error)
- **Value:** closes silent Space fallback drift vs MFR7
- **Cost:** small UX decision + tests

### 8. `CountFilterOptions.ClampToLength` helper — **medium**

- **Sites:** identical clamp + slice in four Count filters
- **Target:** one clamp API; filters keep one-line transforms
- **Value:** one owner for clamp policy
- **Cost:** low; four call sites

### 9. Cache Setup HashSets (skip words / triggers) — **low–medium**

- **Sites:** Letters Case capitalize skip-words; Space After/Around / Capitalize After trigger sets
- **Target:** instance fields cleared/assigned in `_Setup` (BaseFilter cache rules)
- **Value:** less alloc per file
- **Cost:** low LOC; only worth if hot

### 10. Cleaner → `FilterEditorLabeledRow` — **medium** (UI polish)

- **Sites:** Cleaner AXAML hand-rolled Grid vs Replacer labeled rows
- **Target:** one labeled-row for “Characters to clean”
- **Value:** consistent filter-editor chrome
- **Cost:** low; 1 AXAML + possible headless tweak

### 11. Replace List Regex compile at Setup — **medium**

- **Sites:** Replacer validates invalid Regex in `_Setup`; Replace List can fail later via `ReplaceSegment`
- **Target:** validate/compile Regex entries in Replace List `_Setup`
- **Value:** fail early; share compile helper with Replacer
- **Cost:** medium; list of patterns

## Explicit non-goals / skip

| Idea                                              | Why skip                                                       |
| ------------------------------------------------- | -------------------------------------------------------------- |
| Merge Counter ↔ Inserter ↔ Token Mover “position” | Different models (placement enum vs char index vs token index) |
| Merge Replacer + Replace List into one editor     | Primary surfaces differ; mode/match already shared             |
| Unify Space After/Around option JSON schema       | Editor already shared; persist key rename for little gain      |
| Merge four Count filters into one type + enum     | High JSON/palette/docs churn                                   |
| Shared single-char editor VM                      | Only Shrink Dup today; weak until a second identical editor    |
| Force-merge list parsers (Name/Replace/Casing)    | Intentional blank / `=>` / whitespace semantics differ         |
| Non-regex `CharacterRunHelpers` rewrite           | Micro-gain; shared Shrink Spaces risk                          |

## PR index (this review pass)

| PR  | Filter / area     |
| --- | ----------------- |
| #4  | Counter           |
| #6  | Replace List      |
| #7  | Name List         |
| #8  | Replacer          |
| #9  | Inserter          |
| #10 | Token Mover       |
| #11 | Date/Time Setter  |
| #12 | Space Character   |
| #13 | Trim Between      |
| #14 | Strip Parentheses |
| #15 | Casing List       |
| #16 | Character List    |
| #17 | Space Trigger     |
| #18 | Letters Case      |
| #19 | Count L/R         |
| #20 | Shrink Duplicate  |
| #21 | Cleaner           |
| #22 | Fix Leading Zeros |
| #23 | Mover             |

For Attributes / Audio F5 editors (#24–#30) follow-ups, see
`docs/plans/f5-attributes-audio-editors-review-deeper-refactors.md`.
