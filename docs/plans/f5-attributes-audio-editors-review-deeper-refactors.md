---
name: F5 Attributes/Audio editors review deeper refactors
overview: Ranked follow-ups from MFR skill reviews of PRs #24–#30 that were proposed but not applied in the autofix PR. Prefer high cost-to-value first.
---

# F5 Attributes / Audio editors review — deeper refactors

Synthesized from per-PR findings-only reviews of:

| PR  | Area                                                     |
| --- | -------------------------------------------------------- |
| #24 | Date/Time Setter merge                                   |
| #25 | Attributes Setter editor (tri-state → superseded by #29) |
| #26 | Time Shifter editor                                      |
| #27 | Audio Tag Remover editor                                 |
| #28 | Date/Time Setter illegal date/time                       |
| #29 | Attributes Setter On/Off/Keep radios                     |
| #30 | Date/Time Setter 2100 cap + field sync                   |

High-confidence autofixes landed separately (shared `TimestampFieldChoice.All`/`For`, Tag Remover selective default, Date/Time Setter HH:mm revert + independent OOR date apply, Attributes Keep field defaults, tip/min-max cleanup, tests).

This doc lists **not done** work only.

## Already done (do not re-open)

- Date + Time Setter → one `DateTimeSetter` filter + editor (#24)
- Attributes Setter radios bind `AttributeTriState` directly (#29)
- `FileTimestampDateLimits` shared range owner (#30)
- Per-editor Filter Configuration test suites under `Mfr.Tests/Ui/FilterEditors/<Group>/` (master after #24–#30)
- Shared timestamp-field combo catalog on `TimestampFieldChoice` (autofix)
- Tag Remover block-kind catalog UI (`AudioTagBlockKindChoice` + row VMs + `ItemsControl`)

## Ranked follow-ups (best cost-to-value first)

### 1. Tag Remover block-kind catalog UI — **done**

- Shipped: `AudioTagBlockKindChoice` + `TagRemoverBlockRowViewModel` + `ItemsControl` over rows; selection = `IsSelected` → `Blocks` list.

### 2. Time Shifter DateTime overflow policy — **medium / high**

- **Sites:** `TimeShifterFilter._Shift` (`AddMonths` / `AddYears` / …); editor ±10M spinners
- **Target:** catch/clamp out-of-range `Add*` in domain (no-op or clamp), and/or unit-aware UI maxes; optionally reuse `FileTimestampDateLimits` when the shifted calendar date would leave the product range
- **Value:** preview/apply never throws on legal spinner input; closes commit failure modes
- **Cost:** medium (behavior choice + filter tests); editor-only tighter max is smaller but weaker
- **Rank:** medium–high — harden when touching Attributes filters again

### 3. Shared timestamp-field RichToolTip resource — **medium**

- **Sites:** identical Creation / Last Write / Last Access bullets in `DateTimeSetterFilterEditorView.axaml` and `TimeShifterFilterEditorView.axaml`
- **Target:** one tip resource or small tip control used by both
- **Value:** closes tip-text drift
- **Cost:** low–medium AXAML; weak until a tip wording change is needed
- **Rank:** medium — later / ride along

### 4. AudioTagBlockKind UI display names — **partial / medium**

- **Sites:** `AudioTagBlockKindChoice` (Tag Remover labels/tips) vs `AudioTagContainerPolicy` describe helpers
- **Target:** promote/reuse the choice catalog when Audio Tag Setter / more block UIs land (do **not** bind AXAML to private domain helpers)
- **Value:** one label source across block UIs
- **Cost:** low once a second consumer appears
- **Rank:** medium — wait for a second consumer

### 5. Attributes Setter radio-row control — **medium / low**

- **Sites:** four near-identical On/Off/Keep stacks in `AttributesSetterFilterEditorView.axaml`
- **Target:** small UserControl with Label + GroupName + two-way `AttributeTriState`
- **Value:** ~80 AXAML lines → one template; spacing/tip drift closed
- **Cost:** new control + headless name reachability; **single caller today**
- **Rank:** medium/low — only if layout keeps churning or a second On/Off/Keep editor appears

### 6. Multiline list editor / line-iteration helper — **medium** (carried from prior review doc)

- **Sites:** Name List + Replace List Entries fieldsets; Format/Parse line loops
- **Target:** shared multiline Entries control; optional shared `EnumerateLines` where semantics match
- **Value:** less AXAML drift
- **Cost:** medium
- **Rank:** medium — unchanged from prior pass

### 7. Space Character MFR7 empty-Other validation — **medium** (carried)

- **Sites:** `_ResolveSpaceCharacter` empty Other → `' '`
- **Target:** don’t persist / don’t apply when Other + empty
- **Rank:** medium

### 8. `CountFilterOptions.ClampToLength` helper — **medium** (carried)

- **Sites:** identical clamp + slice in four Count filters
- **Rank:** medium

### 9. Replace List Regex compile at Setup — **medium** (carried)

- See `docs/plans/filter-editor-review-deeper-refactors.md`

### 10. `_NudgeBoundText` binding redesign — **low**

- **Sites:** Date/Time Setter revert path
- **Target:** OneWay + code-behind or other Avalonia binding approach instead of Dispatcher clear-then-restore
- **Value:** less fragile UI sync
- **Cost:** higher churn/risk; headless rewrite
- **Rank:** low — keep race-hardened nudge unless a second consumer appears

### 11. Structural equality for list-valued filter options — **low**

- **Sites:** `TagRemoverOptions.Blocks` and other `IReadOnlyList<>` options + `ApplyIfChanged`
- **Target:** sequence equality so identical selective lists do not spuriously `SetFilter`
- **Rank:** low — cross-cutting; skip unless touching options equality broadly

## Explicit non-goals / skip

| Idea                                                         | Why skip                               |
| ------------------------------------------------------------ | -------------------------------------- |
| Re-split Date/Time Setter into two filters                   | Product chose one filter (#24)         |
| Restore Attributes Setter tri-state checkboxes               | Radios are clearer for Set/Clear/Keep  |
| Merge Time Shifter into Date/Time Setter                     | Shift ≠ set; plan forbids grouping     |
| Reorder `AttributeTriState` so Keep = 0                      | Field init is enough; avoid enum churn |
| Shared enum-radio primitive across Letters Case + Attributes | Different layouts/semantics            |
| Force-merge Tag Remover with Audio Tag Setter                | Options surfaces differ                |
| Drop apply-time `FileTimestampDateLimits` guard              | Presets/JSON can still carry bad dates |
| Legacy JSON migration for `DateSetter` / `TimeSetter`        | No-legacy-compat policy                |

## Autofix PR index (this pass)

Applied on branch `cursor/review-f5-editors-autofix-eb72` (see that PR for the concrete diff).
