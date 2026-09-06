---
name: F6 FormatEditor review deeper refactors
overview: Ranked follow-ups from MFR skill reviews of F6 PRs A/B/C (engine, FormatEditor, param editors) that were proposed but not applied in review autofixes. Prefer high cost-to-value first.
---

# F6 FormatEditor — deeper refactors

Synthesized from per-PR findings reviews of:

| PR  | Area                                                           |
| --- | -------------------------------------------------------------- |
| A   | `FormatTokenCatalog` + `FormatStringSyntax.TryValidate`        |
| B   | Shared `FormatEditor` + Formatter filter wiring                |
| C   | Param dialogs / registry / token editor VMs + Edit under caret |

High-confidence autofixes landed in those review passes (see below). This doc lists **not done** work only; **Value** means why a human would bother (bugs avoided, UX, or future cost), not the code shape.

## Already done in reviews (do not re-open)

### PR A

- Closed `TryValidate` / `Compile` mismatch (balanced unknown spans fail like Compile; LooksLike only for unclosed `<`)
- Frozen catalog list (`[.. catalog]`)
- Stronger catalog/syntax tests (`InsertText` validates + compiles; balanced-unknown fail theory)

### PR B

- Insert on list **tap**, not `SelectionChanged` (keyboard highlight no longer inserts)
- Double-tap token select: Bubble routing; do not reset `CaretIndex` after selection
- Suppress Text DP → `TemplateBox` push so Validate does not double-fire
- Single `MaxInlineErrorLength`; details dialog when inline ≠ full message
- Headless facts: tap insert, double-tap select, long-error truncate, search clear on insert

### PR C

- Layering: `CreateBody` moved out of ViewModels → `FormatTokenEditorBodyFactory` in Views
- Edit path: dropped redundant `HasEditor` before `TryCreate`; replace uses `ResultingFormatString`
- Removed unused `TimestampField` from `FileDate` `KindChoice`
- Replaced nested ternary in `FileSize` with `_ResolveUnit`

### Polish (post-review)

- Shared `FormatTokenChoice(string Value, string Label)` under FormatEditor VMs; Counter / FileDate / FileSize nested choice records deleted
- Convention `FormatTokenEditorViewLocator` (`…ViewModel` → `…View`); deleted `FormatTokenEditorBodyFactory` switch; `FormatTokenEditorRegistry` name→VM list kept

## Defer past F6 (feature / host work)

| Item                                                      | Why defer                                      |
| --------------------------------------------------------- | ---------------------------------------------- |
| Nested `FormatEditor` in substr/token `source=` fields    | High engine+UI+gesture cost; not F6 exit       |
| Nested token spans / caret hit-test in `TryValidate`      | Needed mainly for nested Edit; ride with above |
| Inserter / Audio “likely-token” validation policy flag    | No second FormatEditor host yet                |
| Shared OK-only message dialog across app hosts            | Wait for PathMover / Audio / second consumer   |
| Replace `[FormatTokenInfo]` reflection with hand registry | Explicit non-goal (open catalog is correct)    |

## Ranked follow-ups (best cost-to-value first)

### 1. Share named-arg parse with Filters — **high**

- **Sites:** `NamedFormatOptionsBuilder` (`TryParse` / `_SplitNamedArgumentSegments`) ↔ `FormatOptionsParsing.SplitNamedArgumentSegments` / `ParseNamedKeyValuePairs` (identical depth-aware comma split + key=value rules; throw vs bool)
- **Target:** one public owner in `Mfr.Filters` (or `Mfr.Utils`); UI wraps throw→bool (and keeps Join / FormatInt / FormatBool / soft Get helpers)
- **Value:** Today the dialog layer and the rename engine each re-implement “split on commas unless inside `<…>`.” If those copies drift, a user can OK a token in the FormatEditor dialog that later fails at preview/Apply (or the reverse: Compile accepts something the dialog cannot round-trip). One owner means nested `source=<…>` and `key=value` rules stay identical forever, and the next named-option token only learns one API.
- **Cost:** public API surface + Filters tests; medium churn; keep soft dialog defaults separate from Compile throws
- **Rank:** high — first structural dedup after F6 merge

### 2. One template-walk owner for Compile + TryValidate — **done**

- **Done:** `FormatStringScan.TryWalk` yields ordered literal/token pieces (+ unclosed-likely error); `Compile` / `TryValidate` consume the same walk; shared `UnknownTokenMessage`
- **Value (why it mattered):** FormatEditor used to be able to show green while Formatter `_Setup` threw on the same string (or vice versa). One walk closed that “UI said OK, rename failed” class of bugs for balanced spans.
- **Left as-is:** `ContainsLikelyFormatTokens` keeps its own trim-aware heuristic walk (see item 6 for name-trim product call)

### 3. Grouped insert picker when search empty — **done**

- **Done:** `FormatInsertPickerNode` tree by `GroupPath` when search empty; flat leaves (+ group subtitle) when filtering; `TreeView` insert flyout; tap inserts leaves only
- **Value (why it mattered):** With 90+ tokens, a flat list is hard to browse the MFR7 way (File Name / Audio / Image…). Grouping restores muscle memory; search still wins when the user knows a fragment of the name.
- **Sites:** `FormatEditor.axaml` / `FormatEditorViewModel` / headless `InsertList_*`

### 4. Flyout UX: focus search + Enter-to-insert — **done**

- **Done:** focus `InsertSearchBox` on flyout open; Enter on search/list inserts highlighted leaf only (tap still inserts; SelectionChanged does not)
- **Value (why it mattered):** Keyboard-only insert without mouse hunting; matches “open ▶, type, Enter” expectations and avoids accidental inserts from arrow-key selection alone.
- **Sites:** `FormatEditor.axaml` / `FormatEditor.axaml.cs` / headless `InsertFlyout_*` / `InsertList_Enter_*` / `InsertSearchBox_Enter_*`

### 5. Convention ViewLocator for token bodies — **done**

- **Done:** `FormatTokenEditorViewLocator` naming-convention `…ViewModel` → `…View` (mirrors `FilterEditorViewLocator`); dialog hosts body via `ContentControl` + locator; `FormatTokenEditorBodyFactory` removed
- **Kept:** explicit `FormatTokenEditorRegistry` editable-token name→VM list (product identity)
- **Value (why it mattered):** Adding a new arg-bearing token no longer means editing a twin switch (VM factory *and* View factory). Less chance the dialog opens with a blank body after a rename.
- **Sites:** `FormatTokenEditorViewLocator` / `FormatTokenEditorDialog` / `FormatTokenEditorViewLocatorTests`

### 6. Consistent name trimming in Compile — **medium** (product call)

- **Sites:** `_CompileToken` (no trim) vs any residual trim expectations in UX / docs
- **Target:** trim both or neither; document
- **Value:** Rare edge: templates like `<file-name >` (space before `>`). Today engine and UI can disagree on whether that is a known token. Aligning trim policy removes “works in one place, unknown token in the other” for whitespace typos—but changing Compile may break templates that currently rely on exact spacing, so it needs an explicit product call, not a silent fix.
- **Cost:** behavior change for edge templates; low LOC
- **Rank:** medium — only with an explicit product decision

### 7. Theme-aware error link color — **low**

- **Sites:** `Themes/FilterEditor.axaml` hard-coded `#C42B1C`
- **Target:** Light/Dark dictionary brushes
- **Value:** On dark UI chrome the fixed red can be hard to read or clash with theme accents. Theme brushes keep the “this is an error link” cue without fighting Dark mode. Pure polish—no correctness impact.
- **Cost:** tiny
- **Rank:** low — polish when touching theme resources

### 8. Shared OK-only message dialog — **low**

- **Sites:** `FormatEditorMessageDialog`; similar OK-only windows elsewhere
- **Target:** one small shared dialog when a second FormatEditor host (or peer control) needs it
- **Value:** Avoids a third copy of “title + message + OK” when PathMover / Audio Tag Setter adopt FormatEditor (or another control needs the same Edit-miss warning). Little payoff while FormatEditor has one host—extracting now is busywork.
- **Cost:** low now, weak until reuse
- **Rank:** low — wait for PathMover / Audio / second consumer

### 9. Nested FormatEditor in `source=` + nested spans — **low for F6**

- **Sites:** `SubstrFormatTokenEditorViewModel` / `TokenFormatTokenEditorViewModel` Source text boxes; outer-only `FormatTokenSpan` list; tokens that `Compile` nested `source=`
- **Target:** embed FormatEditor in source fields + optional nested spans / caret hit-test in `TryValidate`
- **Value:** Today you can insert `<substr:…,source=<file-name>>` and edit the *outer* token, but editing the inner `<file-name>` (or jumping to an error *inside* `source=`) needs a second parser / nested editor. Shipping nested FormatEditor would make complex format strings editable the same way as the top-level box—high user value for power users, but it is a feature project, not a cleanup.
- **Cost:** high (engine + UI + gestures + tests)
- **Rank:** low for F6 — post-F6 feature; pair engine nested spans with UI

### 10. Inserter/Audio “likely-token” validation mode — **low**

- **Sites:** `FormatStringCompiler.ContainsLikelyFormatTokens` gate vs Formatter always-Compile; FormatEditor always validates
- **Target:** FormatEditor validation policy flag when hosts do not always-Compile
- **Value:** Formatter always treats balanced `<…>` as tokens. Inserter / some Audio fields only compile when text “looks like” tokens, so literal `<3>` or comparison text can stay literal. If those hosts reuse FormatEditor without a policy flag, the error row will red-flag strings that currently rename fine. The flag preserves host semantics when FormatEditor is adopted outside Formatter.
- **Cost:** UI API; not needed until those hosts adopt FormatEditor
- **Rank:** low — defer until reuse

## Explicit non-goals / skip

| Idea                                                      | Why skip                                    |
| --------------------------------------------------------- | ------------------------------------------- |
| Hand-written token catalog instead of `[FormatTokenInfo]` | Open catalog + missing-attr fail is correct |
| Erase explicit `FormatTokenEditorRegistry` name list      | Editable-token set is product identity      |
| Force nested FormatEditor into PR C / F6 exit             | Soft Source text boxes are enough for F6    |
| Merge Padding/Reset choices into enums fighting ComboBox  | String Value + Label binds cleanly          |
| Debounce live Validate                                    | Validate is cheap; B left it intentional    |
| Move format string off `Text` DP onto FormatEditor VM     | DP is right for a reusable control          |

## Autofix index (this pass)

Applied inline on branches `f6/formateditor-a`, `f6/formateditor-b`, and `f6/formateditor-c` during the respective PR review gates (see those review transcripts / diffs). No separate autofix PR required for the applied items above.
